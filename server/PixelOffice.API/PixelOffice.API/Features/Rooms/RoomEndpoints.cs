using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PixelOffice.API.Data;

namespace PixelOffice.API.Features.Rooms;

/// <summary>
/// What the client needs to draw a room (US-04): its name, capacity and Tiled map.
/// <paramref name="Map"/> is the Tiled JSON as an object, not a string, so the client can use it directly.
/// </summary>
public sealed record RoomDefinition(string Slug, string Name, int MaxPlayers, int MapVersion, JsonElement Map);

public static class RoomEndpoints
{
    public static IEndpointRouteBuilder MapRoomEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/rooms/default", GetDefaultRoomAsync).RequireAuthorization();
        return app;
    }

    private static async Task<IResult> GetDefaultRoomAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var room = await db.Rooms.AsNoTracking()
            .Where(r => r.Slug == Room.DefaultSlug)
            .Select(r => new { r.Slug, r.Name, r.MaxPlayers, r.Map.Version, r.Map.TiledJson })
            .SingleOrDefaultAsync(cancellationToken);

        // The seeder creates the default room on startup; missing means the database was not seeded.
        if (room is null)
        {
            return TypedResults.Problem(
                title: "Room not available",
                detail: "The default room has not been set up yet.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        using var map = JsonDocument.Parse(room.TiledJson);
        return TypedResults.Ok(new RoomDefinition(room.Slug, room.Name, room.MaxPlayers, room.Version, map.RootElement.Clone()));
    }
}
