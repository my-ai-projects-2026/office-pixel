using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PixelOffice.API.Features.Rooms;

namespace PixelOffice.API.Data.Configurations;

internal sealed class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("Rooms", table => table.HasCheckConstraint(
            "CK_Rooms_MaxPlayers",
            $"\"{nameof(Room.MaxPlayers)}\" > 0"));

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.Slug).HasMaxLength(64).IsRequired();
        builder.HasIndex(r => r.Slug).IsUnique();

        builder.Property(r => r.Name).HasMaxLength(64).IsRequired();
        builder.Property(r => r.MaxPlayers).IsRequired();

        builder.HasOne(r => r.Map)
            .WithMany()
            .HasForeignKey(r => r.MapId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
