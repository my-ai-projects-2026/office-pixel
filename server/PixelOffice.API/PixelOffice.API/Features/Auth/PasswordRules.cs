namespace PixelOffice.API.Features.Auth;

/// <summary>Password length rule (BR-20).</summary>
public static class PasswordRules
{
    public const int MinLength = 8;

    public const int MaxLength = 128;

    public static bool IsValid(string? password) => password is { Length: >= MinLength and <= MaxLength };
}
