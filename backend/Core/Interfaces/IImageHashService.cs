using System.Threading.Tasks;

namespace CivicHero.Backend.Core.Interfaces;

/// <summary>
/// Service for computing and comparing image hashes to detect duplicates or near-duplicates.
/// </summary>
public interface IImageHashService
{
    /// <summary>
    /// Computes a cryptographic hash (SHA-256) of the image bytes.
    /// </summary>
    /// <param name="imageBytes">The image bytes.</param>
    /// <returns>Hexadecimal string representation of the hash.</returns>
    Task<string> ComputeHashAsync(byte[] imageBytes);

    /// <summary>
    /// Computes a perceptual hash (average hash) of the image for similarity detection.
    /// </summary>
    /// <param name="imageBytes">The image bytes.</param>
    /// <returns>64-bit hash as a hexadecimal string.</returns>
    Task<string> ComputePerceptualHashAsync(byte[] imageBytes);

    /// <summary>
    /// Computes the Hamming distance between two perceptual hash strings.
    /// Returns the number of differing bits.
    /// </summary>
    /// <param name="hash1">First hash as hex string.</param>
    /// <param name="hash2">Second hash as hex string.</param>
    /// <returns>Hamming distance.</returns>
    int ComputeHammingDistance(string hash1, string hash2);
}