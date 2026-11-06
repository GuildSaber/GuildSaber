using Testcontainers.PostgreSql;

namespace GuildSaber.Common.UnitTests.Utils;

internal static class PostgresTestServer
{
    private static readonly Lazy<Task<PostgreSqlContainer>> _container = new(StartAsync);

    public static async Task<string> GetConnectionStringAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable("GUILDSABER_TEST_POSTGRES");

        if (!string.IsNullOrWhiteSpace(connectionString))
            return connectionString;

        Skip.Unless(bool.TryParse(Environment.GetEnvironmentVariable("GUILDSABER_TEST_CONTAINERS"), out var enabled)
                    && enabled,
            "Set GUILDSABER_TEST_CONTAINERS=true to run PostgreSQL tests with Docker, "
            + "or set GUILDSABER_TEST_POSTGRES to an isolated PostgreSQL server.");

        return (await _container.Value).GetConnectionString();
    }

    private static async Task<PostgreSqlContainer> StartAsync()
    {
        var container = new PostgreSqlBuilder("postgres:18.6").Build();

        try
        {
            await container.StartAsync();

            return container;
        }
        catch
        {
            await container.DisposeAsync();

            throw;
        }
    }

    [After(TestSession)]
    public static async Task DisposeAsync()
    {
        if (!_container.IsValueCreated)
            return;

        var initialization = _container.Value;

        if (initialization.IsCompletedSuccessfully)
            await (await initialization).DisposeAsync();
    }
}