using CivicHero.Backend.Core.DTOs.Analytics;

namespace CivicHero.Backend.Tests.Unit.Analytics;

public sealed class PublicHeatmapPrivacyContractTests
{
    [Fact]
    public void Public_heatmap_contract_should_not_expose_complaint_identity_or_private_location_fields()
    {
        var properties = typeof(PublicHeatmapPointResponse)
            .GetProperties()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        properties.Should().NotContain("ComplaintId");
        properties.Should().NotContain("LatestComplaintId");
        properties.Should().NotContain("LatestComplaintTitle");
        properties.Should().NotContain("Title");
        properties.Should().NotContain("Description");
        properties.Should().NotContain("Address");
        properties.Should().NotContain("CitizenId");
        properties.Should().NotContain("CitizenName");
        properties.Should().NotContain("Evidence");
    }
}
