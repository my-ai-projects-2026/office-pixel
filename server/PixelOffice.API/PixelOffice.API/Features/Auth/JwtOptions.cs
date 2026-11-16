using System.ComponentModel.DataAnnotations;

namespace PixelOffice.API.Features.Auth;

/// <summary>Settings for the access tokens the API issues (section <c>Jwt</c>).</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>A sign-in lasts 12 hours (BR-23). Fixed by the spec, so not configurable.</summary>
    public static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(12);

    [Required]
    public string Issuer { get; set; } = "pixeloffice";

    [Required]
    public string Audience { get; set; } = "pixeloffice";

    /// <summary>
    /// HMAC-SHA256 signing key, at least 32 characters. Never kept in source: the AppHost generates one and
    /// stores it in its user secrets; tests set their own.
    /// </summary>
    [Required(ErrorMessage = "Jwt:SigningKey is not set. Run the API through the AppHost, or set Jwt__SigningKey.")]
    [MinLength(32, ErrorMessage = "Jwt:SigningKey must be at least 32 characters.")]
    public string SigningKey { get; set; } = string.Empty;
}
