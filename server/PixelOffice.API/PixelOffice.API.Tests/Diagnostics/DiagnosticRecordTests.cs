using PixelOffice.API.Features.Diagnostics;

namespace PixelOffice.API.Tests.Diagnostics;

// BR-18: diagnostic records for joins, leaves, replaced sessions and rejected movements.
public sealed class DiagnosticRecordTests
{
    private static readonly Guid PlayerId = Guid.NewGuid();
    private static readonly Guid RoomId = Guid.NewGuid();

    [Fact]
    public void Occurred_at_is_stored_in_utc()
    {
        var local = new DateTimeOffset(2026, 11, 16, 17, 0, 0, TimeSpan.FromHours(8));

        var record = DiagnosticRecord.Create(DiagnosticRecordType.Joined, PlayerId, RoomId, local);

        Assert.Equal(TimeSpan.Zero, record.OccurredAt.Offset);
        Assert.Equal(local, record.OccurredAt);
    }

    [Fact]
    public void Detail_longer_than_the_limit_is_cut()
    {
        var record = DiagnosticRecord.Create(
            DiagnosticRecordType.MoveRejected, PlayerId, RoomId, DateTimeOffset.UtcNow, new string('x', 300));

        Assert.Equal(DiagnosticRecord.DetailMaxLength, record.Detail!.Length);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_detail_is_stored_as_none(string? detail)
    {
        var record = DiagnosticRecord.Create(DiagnosticRecordType.Left, PlayerId, RoomId, DateTimeOffset.UtcNow, detail);

        Assert.Null(record.Detail);
    }

    [Fact]
    public void Unknown_type_is_refused()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DiagnosticRecord.Create((DiagnosticRecordType)99, PlayerId, RoomId, DateTimeOffset.UtcNow));
    }
}
