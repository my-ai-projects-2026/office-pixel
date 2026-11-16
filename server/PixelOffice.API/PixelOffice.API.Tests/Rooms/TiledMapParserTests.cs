using System.Text.Json.Nodes;
using PixelOffice.API.Data;
using PixelOffice.API.Features.Rooms;
using PixelOffice.API.Tests.Fixtures;

namespace PixelOffice.API.Tests.Rooms;

// T11: the map is read into a collision grid: blocked tiles and boundary (BR-06), spawn and default facing (BR-04).
public sealed class TiledMapParserTests
{
    private static readonly CollisionGrid Office = TiledMapParser.Parse(File.ReadAllText(DbSeeder.OfficeMapPath));

    [Fact]
    public void Office_map_is_30_by_20()
    {
        Assert.Equal(30, Office.Width);
        Assert.Equal(20, Office.Height);
    }

    [Fact]
    public void Office_spawn_is_bottom_centre_facing_down() // BR-04
    {
        Assert.Equal(new TilePosition(15, 16), Office.Spawn);
        Assert.Equal(Facing.Down, CollisionGrid.SpawnFacing);
        Assert.True(Office.IsWalkable(Office.Spawn));
    }

    [Theory]
    [InlineData(0, 0)]   // wall, top-left corner
    [InlineData(15, 0)]  // wall, top edge
    [InlineData(0, 10)]  // wall, left edge
    [InlineData(29, 19)] // wall, bottom-right corner
    [InlineData(4, 4)]   // desk, first cluster
    [InlineData(7, 5)]   // desk, first cluster's far corner
    [InlineData(25, 11)] // desk, last cluster
    [InlineData(1, 1)]   // plant
    public void Office_walls_desks_and_plants_block_movement(int x, int y) // BR-06
    {
        Assert.True(Office.IsBlocked(new TilePosition(x, y)));
        Assert.False(Office.IsWalkable(new TilePosition(x, y)));
    }

    [Theory]
    [InlineData(2, 1)]   // next to the plant
    [InlineData(8, 4)]   // aisle between desk clusters
    [InlineData(4, 6)]   // aisle below the first desk row
    [InlineData(28, 2)]  // inside the right wall
    public void Office_floor_tiles_are_walkable(int x, int y)
    {
        Assert.True(Office.IsWalkable(new TilePosition(x, y)));
    }

    [Theory]
    [InlineData(-1, 5)]
    [InlineData(5, -1)]
    [InlineData(30, 5)]
    [InlineData(5, 20)]
    public void Tiles_outside_the_map_are_not_walkable(int x, int y) // BR-06 boundary
    {
        var tile = new TilePosition(x, y);
        Assert.False(Office.IsInBounds(tile));
        Assert.False(Office.IsWalkable(tile));
    }

    [Fact]
    public void Small_fixture_map_marks_walls_and_furniture_and_finds_the_spawn()
    {
        var grid = TiledMapParser.Parse(TestMaps.Build(
        [
            "#####",
            "#.D.#",
            "#.S.#",
            "#####",
        ]));

        Assert.Equal((5, 4), (grid.Width, grid.Height));
        Assert.Equal(new TilePosition(2, 2), grid.Spawn);
        Assert.True(grid.IsBlocked(new TilePosition(2, 1)));  // furniture
        Assert.True(grid.IsBlocked(new TilePosition(0, 0)));  // wall
        Assert.True(grid.IsWalkable(new TilePosition(1, 1))); // floor
        Assert.True(grid.IsWalkable(new TilePosition(3, 2))); // floor
    }

    [Fact]
    public void Flipped_tiles_still_count_as_blocked()
    {
        // Tiled stores flip flags in the top bits of a tile id, making it larger than a signed int.
        var grid = TiledMapParser.Parse(TestMaps.Build(["#S.#"], map =>
            TestMaps.Layer(map, "furniture")["data"]![2] = 2147483651u)); // id 3, flipped horizontally

        Assert.True(grid.IsBlocked(new TilePosition(2, 0)));
    }

