namespace PixelOffice.API.Features.Players;

/// <summary>Sign-in email rules (BR-19).</summary>
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
}
