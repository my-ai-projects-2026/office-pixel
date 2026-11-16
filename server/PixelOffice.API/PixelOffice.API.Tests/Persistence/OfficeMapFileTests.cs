using System.Text.Json;
using PixelOffice.API.Data;

namespace PixelOffice.API.Tests.Persistence;

// T05: the shipped office map has the agreed size and layers. Full parsing into a collision grid is T11.
public sealed class OfficeMapFileTests
{
    private static readonly JsonElement Map = JsonDocument.Parse(File.ReadAllText(DbSeeder.OfficeMapPath)).RootElement;

    [Fact]
    public void Office_map_is_30_by_20_tiles_of_32_pixels()
    {
        Assert.Equal(30, Map.GetProperty("width").GetInt32());
        Assert.Equal(20, Map.GetProperty("height").GetInt32());
        Assert.Equal(32, Map.GetProperty("tilewidth").GetInt32());
        Assert.Equal(32, Map.GetProperty("tileheight").GetInt32());
    }

    [Fact]
    public void Office_map_has_floor_walls_furniture_spawn_and_zones_layers()
    {
        var names = Map.GetProperty("layers").EnumerateArray().Select(l => l.GetProperty("name").GetString());
        Assert.Equal(["floor", "walls", "furniture", "spawn", "zones"], names);
    }

    [Fact]
    public void Office_map_has_exactly_one_spawn_point() // BR-04
    {
        var spawn = Map.GetProperty("layers").EnumerateArray().Single(l => l.GetProperty("name").GetString() == "spawn");
        var point = Assert.Single(spawn.GetProperty("objects").EnumerateArray());
        Assert.True(point.GetProperty("point").GetBoolean());
    }
}
