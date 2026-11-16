namespace PixelOffice.API.Features.Diagnostics;

/// <summary>What a diagnostic record is about (BR-18; spec "Data": diagnostic record).</summary>
public enum DiagnosticRecordType
{
    Joined,
    Left,
    GraceExpired,
    SessionReplaced,
    MoveRejected,
}

/// <summary>
/// One join, leave, replaced session or rejected movement, kept 30 days for abuse review and troubleshooting
/// (BR-18). Never holds a position, a password or an email.
/// </summary>
/// <remarks>
/// No foreign keys to players or rooms on purpose: this is an append-only log written in batches (T19)
/// and purged by age (T20), and a write must never fail because of the state of other tables.
/// </remarks>
public sealed class DiagnosticRecord
{
    /// <summary>Longest <see cref="Detail"/> kept; longer text is cut.</summary>
    public const int DetailMaxLength = 256;

    // For EF Core materialisation.
    private DiagnosticRecord()
    {
    }

    /// <summary>Database-generated; records are only ever added and purged.</summary>
    public long Id { get; private set; }

    public DiagnosticRecordType Type { get; private set; }

    public Guid PlayerId { get; private set; }

    public Guid RoomId { get; private set; }

    /// <summary>When it happened, UTC. Retention counts 30 days from this (BR-18).</summary>
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>Short reason, e.g. a rejection reason and count. Optional.</summary>
    public string? Detail { get; private set; }

    public static DiagnosticRecord Create(
        DiagnosticRecordType type,
        Guid playerId,
        Guid roomId,
        DateTimeOffset occurredAt,
        string? detail = null)
    {
        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown diagnostic record type.");
        }

        var trimmed = string.IsNullOrWhiteSpace(detail) ? null : detail.Trim();

        return new DiagnosticRecord
        {
            Type = type,
            PlayerId = playerId,
            RoomId = roomId,
            OccurredAt = occurredAt.ToUniversalTime(),
            Detail = trimmed is { Length: > DetailMaxLength } ? trimmed[..DetailMaxLength] : trimmed,
        };
    }
}
