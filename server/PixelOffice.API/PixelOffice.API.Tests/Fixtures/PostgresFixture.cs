using Microsoft.EntityFrameworkCore;
using Npgsql;
using PixelOffice.API.Data;
using Testcontainers.PostgreSql;

namespace PixelOffice.API.Tests.Fixtures;

/// <summary>
/// One real PostgreSQL container (same image as the AppHost) shared by every persistence test.
/// Each test gets its own database, so tests never see each other's rows. Needs Docker running.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:18.3").Build();

    public Task InitializeAsync() => container.StartAsync();

    public Task DisposeAsync() => container.DisposeAsync().AsTask();

    /// <summary>Connection string for a new, not-yet-created database (it is created on first migrate).</summary>
    public string NewDatabaseConnectionString() =>
        new NpgsqlConnectionStringBuilder(container.GetConnectionString())
        {
            Database = $"test_{Guid.NewGuid():N}",
        }.ConnectionString;

    /// <summary>Creates a fresh database by applying the real migrations, and returns options pointing at it.</summary>
    public async Task<DbContextOptions<AppDbContext>> CreateDatabaseAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(NewDatabaseConnectionString()).Options;
        await using var db = new AppDbContext(options);
        await db.Database.MigrateAsync();
        return options;
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
