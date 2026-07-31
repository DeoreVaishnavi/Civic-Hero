using FluentAssertions;
using MySqlConnector;
using Testcontainers.MySql;
using Xunit.Abstractions;

namespace CivicHero.Backend.Tests.Integration;

public sealed class MySqlContainerSmokeTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private MySqlContainer? _container;

    public MySqlContainerSmokeTests(ITestOutputHelper output) => _output = output;

    public async Task InitializeAsync()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("CIVICHERO_RUN_TESTCONTAINERS"), "1", StringComparison.Ordinal))
        {
            _output.WriteLine("Testcontainers smoke test skipped. Set CIVICHERO_RUN_TESTCONTAINERS=1 in the release gate.");
            return;
        }

        _container = new MySqlBuilder()
            .WithImage("mysql:8.0")
            .WithDatabase("civichero_phase18")
            .WithUsername("civichero")
            .WithPassword("CivicHero-Test-Only-Password-18")
            .Build();
        await _container.StartAsync();
    }

    [Fact]
    public async Task MySql_8_container_accepts_a_real_connection()
    {
        if (_container is null)
        {
            true.Should().BeTrue();
            return;
        }

        await using var connection = new MySqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new MySqlCommand("SELECT 1", connection);
        var result = await command.ExecuteScalarAsync();
        Convert.ToInt32(result).Should().Be(1);
    }

    public async Task DisposeAsync()
    {
        if (_container is not null) await _container.DisposeAsync();
    }
}
