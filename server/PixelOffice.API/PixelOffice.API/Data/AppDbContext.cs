using Microsoft.EntityFrameworkCore;
using PixelOffice.API.Features.Diagnostics;
using PixelOffice.API.Features.Players;
using PixelOffice.API.Features.Rooms;

namespace PixelOffice.API.Data;

// Persistent data only: accounts, avatar choice, room/map definition, diagnostic records (BR-13, BR-18).
// Live presence and positions are never stored here.
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Player> Players => Set<Player>();

    public DbSet<Map> Maps => Set<Map>();

    public DbSet<Room> Rooms => Set<Room>();

    public DbSet<DiagnosticRecord> DiagnosticRecords => Set<DiagnosticRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
