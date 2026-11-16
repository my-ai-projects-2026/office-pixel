namespace PixelOffice.API.Features.Rooms;

/// <summary>
/// A tile map definition (spec "Data": map), stored as the Tiled JSON export.
/// Grid size, blocked tiles, spawn point and boundary are all read from that JSON (T11).
/// </summary>
public sealed class Map
{
    // For EF Core materialisation.
    private Map()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>Unique name, e.g. "office".</summary>
    public string Name { get; private set; } = null!;

    /// <summary>Starts at 1 and goes up each time the map file content changes.</summary>
    public int Version { get; private set; }

    /// <summary>SHA-256 (hex) of the map file the JSON came from; lets the seeder detect a changed file.</summary>
    public string ContentHash { get; private set; } = null!;

    /// <summary>Tiled map JSON (.tmj), stored as jsonb.</summary>
    public string TiledJson { get; private set; } = null!;

    public static Map Create(string name, string tiledJson, string contentHash, DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(tiledJson);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentHash);

        return new Map
        {
            Id = Guid.CreateVersion7(createdAt),
            Name = name,
            Version = 1,
            ContentHash = contentHash,
            TiledJson = tiledJson,
        };
    }

    /// <summary>Replaces the JSON and bumps <see cref="Version"/> when the content differs; otherwise does nothing.</summary>
    /// <returns>True when the map changed.</returns>
    public bool ReplaceContent(string tiledJson, string contentHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tiledJson);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentHash);

        if (string.Equals(ContentHash, contentHash, StringComparison.Ordinal))
        {
            return false;
        }

        TiledJson = tiledJson;
        ContentHash = contentHash;
        Version++;
        return true;
    }
}