    [Fact]
    public void Spawn_tile_is_the_one_containing_the_spawn_point()
    {
        // A point on the far edge of a tile (x = 63.9 px) is still in tile 1.
        var grid = TiledMapParser.Parse(TestMaps.Build(["#S.#"], map =>
            TestMaps.Layer(map, "spawn")["objects"]![0]!["x"] = 63.9));

        Assert.Equal(new TilePosition(1, 0), grid.Spawn);
    }

    public static TheoryData<string, string> BrokenMaps => new()
    {
        { "not json at all", "not valid JSON" },
        { "[1, 2, 3]", "must be a JSON object" },
        { Map(m => m.Remove("width")), "\"width\" must be a positive whole number" },
        { Map(m => m["height"] = 0), "\"height\" must be a positive whole number" },
        { Map(m => m["tilewidth"] = "32"), "\"tilewidth\" must be a positive whole number" },
        { Map(m => m["orientation"] = "isometric"), "orthogonal" },
        { Map(m => m["orientation"] = 1), "orthogonal" },
        { Map(m => m["layers"]!.AsArray().Add(5)), "must be a layer object" },
        { Map(m => TestMaps.Layer(m, "furniture")["data"]![0] = "3"), "not a tile id" },
        { Map(m => TestMaps.Layer(m, "spawn")["objects"]![0]!["x"] = "48"), "no x/y position" },
        { Map(m => m["infinite"] = true), "Infinite maps are not supported" },
        { Map(m => m.Remove("layers")), "no \"layers\" array" },
        { Map(m => TestMaps.Layer(m, "walls")["data"]!.AsArray().RemoveAt(0)), "Layer \"walls\" has 11 tiles; a 4 x 3 map needs 12" },
        { Map(m => TestMaps.Layer(m, "walls")["encoding"] = "base64"), "export tile layers as CSV" },
        { Map(m => TestMaps.Layer(m, "furniture")["data"]![0] = -1), "not a tile id" },
        { Map(m => TestMaps.Layer(m, "walls")["type"] = "objectgroup"), "Layer \"walls\" must be a tile layer" },
        { Map(m => RemoveLayers(m, "walls", "furniture")), "no \"walls\" or \"furniture\" tile layer" },
        { Map(m => RemoveLayers(m, "spawn")), "no \"spawn\" object layer" },
        { Map(m => TestMaps.Layer(m, "spawn")["objects"]!.AsArray().Clear()), "exactly one object; it holds 0" },
        { Map(m => TestMaps.Layer(m, "spawn")["objects"]!.AsArray().Add(TestMaps.Layer(m, "spawn")["objects"]![0]!.DeepClone())), "exactly one object; it holds 2" },
        { Map(m => TestMaps.Layer(m, "spawn")["objects"]![0]!["x"] = 16), "on a blocked tile" },
        { Map(m => TestMaps.Layer(m, "spawn")["objects"]![0]!["x"] = 500), "outside the 4 x 3 map" },
    };

    [Theory]
    [MemberData(nameof(BrokenMaps))]
    public void Broken_maps_are_refused_with_a_message_saying_what_is_wrong(string tiledJson, string expectedMessage)
    {
        var error = Assert.Throws<InvalidMapException>(() => TiledMapParser.Parse(tiledJson));

        Assert.Contains(expectedMessage, error.Message, StringComparison.Ordinal);
    }

    // A valid 4 x 3 map with one spawn point, changed by the test case.
    private static string Map(Action<JsonObject> breakIt) => TestMaps.Build(["####", "#S.#", "####"], breakIt);

    private static void RemoveLayers(JsonObject map, params string[] names)
    {
        var layers = map["layers"]!.AsArray();
        foreach (var layer in layers.Where(l => names.Contains((string?)l!["name"])).ToList())
        {
            layers.Remove(layer);
        }
    }
}
