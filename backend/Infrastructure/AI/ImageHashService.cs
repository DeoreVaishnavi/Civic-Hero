using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Cryptography;
using System.Text;
using CivicHero.Backend.Core.Interfaces;

namespace CivicHero.Backend.Infrastructure.AI;

/// <summary>
/// Implementation of image hashing services for duplicate detection and perceptual similarity.
/// </summary>
public class ImageHashService : IImageHashService
{
    /// <summary>
    /// Computes the SHA-256 hash of the image bytes (exact match).
    /// </summary>
    /// <param name="imageBytes">The image bytes.</param>
    /// <returns>Hexadecimal string representation of the hash.</returns>
    public Task<string> ComputeHashAsync(byte[] imageBytes)
    {
        return Task.Run(() =>
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(imageBytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        });
    }

    /// <summary>
    /// Computes a perceptual hash (average hash) of the image for similarity detection.
    /// The image is resized to 8x8, converted to grayscale, and compared against the mean.
    /// Returns a 64-bit hash as a hexadecimal string.
    /// </summary>
    /// <param name="imageBytes">The image bytes.</param>
    /// <returns>Hexadecimal string representation of the 64-bit perceptual hash.</returns>
    public Task<string> ComputePerceptualHashAsync(byte[] imageBytes)
    {
        return Task.Run(() =>
        {
            using var ms = new MemoryStream(imageBytes);
            using var img = Image.FromStream(ms);
            // Resize to 8x8
            using var resized = new Bitmap(8, 8);
            using var graphics = Graphics.FromImage(resized);
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
            graphics.DrawImage(img, 0, 0, 8, 8);

            // Convert to grayscale and get pixel values
            var grayValues = new int[8, 8];
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    var pixel = resized.GetPixel(x, y);
                    // Grayscale conversion (standard luminance)
                    int gray = (int)(0.299 * pixel.R + 0.587 * pixel.G + 0.114 * pixel.B);
                    grayValues[y, x] = gray;
                }
            }

            // Compute mean
            double sum = 0;
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                    sum += grayValues[y, x];
            double mean = sum / 64;

            // Build 64-bit hash
            ulong hash = 0;
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    hash <<= 1;
                    if (grayValues[y, x] >= mean)
                        hash |= 1;
                }
            }

            return hash.ToString("x16"); // 16 hex characters = 64 bits
        });
    }

    /// <summary>
    /// Computes the Hamming distance between two perceptual hash strings.
    /// Returns the number of differing bits.
    /// </summary>
    /// <param name="hash1">First hash as hex string.</param>
    /// <param name="hash2">Second hash as hex string.</param>
    /// <returns>Hamming distance.</returns>
    public int ComputeHammingDistance(string hash1, string hash2)
    {
        // Convert hex strings to ulong
        if (!ulong.TryParse(hash1, System.Globalization.NumberStyles.HexNumber, null, out ulong val1) ||
            !ulong.TryParse(hash2, System.Globalization.NumberStyles.HexNumber, null, out ulong val2))
        {
            throw new ArgumentException("Invalid hash string format.");
        }

        // XOR and count bits
        ulong x = val1 ^ val2;
        int distance = 0;
        while (x != 0)
        {
            distance += (int)(x & 1UL);
            x >>= 1;
        }
        return distance;
    }
}