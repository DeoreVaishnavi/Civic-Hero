using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace CivicHero.Backend.Infrastructure.AI;

public sealed record ExtractedMediaMetadata(
    string Status,
    string? CameraMake,
    string? CameraModel,
    string? Software,
    string? CapturedAtRaw,
    DateTimeOffset? CapturedAt,
    bool CaptureTimeHasOffset,
    decimal? Latitude,
    decimal? Longitude,
    IReadOnlyList<string> EditingTools,
    IReadOnlyList<string> Notes);

public static class MediaMetadataExtractor
{
    private static readonly string[] EditorKeywords =
    [
        "adobe photoshop", "photoshop", "lightroom", "gimp", "snapseed",
        "picsart", "canva", "affinity photo", "pixelmator", "paint.net",
        "photopea", "capture one", "luminar", "corel paintshop"
    ];

    public static ExtractedMediaMetadata Extract(ReadOnlyMemory<byte> content, string? mimeType = null)
    {
        if (content.Length < 12)
            return Empty("UnsupportedOrTruncated", "The file is too small for supported image metadata parsing.");

        try
        {
            var bytes = content.ToArray();
            byte[]? tiff = null;
            var text = new List<string>();
            var notes = new List<string>();

            if (IsJpeg(bytes))
                ReadJpeg(bytes, ref tiff, text, notes);
            else if (IsPng(bytes))
                ReadPng(bytes, ref tiff, text, notes);
            else if (IsWebP(bytes))
                ReadWebP(bytes, ref tiff, text, notes);
            else
                return Empty("UnsupportedFormat", $"Metadata parsing is not available for {mimeType ?? "this file format"}.");

            TiffMetadata? parsed = null;
            if (tiff is { Length: >= 8 })
            {
                try { parsed = new TiffReader(tiff).Read(); }
                catch (InvalidDataException exception) { notes.Add($"EXIF block could not be fully parsed: {exception.Message}"); }
            }

            var combinedText = string.Join(" ", text.Append(parsed?.Software ?? string.Empty));
            var tools = EditorKeywords
                .Where(keyword => combinedText.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                .Select(NormalizeToolName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var status = parsed is not null
                ? "Extracted"
                : text.Count > 0 ? "TextMetadataOnly" : "NoExifMetadata";

            return new ExtractedMediaMetadata(
                status,
                Clean(parsed?.CameraMake),
                Clean(parsed?.CameraModel),
                Clean(parsed?.Software),
                Clean(parsed?.CapturedAtRaw),
                parsed?.CapturedAt,
                parsed?.CaptureTimeHasOffset ?? false,
                parsed?.Latitude,
                parsed?.Longitude,
                tools,
                notes);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return Empty("MetadataReadFailed", $"Metadata parsing failed safely: {exception.Message}");
        }
    }

    private static ExtractedMediaMetadata Empty(string status, string note) =>
        new(status, null, null, null, null, null, false, null, null, [], [note]);

    private static bool IsJpeg(byte[] bytes) => bytes[0] == 0xFF && bytes[1] == 0xD8;

    private static bool IsPng(byte[] bytes) => bytes.AsSpan(0, 8).SequenceEqual(
        new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });

    private static bool IsWebP(byte[] bytes) =>
        bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
        bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8);

    private static void ReadJpeg(byte[] bytes, ref byte[]? tiff, List<string> text, List<string> notes)
    {
        var offset = 2;
        while (offset + 4 <= bytes.Length)
        {
            while (offset < bytes.Length && bytes[offset] != 0xFF) offset++;
            while (offset < bytes.Length && bytes[offset] == 0xFF) offset++;
            if (offset >= bytes.Length) break;

            var marker = bytes[offset++];
            if (marker is 0xD9 or 0xDA) break;
            if (marker is 0x01 or >= 0xD0 and <= 0xD7) continue;
            if (offset + 2 > bytes.Length) break;

            var segmentLength = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset, 2));
            if (segmentLength < 2 || offset + segmentLength > bytes.Length) break;

            var payloadOffset = offset + 2;
            var payloadLength = segmentLength - 2;
            var payload = bytes.AsSpan(payloadOffset, payloadLength);

            if (marker == 0xE1 && payload.Length >= 6 && payload[..6].SequenceEqual(new byte[] { 0x45, 0x78, 0x69, 0x66, 0, 0 }))
                tiff ??= payload[6..].ToArray();
            else if (marker == 0xE1 || marker == 0xED || marker == 0xEE || marker == 0xFE)
                AddText(payload, text);

