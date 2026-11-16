using System.Text.Json;

namespace PixelOffice.API.Features.Rooms;

/// <summary>A map definition that cannot be used for a room. The message says what is wrong.</summary>
public sealed class InvalidMapException(string message, Exception? innerException = null)
    : Exception(message, innerException);

/// <summary>
/// Reads a Tiled JSON map (.tmj) into a <see cref="CollisionGrid"/>.
/// <list type="bullet">
/// <item>Any non-empty tile in a tile layer named <c>walls</c> or <c>furniture</c> is blocked (BR-06).</item>
/// <item>The object layer named <c>spawn</c> holds exactly one object; the tile under it is the spawn point (BR-04).</item>
/// <item><c>floor</c> is drawn by the client only; <c>zones</c> is reserved for later features and ignored.</item>
/// </list>
/// Pure code, no I/O. Anything unusable throws <see cref="InvalidMapException"/>.
/// </summary>
public static class TiledMapParser
{
    /// <summary>Tile layers whose non-empty tiles block movement.</summary>
    public static readonly IReadOnlySet<string> BlockingLayers = new HashSet<string>(StringComparer.Ordinal) { "walls", "furniture" };

    public const string SpawnLayer = "spawn";

    public static CollisionGrid Parse(string tiledJson)
    {
        ArgumentNullException.ThrowIfNull(tiledJson);

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(tiledJson);
        }
        catch (JsonException ex)
        {
            throw new InvalidMapException("Map is not valid JSON.", ex);
        }

        using (document)
        {
            var map = document.RootElement;
            if (map.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidMapException("Map must be a JSON object.");
            }

            if (map.TryGetProperty("orientation", out var orientation)
                && (orientation.ValueKind != JsonValueKind.String || orientation.GetString() != "orthogonal"))
            {
                throw new InvalidMapException($"Map orientation must be \"orthogonal\", not \"{orientation}\".");
            }

            if (map.TryGetProperty("infinite", out var infinite) && infinite.ValueKind == JsonValueKind.True)
            {
                throw new InvalidMapException("Infinite maps are not supported; save the map with a fixed size.");
            }

            var width = PositiveInt(map, "width");
            var height = PositiveInt(map, "height");
            var tileWidth = PositiveInt(map, "tilewidth");
            var tileHeight = PositiveInt(map, "tileheight");

            if (!map.TryGetProperty("layers", out var layers) || layers.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidMapException("Map has no \"layers\" array.");
            }

            var blocked = new bool[width * height];
            var blockingLayersFound = 0;
            TilePosition? spawn = null;

            foreach (var layer in layers.EnumerateArray())
            {
                if (layer.ValueKind != JsonValueKind.Object)
                {
                    throw new InvalidMapException("Every entry in \"layers\" must be a layer object.");
                }

                var name = layer.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String ? n.GetString() : null;
                var type = layer.TryGetProperty("type", out var t) && t.ValueKind == JsonValueKind.String ? t.GetString() : null;

                if (name is not null && BlockingLayers.Contains(name))
                {
                    if (type != "tilelayer")
                    {
                        throw new InvalidMapException($"Layer \"{name}\" must be a tile layer.");
                    }

                    MarkBlocked(layer, name, width, height, blocked);
                    blockingLayersFound++;
                }
                else if (name == SpawnLayer)
                {
                    if (type != "objectgroup")
                    {
                        throw new InvalidMapException($"Layer \"{SpawnLayer}\" must be an object layer.");
                    }

                    spawn = ReadSpawn(layer, tileWidth, tileHeight);
                }
            }

            if (blockingLayersFound == 0)
            {
                throw new InvalidMapException("Map has no \"walls\" or \"furniture\" tile layer, so nothing would block movement.");
            }

            if (spawn is not { } spawnTile)
            {
                throw new InvalidMapException($"Map has no \"{SpawnLayer}\" object layer.");
            }

            if (spawnTile.X < 0 || spawnTile.Y < 0 || spawnTile.X >= width || spawnTile.Y >= height)
            {
                throw new InvalidMapException($"Spawn point at tile ({spawnTile.X}, {spawnTile.Y}) is outside the {width} x {height} map.");
            }

            if (blocked[(spawnTile.Y * width) + spawnTile.X])
            {
                throw new InvalidMapException($"Spawn point at tile ({spawnTile.X}, {spawnTile.Y}) is on a blocked tile.");
            }

            return new CollisionGrid(width, height, blocked, spawnTile);
        }
    }

    private static int PositiveInt(JsonElement map, string property)
    {
        if (!map.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.Number
            || !value.TryGetInt32(out var number) || number <= 0)
        {
            throw new InvalidMapException($"Map \"{property}\" must be a positive whole number.");
        }

        return number;
    }

    private static void MarkBlocked(JsonElement layer, string name, int width, int height, bool[] blocked)
    {
        if (layer.TryGetProperty("encoding", out var encoding)
            && (encoding.ValueKind != JsonValueKind.String || encoding.GetString() != "csv"))
        {
            throw new InvalidMapException($"Layer \"{name}\" uses \"{encoding}\" encoding; export tile layers as CSV.");
        }

        if (!layer.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidMapException($"Layer \"{name}\" has no tile data.");
        }

        if (data.GetArrayLength() != width * height)
        {
            throw new InvalidMapException(
                $"Layer \"{name}\" has {data.GetArrayLength()} tiles; a {width} x {height} map needs {width * height}.");
        }

        var index = 0;
        foreach (var tile in data.EnumerateArray())
        {
            // Tiled stores flip flags in the high bits of a gid, so read it as unsigned; 0 means empty.
            if (tile.ValueKind != JsonValueKind.Number || !tile.TryGetUInt32(out var gid))
            {
                throw new InvalidMapException($"Layer \"{name}\" has a tile value that is not a tile id at position {index}.");
            }

            if (gid != 0)
            {
                blocked[index] = true;
            }

            index++;
        }
    }

    private static TilePosition ReadSpawn(JsonElement layer, int tileWidth, int tileHeight)
    {
        if (!layer.TryGetProperty("objects", out var objects) || objects.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidMapException($"Layer \"{SpawnLayer}\" has no objects.");
        }

        if (objects.GetArrayLength() != 1)
        {
            throw new InvalidMapException($"Layer \"{SpawnLayer}\" must hold exactly one object; it holds {objects.GetArrayLength()}.");
        }

        var point = objects[0];
        if (point.ValueKind != JsonValueKind.Object
            || !point.TryGetProperty("x", out var x) || x.ValueKind != JsonValueKind.Number || !x.TryGetDouble(out var px)
            || !point.TryGetProperty("y", out var y) || y.ValueKind != JsonValueKind.Number || !y.TryGetDouble(out var py))
        {
            throw new InvalidMapException($"The \"{SpawnLayer}\" object has no x/y position.");
        }

        // Object positions are in pixels; the tile is the one containing the point.
        return new TilePosition((int)Math.Floor(px / tileWidth), (int)Math.Floor(py / tileHeight));
    }
}
