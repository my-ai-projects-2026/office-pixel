using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace PixelOffice.API.Features.Auth;

/// <summary>
/// The signed-in player's id comes only from the token's <c>sub</c> claim, never from what the client sends (BR-01).
/// Used by the API endpoints and, from T15, the hub.
/// </summary>
public static class PlayerIdentity
{
    public static bool TryGetPlayerId(this ClaimsPrincipal user, out Guid playerId)
    {
        playerId = Guid.Empty;
        return user.Identity?.IsAuthenticated == true
            && Guid.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out playerId);
    }
}
