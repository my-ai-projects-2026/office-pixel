using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using PixelOffice.API.Data;
using PixelOffice.API.Features.Rooms;
using PixelOffice.API.Tests.Fixtures;

namespace PixelOffice.API.Tests.Persistence;

// T05: the default room and office map are created once; re-running the seeder changes nothing,
// and a changed map file bumps the map version.
[Collection(PostgresCollection.Name)]
public sealed class DbSeederTests(PostgresFixture postgres)
{
    private readonly FakeTimeProvider clock = new(new DateTimeOffset(2026, 11, 16, 9, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Seeding_twice_leaves_exactly_one_default_room_and_one_map()
    {
        var options = await postgres.CreateDatabaseAsync();

        await SeedFromFileAsync(options);
        await SeedFromFileAsync(options);

        await using var db = new AppDbContext(options);
        var map = Assert.Single(await db.Maps.ToListAsync());
        var room = Assert.Single(await db.Rooms.ToListAsync());
        Assert.Equal(Room.DefaultSlug, room.Slug);
        Assert.Equal(map.Id, room.MapId);
        Assert.Equal(1, map.Version);
    }

    [Fact]
    public async Task Default_room_holds_ten_players_and_uses_the_office_map() // BR-02
    {
        var options = await postgres.CreateDatabaseAsync();

        await SeedFromFileAsync(options);

        await using var db = new AppDbContext(options);
        var room = await db.Rooms.Include(r => r.Map).SingleAsync(r => r.Slug == Room.DefaultSlug);
        Assert.Equal(10, room.MaxPlayers);
        Assert.Equal(DbSeeder.OfficeMapName, room.Map.Name);
        Assert.Contains("\"width\":30", room.Map.TiledJson.Replace(" ", string.Empty, StringComparison.Ordinal));
    }

    [Fact]
    public async Task A_changed_map_file_replaces_the_json_and_bumps_the_version()
    {
        var options = await postgres.CreateDatabaseAsync();
        const string original = """{"width":30,"height":20,"layers":[]}""";
        const string changed = """{"width":30,"height":20,"layers":[],"nextobjectid":2}""";

        await SeedAsync(options, original);
        await SeedAsync(options, original);
        await SeedAsync(options, changed);

        await using var db = new AppDbContext(options);
        var map = Assert.Single(await db.Maps.ToListAsync());
        Assert.Equal(2, map.Version);
        Assert.Contains("nextobjectid", map.TiledJson, StringComparison.Ordinal);
        Assert.Single(await db.Rooms.ToListAsync());
    }

    private async Task SeedFromFileAsync(DbContextOptions<AppDbContext> options)
    {
        await using var db = new AppDbContext(options);
        await new DbSeeder(db, clock, NullLogger<DbSeeder>.Instance).SeedAsync();
    }

    private async Task SeedAsync(DbContextOptions<AppDbContext> options, string mapJson)
    {
        await using var db = new AppDbContext(options);
        await new DbSeeder(db, clock, NullLogger<DbSeeder>.Instance).SeedAsync(mapJson);
    }
}
