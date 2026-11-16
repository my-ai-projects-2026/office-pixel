using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PixelOffice.API.Data;
using PixelOffice.API.Features.Auth;

namespace PixelOffice.API.Features.Players;

public sealed record SetAvatarRequest(int AvatarId);

/// <summary>The signed-in player's profile and avatar choice (BR-12, BR-17).</summary>
public static class PlayerEndpoints
{
    internal const string AvatarMessage = "Choose one of the preset avatars.";

    public static IEndpointRouteBuilder MapPlayerEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/avatars", () => TypedResults.Ok(AvatarCatalog.All)).AllowAnonymous();

        var me = app.MapGroup("/api/me").RequireAuthorization();
        me.MapGet("/", GetMeAsync);
        me.MapPut("/avatar", SetAvatarAsync);
        return app;
    }

    private static async Task<IResult> GetMeAsync(ClaimsPrincipal user, AppDbContext db, CancellationToken cancellationToken)
    {
        if (!user.TryGetPlayerId(out var playerId))
        {
            return TypedResults.Unauthorized();
        }

        var profile = await db.Players.AsNoTracking()
            .Where(p => p.Id == playerId)
            .Select(p => new PlayerProfile(p.Id, p.DisplayName, p.AvatarId))
            .SingleOrDefaultAsync(cancellationToken);

        // A valid token for an account that no longer exists: the player has to sign in again.
        return profile is null ? TypedResults.Unauthorized() : TypedResults.Ok(profile);
    }

    private static async Task<IResult> SetAvatarAsync(
        SetAvatarRequest request,
        ClaimsPrincipal user,
        AppDbContext db,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(typeof(PlayerEndpoints));
        if (!user.TryGetPlayerId(out var playerId))
        {
            return TypedResults.Unauthorized();
        }

        // BR-17: only a preset look; anything else is refused and the current look is kept.
        if (!AvatarCatalog.Contains(request.AvatarId))
        {
            logger.LogInformation("Avatar change refused for player {PlayerId}: not a preset", playerId);
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["avatarId"] = [AvatarMessage] });
        }

        var updated = await db.Players
            .Where(p => p.Id == playerId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.AvatarId, request.AvatarId), cancellationToken);
        if (updated == 0)
        {
            return TypedResults.Unauthorized();
        }

        // Telling players already in the room (BR-17, AC-22) is added with the room in T18.
        logger.LogInformation("Player {PlayerId} chose avatar {AvatarId}", playerId, request.AvatarId);
        return TypedResults.NoContent();
    }
}
