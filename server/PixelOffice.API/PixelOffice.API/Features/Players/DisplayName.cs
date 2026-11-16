using System.Diagnostics.CodeAnalysis;

namespace PixelOffice.API.Features.Players;

/// <summary>Display name rules (BR-12, BR-16, BR-20). Names are not unique.</summary>
public static class DisplayName
{
    public const int MinLength = 1;

    public const int MaxLength = 24;

    /// <summary>
    /// Removes surrounding spaces and checks the length is 1–24 characters (BR-20).
    /// A name made only of spaces is treated as empty and refused.
    /// </summary>
    public static bool TryNormalize(string? displayName, [NotNullWhen(true)] out string? normalized)
    {
        var trimmed = displayName?.Trim();
        if (trimmed is { Length: >= MinLength and <= MaxLength })
        {
            normalized = trimmed;
            return true;
        }

        normalized = null;
        return false;
    }
}
