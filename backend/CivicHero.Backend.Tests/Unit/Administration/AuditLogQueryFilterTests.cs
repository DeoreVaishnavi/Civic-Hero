using CivicHero.Backend.Core.DTOs.Administration;
using CivicHero.Backend.Core.Entities;
using CivicHero.Backend.Core.Services;

namespace CivicHero.Backend.Tests.Unit.Administration;

public sealed class AuditLogQueryFilterTests
{
    [Fact]
    public void Dedicated_ip_filter_returns_only_matching_addresses()
    {
        var logs = new[]
        {
            Log(1, "10.10.5.21"),
            Log(2, "10.10.8.42"),
            Log(3, "2001:db8::21"),
            Log(4, null)
        }.AsQueryable();

        var result = AuditLogQueryFilter.Apply(logs, new AuditLogQuery
        {
            IpAddress = " 10.10.5 "
        }).Select(x => x.Id).ToArray();

        result.Should().Equal(1L);
    }

    [Fact]
    public void General_search_includes_ip_address()
    {
        var logs = new[]
        {
            Log(1, "192.168.1.15"),
            Log(2, "192.168.1.25")
        }.AsQueryable();

        var result = AuditLogQueryFilter.Apply(logs, new AuditLogQuery
        {
            Search = "1.25"
        }).Select(x => x.Id).ToArray();

        result.Should().Equal(2L);
    }

    [Fact]
    public void Ip_filter_combines_with_existing_result_filter()
    {
        var successful = Log(1, "172.16.0.8");
        successful.Success = true;
        var failed = Log(2, "172.16.0.8");
        failed.Success = false;

        var result = AuditLogQueryFilter.Apply(new[] { successful, failed }.AsQueryable(), new AuditLogQuery
        {
            IpAddress = "172.16.0.8",
            Success = false
        }).Select(x => x.Id).ToArray();

        result.Should().Equal(2L);
    }

    private static AuditLog Log(long id, string? ipAddress) => new()
    {
        Id = id,
        Action = "TEST_ACTION",
        EntityName = "TestEntity",
        IpAddress = ipAddress,
        CreatedAt = DateTimeOffset.UtcNow,
        HttpStatusCode = 200
    };
}
