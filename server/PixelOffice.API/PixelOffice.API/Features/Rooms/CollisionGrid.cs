namespace PixelOffice.API.Features.Rooms;

/// <summary>The four directions an avatar can face and move in (BR-14). Sent to clients as 0–3.</summary>
public enum Facing
{
    Up = 0,
    Down = 1,
    Left = 2,
    Right = 3,
}

/// <summary>A tile on the map, counted from the top-left corner (0, 0).</summary>
public readonly record struct TilePosition(int X, int Y);

/// <summary>
/// Which tiles of a room's map can be walked on (BR-06), its size and boundary, and where players appear
/// (BR-04). Built once per map by <see cref="TiledMapParser"/>; immutable, so it can be shared by every
/// player in the room without locking.
/// </summary>
public sealed class CollisionGrid
{
    /// <summary>Direction a player faces when appearing at the spawn point (BR-04).</summary>
    public const Facing SpawnFacing = Facing.Down;

    private readonly bool[] blocked;

    public CollisionGrid(int width, int height, bool[] blocked, TilePosition spawn)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentNullException.ThrowIfNull(blocked);
        if (blocked.Length != width * height)
        {
            throw new ArgumentException($"Expected {width * height} tiles, got {blocked.Length}.", nameof(blocked));
        }

        Width = width;
        Height = height;
        this.blocked = (bool[])blocked.Clone();
        Spawn = spawn;

        if (!IsWalkable(spawn))
        {
            throw new ArgumentException($"Spawn tile ({spawn.X}, {spawn.Y}) must be inside the map and not blocked.", nameof(spawn));
        }
    }

    /// <summary>Map width in tiles.</summary>
    public int Width { get; }

    /// <summary>Map height in tiles.</summary>
    public int Height { get; }

    /// <summary>Where players appear on first entry or after the grace period (BR-04).</summary>
    public TilePosition Spawn { get; }

    /// <summary>True when the tile is inside the map boundary.</summary>
    public bool IsInBounds(TilePosition tile) =>
        tile.X >= 0 && tile.Y >= 0 && tile.X < Width && tile.Y < Height;

    /// <summary>True when the tile is a wall or furniture. Only meaningful inside the map.</summary>
    public bool IsBlocked(TilePosition tile) => IsInBounds(tile) && blocked[(tile.Y * Width) + tile.X];

    /// <summary>True when a player may stand on the tile: inside the map and not blocked (BR-06).</summary>
    public bool IsWalkable(TilePosition tile) => IsInBounds(tile) && !blocked[(tile.Y * Width) + tile.X];
}
