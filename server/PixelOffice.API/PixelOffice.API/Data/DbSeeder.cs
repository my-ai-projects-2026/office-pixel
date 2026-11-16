using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PixelOffice.API.Features.Rooms;

namespace PixelOffice.API.Data;

/// <summary>
/// Creates the default room and its office map if they are missing, and refreshes the map when
/// <c>Assets/maps/office.tmj</c> changes. Safe to run on every start: a second run changes nothing.
/// Startup wiring (after migrations, Development only) comes with the first migration in T07.
/// </summary>
public sealed class DbSeeder(AppDbContext db, TimeProvider timeProvider, ILogger<DbSeeder> logger)
{
    public const string OfficeMapName = "office";

    public const string DefaultRoomName = "Office";

    /// <summary>Path of the office map, copied next to the API assembly at build time.</summary>
    public static string OfficeMapPath { get; } = Path.Combine(AppContext.BaseDirectory, "Assets", "maps", "office.tmj");

    /// <summary>Seeds from the office map file.</summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var officeMapJson = await File.ReadAllTextAsync(OfficeMapPath, cancellationToken);
        await SeedAsync(officeMapJson, cancellationToken);
    }

    /// <summary>Seeds from the given office map JSON (lets tests simulate a changed file).</summary>
    public async Task SeedAsync(string officeMapJson, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(officeMapJson);

        var now = timeProvider.GetUtcNow();
        var contentHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(officeMapJson)));

        var map = await db.Maps.SingleOrDefaultAsync(m => m.Name == OfficeMapName, cancellationToken);
        if (map is null)
        {
            map = Map.Create(OfficeMapName, officeMapJson, contentHash, now);
            db.Maps.Add(map);
            logger.LogInformation("Seeded map {MapName} version {MapVersion}", map.Name, map.Version);
        }
        else if (map.ReplaceContent(officeMapJson, contentHash))
        {
            logger.LogInformation("Updated map {MapName} to version {MapVersion}", map.Name, map.Version);
        }

        var roomExists = await db.Rooms.AnyAsync(r => r.Slug == Room.DefaultSlug, cancellationToken);
        if (!roomExists)
        {
            db.Rooms.Add(Room.Create(Room.DefaultSlug, DefaultRoomName, map, now));
            logger.LogInformation("Seeded room {RoomSlug}", Room.DefaultSlug);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
