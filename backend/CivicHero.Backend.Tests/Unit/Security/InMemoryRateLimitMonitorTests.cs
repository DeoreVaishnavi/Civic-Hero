using CivicHero.Backend.Infrastructure.Services;

namespace CivicHero.Backend.Tests.Unit.Security;

public sealed class InMemoryRateLimitMonitorTests
{
    [Fact]
    public void Snapshot_should_separate_rejections_by_policy()
    {
        var monitor = new InMemoryRateLimitMonitor();
        monitor.RecordAllowed("general");
        monitor.RecordAllowed("general");
        monitor.RecordRejected("authentication");
        monitor.RecordRejected("uploads");
        monitor.RecordRejected("administration");

        var snapshot = monitor.Snapshot();
        snapshot.AllowedRequests.Should().Be(2);
        snapshot.RejectedRequests.Should().Be(3);
        snapshot.AuthenticationRejections.Should().Be(1);
        snapshot.UploadRejections.Should().Be(1);
        snapshot.AdministrationRejections.Should().Be(1);
    }
}
