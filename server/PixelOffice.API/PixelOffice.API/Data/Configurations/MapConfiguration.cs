using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PixelOffice.API.Features.Rooms;

namespace PixelOffice.API.Data.Configurations;

internal sealed class MapConfiguration : IEntityTypeConfiguration<Map>
{
    public void Configure(EntityTypeBuilder<Map> builder)
    {
        builder.ToTable("Maps");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.Name).HasMaxLength(64).IsRequired();
        builder.HasIndex(m => m.Name).IsUnique();

        builder.Property(m => m.Version).IsRequired();
        builder.Property(m => m.ContentHash).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(m => m.TiledJson).HasColumnType("jsonb").IsRequired();
    }
}
