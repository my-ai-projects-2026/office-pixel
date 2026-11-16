namespace PixelOffice.API.Features.Players;

/// <summary>
/// A player account (spec "Data": player account). Persistent; never holds live position (BR-13).
/// </summary>
public sealed class Player
{
    // For EF Core materialisation.
    private Player()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>Normalised sign-in email, unique per account (BR-19). Never shown to other players.</summary>
    public string Email { get; private set; } = null!;

    /// <summary>Password in protected form only; the readable password is never stored.</summary>
    public string PasswordHash { get; private set; } = null!;

    /// <summary>Trimmed, 1–24 characters, not unique (BR-16, BR-20).</summary>
    public string DisplayName { get; private set; } = null!;

    /// <summary>One of <see cref="AvatarCatalog"/> (BR-17).</summary>
    public int AvatarId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Creates a new account with the default avatar (BR-22). Email is normalised (BR-19); the display
    /// name must already satisfy <see cref="Players.DisplayName.TryNormalize"/> (registration validates it first).
    /// </summary>
    public static Player Create(string email, string passwordHash, string displayName, DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        if (!Players.DisplayName.TryNormalize(displayName, out var name))
        {
            throw new ArgumentException(
                $"Display name must be {Players.DisplayName.MinLength}–{Players.DisplayName.MaxLength} characters after trimming.",
                nameof(displayName));
        }

        return new Player
        {
            Id = Guid.CreateVersion7(createdAt),
            Email = EmailAddress.Normalize(email),
            PasswordHash = passwordHash,
            DisplayName = name,
            AvatarId = AvatarCatalog.DefaultAvatarId,
            CreatedAt = createdAt,
        };
    }

    /// <summary>
    /// Switches to another preset look. Returns false, and keeps the current look, when the id is not
    /// in the preset set (BR-17; spec edge case "avatar choice that is not in the preset set").
    /// </summary>
    public bool TryChangeAvatar(int avatarId)
    {
        if (!AvatarCatalog.Contains(avatarId))
        {
            return false;
        }

        AvatarId = avatarId;
        return true;
    }
}
