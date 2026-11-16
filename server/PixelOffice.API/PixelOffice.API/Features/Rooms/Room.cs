namespace PixelOffice.API.Features.Rooms;

/// <summary>A room players enter (spec "Data": room). This slice has exactly one: <see cref="DefaultSlug"/>.</summary>
public sealed class Room
{
    /// <summary>Slug of the single shared room in this slice.</summary>
    public const string DefaultSlug = "default";

    /// <summary>Room capacity (BR-02).</summary>
    public const int DefaultMaxPlayers = 10;

    // For EF Core materialisation.
    private Room()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>Unique, URL-safe identifier.</summary>
    public string Slug { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    /// <summary>Most players present at once, counting those in the reconnect grace period (BR-02, BR-10).</summary>
    public int MaxPlayers { get; private set; }

    public Guid MapId { get; private set; }

    public Map Map { get; private set; } = null!;

    public static Room Create(string slug, string name, Map map, DateTimeOffset createdAt, int maxPlayers = DefaultMaxPlayers)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(map);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxPlayers);

        return new Room
        {
            Id = Guid.CreateVersion7(createdAt),
            Slug = slug,
            Name = name,
            MaxPlayers = maxPlayers,
            MapId = map.Id,
            Map = map,
        };
    }
}
