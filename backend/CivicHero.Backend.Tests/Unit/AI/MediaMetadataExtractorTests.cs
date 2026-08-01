using CivicHero.Backend.Infrastructure.AI;

namespace CivicHero.Backend.Tests.Unit.AI;

public sealed class MediaMetadataExtractorTests
{
    private const string ExifJpegBase64 = "/9j/4QEYRXhpZgAASUkqAAgAAAAFAA8BAgAGAAAASgAAABABAgAJAAAAUAAAADEBAgAVAAAAWQAAAGmHBAABAAAAbgAAACWIBAABAAAApwAAAAAAAABDYW5vbgBFT1MgVGVzdABBZG9iZSBQaG90b3Nob3AgMjUuMAACAAOQAgAUAAAAjAAAABGQAgAHAAAAoAAAAAAAAAAyMDI2OjA4OjAxIDE4OjMwOjAwACswNTozMAAEAAEAAgACAAAATgAAAAIABQADAAAA4AAAAAMAAgACAAAARQAAAAQABQADAAAA+AAAAAAAAAAAAAATAAAAAQAAAAQAAAABAAAAUAEAAGQAAABIAAAAAQAAADQAAAABAAAAkA8AAGQAAAD/2Q==";

    [Fact]
    public void Jpeg_exif_should_extract_camera_time_gps_and_editor_metadata()
    {
        var result = MediaMetadataExtractor.Extract(Convert.FromBase64String(ExifJpegBase64), "image/jpeg");

        result.Status.Should().Be("Extracted");
        result.CameraMake.Should().Be("Canon");
        result.CameraModel.Should().Be("EOS Test");
        result.Software.Should().Contain("Photoshop");
        result.CaptureTimeHasOffset.Should().BeTrue();
        result.CapturedAt.Should().NotBeNull();
        result.Latitude.Should().BeApproximately(19.0676m, 0.0001m);
        result.Longitude.Should().BeApproximately(72.8777m, 0.0001m);
        result.EditingTools.Should().Contain("Adobe Photoshop");
    }

    [Fact]
    public void Missing_exif_should_be_advisory_not_a_parser_failure()
    {
        var jpeg = new byte[] { 0xFF, 0xD8, 0xFF, 0xD9, 0, 0, 0, 0, 0, 0, 0, 0 };

        var result = MediaMetadataExtractor.Extract(jpeg, "image/jpeg");

        result.Status.Should().Be("NoExifMetadata");
        result.Latitude.Should().BeNull();
        result.EditingTools.Should().BeEmpty();
    }
}
