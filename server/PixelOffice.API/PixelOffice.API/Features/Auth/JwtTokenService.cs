using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using PixelOffice.API.Features.Players;

namespace PixelOffice.API.Features.Auth;

/// <summary>Issues access tokens. The player id is the token's <c>sub</c>; nothing else about the player is in it (BR-01).</summary>
public sealed class JwtTokenService(IOptions<JwtOptions> options, TimeProvider timeProvider)
{
    private readonly JsonWebTokenHandler handler = new() { SetDefaultTimesOnTokenCreation = false };

    public IssuedToken Issue(Player player)
    {
        var settings = options.Value;
        var now = timeProvider.GetUtcNow();
        var expiresAt = now + JwtOptions.TokenLifetime;

        var token = handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            Subject = new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, player.Id.ToString())]),
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(SigningKey(settings), SecurityAlgorithms.HmacSha256),
        });

        return new IssuedToken(token, expiresAt);
    }

    internal static SymmetricSecurityKey SigningKey(JwtOptions settings) =>
        new(Encoding.UTF8.GetBytes(settings.SigningKey));
}

public sealed record IssuedToken(string AccessToken, DateTimeOffset ExpiresAt);
