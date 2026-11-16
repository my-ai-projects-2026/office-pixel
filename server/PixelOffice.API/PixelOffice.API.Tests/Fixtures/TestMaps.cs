using System.Text.Json;
using System.Text.Json.Nodes;

namespace PixelOffice.API.Tests.Fixtures;

/// <summary>
/// Builds small Tiled JSON maps from text pictures, one string per row:
/// <c>.</c> floor, <c>#</c> wall, <c>D</c> furniture, <c>S</c> spawn (on floor).
/// </summary>
public static class TestMaps
{
    public const int TileSize = 32;

    /// <summary>Tiled JSON for the picture. Use <paramref name="tweak"/> to break or change it for a test.</summary>
    public static string Build(string[] rows, Action<JsonObject>? tweak = null)
    {
        var map = BuildNode(rows);
        tweak?.Invoke(map);
        return map.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    public static JsonObject BuildNode(string[] rows)
    {
        var height = rows.Length;
        var width = rows[0].Length;
        if (rows.Any(r => r.Length != width))
        {
            throw new ArgumentException("All rows must be the same length.", nameof(rows));
        }

        var walls = new JsonArray();
        var furniture = new JsonArray();
        var floor = new JsonArray();
        var spawnObjects = new JsonArray();
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var c = rows[y][x];
                floor.Add(1);
                walls.Add(c == '#' ? 2 : 0);
                furniture.Add(c == 'D' ? 3 : 0);
                if (c == 'S')
                {
                    spawnObjects.Add(new JsonObject
                    {
                        ["id"] = 1, ["name"] = "spawn", ["point"] = true,
                        ["x"] = (x * TileSize) + (TileSize / 2), ["y"] = (y * TileSize) + (TileSize / 2),
                        ["width"] = 0, ["height"] = 0, ["rotation"] = 0, ["type"] = "", ["visible"] = true,
                    });
                }
            }
        }

        return new JsonObject
        {
            ["type"] = "map", ["orientation"] = "orthogonal", ["renderorder"] = "right-down", ["infinite"] = false,
            ["width"] = width, ["height"] = height, ["tilewidth"] = TileSize, ["tileheight"] = TileSize,
            ["layers"] = new JsonArray
            {
                TileLayer(1, "floor", width, height, floor),
                TileLayer(2, "walls", width, height, walls),
                TileLayer(3, "furniture", width, height, furniture),
                new JsonObject { ["id"] = 4, ["name"] = "spawn", ["type"] = "objectgroup", ["objects"] = spawnObjects },
                new JsonObject { ["id"] = 5, ["name"] = "zones", ["type"] = "objectgroup", ["objects"] = new JsonArray() },
            },
        };
    }

    /// <summary>The named layer of a map node, for tweaks.</summary>
    public static JsonObject Layer(JsonObject map, string name) =>
        map["layers"]!.AsArray().Select(l => l!.AsObject()).Single(l => (string?)l["name"] == name);

    private static JsonObject TileLayer(int id, string name, int width, int height, JsonArray data) => new()
    {
        ["id"] = id, ["name"] = name, ["type"] = "tilelayer", ["width"] = width, ["height"] = height,
        ["x"] = 0, ["y"] = 0, ["opacity"] = 1, ["visible"] = true, ["data"] = data,
    };
}
