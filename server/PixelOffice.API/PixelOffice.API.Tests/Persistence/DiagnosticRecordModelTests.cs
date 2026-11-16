using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using PixelOffice.API.Data;
using PixelOffice.API.Features.Diagnostics;
using PixelOffice.API.Tests.Fixtures;

namespace PixelOffice.API.Tests.Persistence;

// T06: diagnostic records are indexed for the 30-day purge and for per-player review (BR-18).
public sealed class DiagnosticRecordModelTests
{
    private static IEntityType RecordEntity()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=unused.invalid;Database=pixeloffice")
            .Options;
        using var context = new AppDbContext(options);
        return context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(DiagnosticRecord))!;
    }

    [Fact]
    public void Occurred_at_has_its_own_index_for_the_retention_purge()
    {
        Assert.Contains(
            RecordEntity().GetIndexes(),
            i => i.Properties.Select(p => p.Name).SequenceEqual([nameof(DiagnosticRecord.OccurredAt)]));
    }

    [Fact]
    public void Player_and_occurred_at_have_a_combined_index_for_review()
    {
        Assert.Contains(
            RecordEntity().GetIndexes(),
            i => i.Properties.Select(p => p.Name)
                .SequenceEqual([nameof(DiagnosticRecord.PlayerId), nameof(DiagnosticRecord.OccurredAt)]));
    }

    [Fact]
    public void Type_is_stored_as_text()
    {
        var type = RecordEntity().FindProperty(nameof(DiagnosticRecord.Type))!;
        Assert.Equal(typeof(string), type.GetProviderClrType());
    }
}

[Collection(PostgresCollection.Name)]
public sealed class DiagnosticRecordStorageTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Records_round_trip_through_postgres_with_generated_ids()
    {
        var options = await postgres.CreateDatabaseAsync();
        var playerId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var at = new DateTimeOffset(2026, 11, 16, 9, 0, 0, TimeSpan.Zero);

        await using (var db = new AppDbContext(options))
        {
            db.DiagnosticRecords.AddRange(
                DiagnosticRecord.Create(DiagnosticRecordType.Joined, playerId, roomId, at),
                DiagnosticRecord.Create(DiagnosticRecordType.MoveRejected, playerId, roomId, at.AddSeconds(5), "Blocked x3"));
            await db.SaveChangesAsync();
        }

        await using (var db = new AppDbContext(options))
        {
            var records = await db.DiagnosticRecords.OrderBy(r => r.OccurredAt).ToListAsync();
            Assert.Equal(2, records.Count);
            Assert.All(records, r => Assert.True(r.Id > 0));
            Assert.Equal(DiagnosticRecordType.MoveRejected, records[1].Type);
            Assert.Equal("Blocked x3", records[1].Detail);
            Assert.Equal(at.AddSeconds(5), records[1].OccurredAt);
        }
    }
}
