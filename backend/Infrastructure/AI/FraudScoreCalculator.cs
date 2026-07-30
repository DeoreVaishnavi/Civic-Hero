using CivicHero.Backend.Core.Interfaces;

namespace CivicHero.Backend.Infrastructure.AI;

/// <summary>
/// Calculates a fraud score based on multiple forensic signals.
/// </summary>
public class FraudScoreCalculator
{
    private readonly IMetadataForensics _metadataForensics;
    private readonly IImageHashService _imageHashService;

    // Weights for each component (must sum to 1.0)
    private const double WeightMetadata = 0.3;
    private const double WeightHashDuplicate = 0.2;
    private const double WeightGpsDistance = 0.2;
    private const double WeightUserTrust = 0.3;

    public FraudScoreCalculator(IMetadataForensics metadataForensics, IImageHashService imageHashService)
    {
        _metadataForensics = metadataForensics ?? throw new ArgumentNullException(nameof(metadataForensics));
        _imageHashService = imageHashService ?? throw new ArgumentNullException(nameof(imageHashService));
    }

    /// <summary>
    /// Computes a fraud score (0-100) based on provided evidence.
    /// </summary>
    /// <param name="imageBytes">The image bytes to analyze.</param>
    /// <param name="expectedTimestamp">Expected timestamp of the incident (optional).</param>
    /// <param name="imageGpsLatitude">GPS latitude embedded in image (if available).</param>
    /// <param name="imageGpsLongitude">GPS longitude embedded in image (if available).</param>
    /// <param name="complaintLatitude">Latitude from the complaint report.</param>
    /// <param name="complaintLongitude">Longitude from the complaint report.</param>
    /// <param name="userTrustScore">Trust score of the user submitting the complaint (0-1).</param>
    /// <param name="existingImageHashes">Set of known image hashes to check for duplicates (optional).</param>
    /// <returns>Tuple of fraud score (0-100) and list of reason strings.</returns>
    public async Task<(int score, List<string> reasons)> ComputeFraudScoreAsync(
        byte[] imageBytes,
        DateTime? expectedTimestamp = null,
        double? imageGpsLatitude = null,
        double? imageGpsLongitude = null,
        double? complaintLatitude = null,
        double? complaintLongitude = null,
        double userTrustScore = 0.5,
        ISet<string>? existingImageHashes = null)
    {
        var reasons = new List<string>();
        double scoreContribution = 0.0;
        double totalWeight = 0.0;

        // 1. Metadata forensics
        var metaResult = await _metadataForensics.AnalyzeMetadataAsync(imageBytes, expectedTimestamp);
        double metaScore = 1.0 - metaResult.AuthenticityScore; // Invert: higher authenticity -> lower fraud contribution
        if (metaResult.HasExifData)
        {
            scoreContribution += metaScore * WeightMetadata;
            totalWeight += WeightMetadata;
            if (metaScore > 0.1)
                reasons.Add($"Image metadata shows signs of tampering or inconsistency (authenticity score: {metaResult.AuthenticityScore:F2}).");
            else
                reasons.Add("Image metadata appears consistent.");
        }
        else
        {
            // No EXIF data is suspicious
            scoreContribution += 1.0 * WeightMetadata; // fully suspicious
            totalWeight += WeightMetadata;
            reasons.Add("Image lacks EXIF metadata, which may indicate stripping or screenshot.");
        }

        // 2. Perceptual hash duplicate detection
        if (existingImageHashes != null && existingImageHashes.Any())
        {
            string hash = await _imageHashService.ComputePerceptualHashAsync(imageBytes);
            int minDistance = int.MaxValue;
            string? closestHash = null;
            foreach (var knownHash in existingImageHashes)
            {
                int dist = _imageHashService.ComputeHammingDistance(hash, knownHash);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestHash = knownHash;
                }
            }

            // If distance is small (<=5) consider it a duplicate/near-duplicate
            if (minDistance <= 5)
            {
                scoreContribution += 1.0 * WeightHashDuplicate; // fully suspicious
                totalWeight += WeightHashDuplicate;
                reasons.Add($"Image appears to be a duplicate or near-duplicate of existing evidence (Hamming distance: {minDistance}).");
            }
            else
            {
                // No close match; contributes nothing to fraud (maybe slight trust)
                totalWeight += WeightHashDuplicate; // weight still counts but adds zero
            }
        }
        else
        {
            // No comparison set; weight still counts but adds zero
            totalWeight += WeightHashDuplicate;
        }

        // 3. GPS distance mismatch
        if (imageGpsLatitude.HasValue && imageGpsLongitude.HasValue &&
            complaintLatitude.HasValue && complaintLongitude.HasValue)
        {
            double distanceKm = HaversineDistance(
                imageGpsLatitude.Value, imageGpsLongitude.Value,
                complaintLatitude.Value, complaintLongitude.Value);
            // Expect distance within some tolerance (e.g., 0.5 km). Beyond that, increase suspicion.
            double gpsScore = Math.Min(1.0, distanceKm / 5.0); // 5 km => full suspicion
            scoreContribution += gpsScore * WeightGpsDistance;
            totalWeight += WeightGpsDistance;
            if (distanceKm > 0.5)
                reasons.Add("GPS coordinates in image differ from complaint location by {distanceKm:F1} km.");
            else
                reasons.Add("GPS location matches complaint location within tolerance.");
        }
        else
        {
            // Missing GPS data is somewhat suspicious
            scoreContribution += 0.5 * WeightGpsDistance; // moderate suspicion
            totalWeight += WeightGpsDistance;
            reasons.Add("GPS location data missing from image or complaint, limiting location verification.");
        }

        // 4. User trust score (inverse: lower trust -> higher fraud contribution)
        double trustContribution = (1.0 - Math.Max(0.0, Math.Min(1.0, userTrustScore))) * WeightUserTrust;
        scoreContribution += trustContribution;
        totalWeight += WeightUserTrust;
        if (userTrustScore < 0.3)
            reasons.Add($"User trust score is low ({userTrustScore:F2}), increasing suspicion.");
        else if (userTrustScore > 0.7)
            reasons.Add($"User trust score is high ({userTrustScore:F2}), decreasing suspicion.");
        else
            reasons.Add($"User trust score is moderate ({userTrustScore:F2}).");

        // Compute final score (0-100)
        double finalScore = 0;
        if (totalWeight > 0)
            finalScore = (scoreContribution / totalWeight) * 100.0;

        int scoreInt = (int)Math.Round(Math.Max(0, Math.Min(100, finalScore)));
        return (scoreInt, reasons);
    }

    /// <summary>
    /// Calculates the great-circle distance between two latitude/longitude points using the Haversine formula.
    /// </summary>
    private static double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusKm = 6371.0;

        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        var lat1Rad = DegreesToRadians(lat1);
        var lat2Rad = DegreesToRadians(lat2);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1Rad) * Math.Cos(lat2Rad);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return earthRadiusKm * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
}