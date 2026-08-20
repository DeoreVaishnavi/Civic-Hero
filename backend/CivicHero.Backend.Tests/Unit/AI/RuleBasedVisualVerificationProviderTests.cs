using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Infrastructure.AI;

namespace CivicHero.Backend.Tests.Unit.AI;

public sealed class RuleBasedVisualVerificationProviderTests
{
    private readonly RuleBasedVisualVerificationProvider _provider = new();

    [Fact]
    public async Task Missing_evidence_should_require_human_review()
    {
        var result = await _provider.AnalyzeAsync(new Complaint(), [], []);
        result.Should().NotBeNull();
        result!.Verdict.Should().Be("InsufficientEvidence");
        result.OverallConfidence.Should().BeLessThan(0.5m);
    }

    [Fact]
    public async Task Metadata_only_provider_should_never_auto_confirm_resolution()
    {
        var before = new[] { new ComplaintImage { FileSize = 100_000, MimeType = "image/jpeg" } };
        var after = new[] { new ComplaintImage { FileSize = 95_000, MimeType = "image/jpeg" } };
        var result = await _provider.AnalyzeAsync(new Complaint(), before, after);
        result.Should().NotBeNull();
        result!.Verdict.Should().Be("NeedsHumanReview");
    }
}
