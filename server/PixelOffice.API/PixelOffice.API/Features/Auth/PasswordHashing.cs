using Microsoft.AspNetCore.Identity;

namespace PixelOffice.API.Features.Auth;

/// <summary>
/// Wraps ASP.NET Core's PBKDF2 password hasher. Passwords are only ever stored in this protected form.
/// </summary>
public sealed class PasswordHashing
{
    private static readonly object NoUser = new();
    private readonly PasswordHasher<object> hasher = new();

    /// <summary>A hash of a random value, checked when the email is unknown so both failures take similar time (BR-21).</summary>
    private readonly string dummyHash;

    public PasswordHashing() => dummyHash = Hash(Guid.NewGuid().ToString("N"));

    public string Hash(string password) => hasher.HashPassword(NoUser, password);

    public PasswordCheck Verify(string hash, string password) =>
        hasher.VerifyHashedPassword(NoUser, hash, password) switch
        {
            PasswordVerificationResult.Success => PasswordCheck.Valid,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordCheck.ValidButRehash,
            _ => PasswordCheck.Invalid,
        };

    /// <summary>Spends the same work as a real check, for an email with no account.</summary>
    public void VerifyAgainstDummy(string password) => Verify(dummyHash, password);
}

public enum PasswordCheck
{
    Invalid,
    Valid,
    ValidButRehash,
}
