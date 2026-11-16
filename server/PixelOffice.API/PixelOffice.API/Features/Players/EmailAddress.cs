using System.Net.Mail;

namespace PixelOffice.API.Features.Players;

/// <summary>Sign-in email rules (BR-19, BR-20).</summary>
public static class EmailAddress
{
    /// <summary>Longest email accepted at registration (BR-20).</summary>
    public const int MaxLength = 254;

    /// <summary>
    /// Canonical form used for storage and comparison: surrounding spaces removed, lower case (BR-19).
    /// " Ana@Example.com " and "ana@example.com" are the same account.
    /// </summary>
    public static string Normalize(string email)
    {
        ArgumentNullException.ThrowIfNull(email);
        return email.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// True when a normalised email looks like an address (BR-20): at most 254 characters, one plain
    /// <c>local@domain</c> address with no display name, and a dot inside the domain.
    /// </summary>
    public static bool IsValid(string normalizedEmail) =>
        normalizedEmail.Length is > 0 and <= MaxLength
        && MailAddress.TryCreate(normalizedEmail, out var address)
        && string.Equals(address.Address, normalizedEmail, StringComparison.Ordinal)
        && address.Host.Contains('.', StringComparison.Ordinal)
        && !address.Host.StartsWith('.')
        && !address.Host.EndsWith('.');
}