            offset += segmentLength;
        }

        if (tiff is null) notes.Add("No JPEG EXIF APP1 block was found.");
    }

    private static void ReadPng(byte[] bytes, ref byte[]? tiff, List<string> text, List<string> notes)
    {
        var offset = 8;
        while (offset + 12 <= bytes.Length)
        {
            var length = checked((int)BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset, 4)));
            if (length < 0 || offset + 12L + length > bytes.Length) break;
            var type = Encoding.ASCII.GetString(bytes, offset + 4, 4);
            var payload = bytes.AsSpan(offset + 8, length);

            if (type == "eXIf") tiff ??= payload.ToArray();
            else if (type is "tEXt" or "iTXt") AddText(payload, text);

            offset += length + 12;
            if (type == "IEND") break;
        }

        if (tiff is null) notes.Add("No PNG eXIf chunk was found.");
    }

    private static void ReadWebP(byte[] bytes, ref byte[]? tiff, List<string> text, List<string> notes)
    {
        var offset = 12;
        while (offset + 8 <= bytes.Length)
        {
            var type = Encoding.ASCII.GetString(bytes, offset, 4);
            var length = checked((int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset + 4, 4)));
            if (length < 0 || offset + 8L + length > bytes.Length) break;
            var payload = bytes.AsSpan(offset + 8, length);

            if (type == "EXIF")
            {
                tiff ??= payload.Length >= 6 && payload[..6].SequenceEqual(new byte[] { 0x45, 0x78, 0x69, 0x66, 0, 0 })
                    ? payload[6..].ToArray()
                    : payload.ToArray();
            }
            else if (type == "XMP ") AddText(payload, text);

            offset += 8 + length + (length % 2);
        }

        if (tiff is null) notes.Add("No WebP EXIF chunk was found.");
    }

    private static void AddText(ReadOnlySpan<byte> bytes, List<string> output)
    {
        if (bytes.Length == 0) return;
        var length = Math.Min(bytes.Length, 64 * 1024);
        var value = Encoding.UTF8.GetString(bytes[..length]).Replace('\0', ' ').Trim();
        if (!string.IsNullOrWhiteSpace(value)) output.Add(value);
    }

    private static string NormalizeToolName(string keyword) => keyword switch
    {
        "photoshop" or "adobe photoshop" => "Adobe Photoshop",
        "lightroom" => "Adobe Lightroom",
        "gimp" => "GIMP",
        "snapseed" => "Snapseed",
        "picsart" => "Picsart",
        "canva" => "Canva",
        "affinity photo" => "Affinity Photo",
        "pixelmator" => "Pixelmator",
        "paint.net" => "Paint.NET",
        "photopea" => "Photopea",
        "capture one" => "Capture One",
        "luminar" => "Luminar",
        "corel paintshop" => "Corel PaintShop",
        _ => keyword
    };

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record TiffMetadata(
        string? CameraMake,
        string? CameraModel,
        string? Software,
        string? CapturedAtRaw,
        DateTimeOffset? CapturedAt,
        bool CaptureTimeHasOffset,
        decimal? Latitude,
        decimal? Longitude);

    private sealed class TiffReader
    {
        private readonly byte[] _bytes;
        private readonly bool _littleEndian;

        public TiffReader(byte[] bytes)
        {
            _bytes = bytes;
            if (bytes.Length < 8) throw new InvalidDataException("TIFF header is truncated.");
            _littleEndian = bytes[0] == (byte)'I' && bytes[1] == (byte)'I';
            if (!_littleEndian && !(bytes[0] == (byte)'M' && bytes[1] == (byte)'M'))
                throw new InvalidDataException("TIFF byte order is invalid.");
            if (ReadUInt16(2) != 42) throw new InvalidDataException("TIFF marker is invalid.");
        }

        public TiffMetadata Read()
        {
            var rootOffset = checked((int)ReadUInt32(4));
            var root = ReadIfd(rootOffset);

            var make = ReadAscii(root, 0x010F);
            var model = ReadAscii(root, 0x0110);
            var software = ReadAscii(root, 0x0131);
            var capturedRaw = ReadAscii(root, 0x0132);
            string? offsetRaw = null;

            if (ReadPointer(root, 0x8769) is int exifOffset)
            {
                var exif = ReadIfd(exifOffset);
                capturedRaw = ReadAscii(exif, 0x9003) ?? ReadAscii(exif, 0x9004) ?? capturedRaw;
                offsetRaw = ReadAscii(exif, 0x9011) ?? ReadAscii(exif, 0x9012);
            }

            decimal? latitude = null;
            decimal? longitude = null;
            if (ReadPointer(root, 0x8825) is int gpsOffset)
            {
                var gps = ReadIfd(gpsOffset);
                var latRef = ReadAscii(gps, 0x0001);
                var lat = ReadRationals(gps, 0x0002);
                var lonRef = ReadAscii(gps, 0x0003);
                var lon = ReadRationals(gps, 0x0004);
                latitude = ToCoordinate(lat, latRef, 'S');
                longitude = ToCoordinate(lon, lonRef, 'W');
            }

            var (capturedAt, hasOffset) = ParseDate(capturedRaw, offsetRaw);
            return new TiffMetadata(make, model, software, capturedRaw, capturedAt, hasOffset, latitude, longitude);
        }

        private Dictionary<ushort, Entry> ReadIfd(int offset)
        {
            Ensure(offset, 2);
            var count = Math.Min((int)ReadUInt16(offset), 512);
            var result = new Dictionary<ushort, Entry>();
            var cursor = offset + 2;
            for (var index = 0; index < count; index++)
            {
                Ensure(cursor, 12);
                var tag = ReadUInt16(cursor);
                result[tag] = new Entry(
                    tag,
                    ReadUInt16(cursor + 2),
                    ReadUInt32(cursor + 4),
                    cursor + 8);
                cursor += 12;
            }
            return result;
        }

        private string? ReadAscii(IReadOnlyDictionary<ushort, Entry> entries, ushort tag)
        {
            if (!entries.TryGetValue(tag, out var entry) || entry.Type != 2 || entry.Count == 0) return null;
            var length = checked((int)Math.Min(entry.Count, 4096u));
            var bytes = ReadEntryBytes(entry, length);
            return Encoding.ASCII.GetString(bytes).TrimEnd('\0', ' ', '\r', '\n', '\t');
        }

        private int? ReadPointer(IReadOnlyDictionary<ushort, Entry> entries, ushort tag)
        {
            if (!entries.TryGetValue(tag, out var entry) || entry.Count == 0 || entry.Type is not (3 or 4)) return null;
            var bytes = ReadEntryBytes(entry, entry.Type == 3 ? 2 : 4);
            var value = entry.Type == 3 ? ReadUInt16(bytes, 0) : ReadUInt32(bytes, 0);
            return value > int.MaxValue ? null : (int)value;
        }

        private IReadOnlyList<decimal> ReadRationals(IReadOnlyDictionary<ushort, Entry> entries, ushort tag)
        {
            if (!entries.TryGetValue(tag, out var entry) || entry.Type != 5 || entry.Count == 0) return [];
            var count = checked((int)Math.Min(entry.Count, 16u));
            var bytes = ReadEntryBytes(entry, count * 8);
            var output = new List<decimal>(count);
            for (var index = 0; index < count; index++)
            {
                var numerator = ReadUInt32(bytes, index * 8);
                var denominator = ReadUInt32(bytes, index * 8 + 4);
                output.Add(denominator == 0 ? 0m : numerator / (decimal)denominator);
            }
            return output;
        }

        private byte[] ReadEntryBytes(Entry entry, int requestedLength)
        {
            var unitSize = entry.Type switch
            {
                1 or 2 or 6 or 7 => 1,
                3 or 8 => 2,
                4 or 9 => 4,
                5 or 10 => 8,
                _ => throw new InvalidDataException($"Unsupported TIFF field type {entry.Type}.")
            };
            var declaredLength = checked((long)entry.Count * unitSize);
            var length = checked((int)Math.Min(declaredLength, requestedLength));
            var sourceOffset = declaredLength <= 4
                ? entry.ValueFieldOffset
                : checked((int)ReadUInt32(entry.ValueFieldOffset));
            Ensure(sourceOffset, length);
            return _bytes.AsSpan(sourceOffset, length).ToArray();
        }

        private ushort ReadUInt16(int offset)
        {
            Ensure(offset, 2);
            return ReadUInt16(_bytes, offset);
        }

        private uint ReadUInt32(int offset)
        {
            Ensure(offset, 4);
            return ReadUInt32(_bytes, offset);
        }

        private ushort ReadUInt16(byte[] source, int offset) => _littleEndian
            ? BinaryPrimitives.ReadUInt16LittleEndian(source.AsSpan(offset, 2))
            : BinaryPrimitives.ReadUInt16BigEndian(source.AsSpan(offset, 2));

        private uint ReadUInt32(byte[] source, int offset) => _littleEndian
            ? BinaryPrimitives.ReadUInt32LittleEndian(source.AsSpan(offset, 4))
            : BinaryPrimitives.ReadUInt32BigEndian(source.AsSpan(offset, 4));

        private void Ensure(int offset, int length)
        {
            if (offset < 0 || length < 0 || offset > _bytes.Length - length)
                throw new InvalidDataException("TIFF metadata points outside the file.");
        }

        private static decimal? ToCoordinate(IReadOnlyList<decimal> parts, string? reference, char negativeReference)
        {
            if (parts.Count < 3) return null;
            var coordinate = parts[0] + (parts[1] / 60m) + (parts[2] / 3600m);
            if (!string.IsNullOrWhiteSpace(reference) && char.ToUpperInvariant(reference[0]) == negativeReference)
                coordinate *= -1m;
            return Math.Round(coordinate, 7);
        }

        private static (DateTimeOffset? Value, bool HasOffset) ParseDate(string? raw, string? offsetRaw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return (null, false);
            if (!DateTime.TryParseExact(raw.Trim('\0', ' '), "yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces, out var local))
                return (null, false);

            if (!string.IsNullOrWhiteSpace(offsetRaw) && TimeSpan.TryParse(offsetRaw.Trim('\0', ' '), CultureInfo.InvariantCulture, out var offset)
                && offset >= TimeSpan.FromHours(-14) && offset <= TimeSpan.FromHours(14))
                return (new DateTimeOffset(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), offset), true);

            return (new DateTimeOffset(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), TimeSpan.Zero), false);
        }

        private sealed record Entry(ushort Tag, ushort Type, uint Count, int ValueFieldOffset);
    }
}
