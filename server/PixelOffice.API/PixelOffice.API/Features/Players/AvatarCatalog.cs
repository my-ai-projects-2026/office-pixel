namespace PixelOffice.API.Features.Players;

/// <summary>One preset look a player can choose (BR-17).</summary>
/// <param name="Id">Stable id stored on the player account.</param>
/// <param name="Key">Name the client uses to find the sprite sheet in <c>public/assets</c>.</param>
public sealed record AvatarOption(int Id, string Key);

/// <summary>
/// The fixed set of 6 preset avatars (BR-17). Kept in code, not in the database: the set is fixed in
/// this slice and must match the client's sprite sheets (plan.md, open point 4).
/// </summary>
public static class AvatarCatalog
{
    /// <summary>Look every new account starts with (BR-22).</summary>
    public const int DefaultAvatarId = 1;

    public static IReadOnlyList<AvatarOption> All { get; } =
    [
        new(1, "avatar-1"),
        new(2, "avatar-2"),
        new(3, "avatar-3"),
        new(4, "avatar-4"),
        new(5, "avatar-5"),
        new(6, "avatar-6"),
    ];

    public static int MinId => All[0].Id;

    public static int MaxId => All[^1].Id;

    public static bool Contains(int avatarId) => All.Any(a => a.Id == avatarId);
}
