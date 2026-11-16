using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PixelOffice.API.Data;
using PixelOffice.API.Features.Rooms;
using PixelOffice.API.Tests.Fixtures;

namespace PixelOffice.API.Tests.Persistence;

// T07: the first migration matches the model, applies to an empty database, and Development startup
// migrates and seeds before serving.
public sealed class MigrationModelTests
{
    [Fact]
    public void Model_has_no_changes_missing_from_the_migrations()
    {
        using var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>().UseNpgsql("Host=unused.invalid;Database=pixeloffice").Options);

        Assert.False(db.Database.HasPendingModelChanges());
    }
}

[Collection(PostgresCollection.Name)]
public sealed class MigrationTests(PostgresFixture postgres)
{
    [Fact]
    public async Task First_migration_applies_cleanly_to_an_empty_database()
    {
        var options = await postgres.CreateDatabaseAsync();

        await using var db = new AppDbContext(options);
        Assert.Equal(["20261116081722_AddAccountsRoomsAndDiagnostics"], await db.Database.GetAppliedMigrationsAsync());
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
    }

    [Fact]
    public async Task Development_startup_migrates_seeds_and_reports_the_database_healthy()
    {
        await using var factory = new DatabaseApiFactory(postgres.NewDatabaseConnectionString());
        using var client = factory.CreateClient();

        using var health = await client.GetAsync("/health");
        Assert.Equal("Healthy", await health.Content.ReadAsStringAsync());

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        var room = await db.Rooms.Include(r => r.Map).SingleAsync();
        Assert.Equal(Room.DefaultSlug, room.Slug);
        Assert.Equal(DbSeeder.OfficeMapName, room.Map.Name);
    }
}
