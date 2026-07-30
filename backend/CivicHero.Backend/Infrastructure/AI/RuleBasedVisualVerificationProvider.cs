using CivicHero.Backend.Core.DTOs.VisualVerification;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Services;

namespace CivicHero.Backend.Infrastructure.AI;

public sealed class RuleBasedVisualVerificationProvider : IVisualVerificationProvider
{
    public Task<VisualProviderResult?> AnalyzeAsync(
        Complaint complaint,
        IReadOnlyList<ComplaintImage> beforeImages,
        IReadOnlyList<ComplaintImage> afterImages,
        CancellationToken cancellationToken = default)
    {
        if (beforeImages.Count == 0 || afterImages.Count == 0)
        {
            return Task.FromResult<VisualProviderResult?>(new VisualProviderResult(
                "RuleBased", "evidence-metadata-v1", "InsufficientEvidence",
                0m, 0.25m, 0.50m, 0.20m,
                "Both original and resolution images are required before visual verification can be attempted.",
                ["Missing before or after evidence."]));
        }

        var quality = Math.Clamp((decimal)afterImages.Count / 3m, 0.35m, 0.85m);
        var beforeBytes = beforeImages.Sum(item => Math.Max(1L, item.FileSize));
        var afterBytes = afterImages.Sum(item => Math.Max(1L, item.FileSize));
        var sizeRatio = beforeBytes == 0 ? 1m : (decimal)afterBytes / beforeBytes;
        var suspicious = sizeRatio is < 0.08m or > 12m;
        return Task.FromResult<VisualProviderResult?>(new VisualProviderResult(
            "RuleBased", "evidence-metadata-v1", suspicious ? "Suspicious" : "NeedsHumanReview",
            0.50m, quality, suspicious ? 0.78m : 0.30m, suspicious ? 0.52m : 0.45m,
            suspicious
                ? "Resolution evidence metadata differs unusually from the original evidence. A human must inspect the images."
                : "Images are present and structurally valid, but a rule-based provider cannot determine whether physical work is complete.",
            suspicious
                ? ["Unusual before/after file-size ratio.", "No automated closure decision was made."]
                : ["Before evidence present.", "Resolution evidence present.", "Human visual review required."]));
    }
}
