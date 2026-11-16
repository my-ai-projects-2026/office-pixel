using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PixelOffice.API.Features.Diagnostics;

namespace PixelOffice.API.Data.Configurations;

internal sealed class DiagnosticRecordConfiguration : IEntityTypeConfiguration<DiagnosticRecord>
{
    public void Configure(EntityTypeBuilder<DiagnosticRecord> builder)
    {
        builder.ToTable("DiagnosticRecords");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedOnAdd();

        // Stored as text so records stay readable when reviewed directly in the database.
        builder.Property(r => r.Type).HasConversion<string>().HasMaxLength(32).IsRequired();

        builder.Property(r => r.PlayerId).IsRequired();
        builder.Property(r => r.RoomId).IsRequired();
        builder.Property(r => r.OccurredAt).IsRequired();
        builder.Property(r => r.Detail).HasMaxLength(DiagnosticRecord.DetailMaxLength);

        // Retention purge deletes by age (BR-18, T20).
        builder.HasIndex(r => r.OccurredAt);

        // Reviewing one player's history, newest first.
        builder.HasIndex(r => new { r.PlayerId, r.OccurredAt });
    }
}
