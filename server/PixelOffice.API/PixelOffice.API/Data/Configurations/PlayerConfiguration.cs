using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PixelOffice.API.Features.Players;

namespace PixelOffice.API.Data.Configurations;

internal sealed class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.ToTable("Players", table => table.HasCheckConstraint(
            "CK_Players_AvatarId",
            $"\"{nameof(Player.AvatarId)}\" BETWEEN {AvatarCatalog.MinId} AND {AvatarCatalog.MaxId}"));

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Email).HasMaxLength(EmailAddress.MaxLength).IsRequired();

        // Emails are stored normalised (trimmed, lower case), so a plain unique index enforces BR-19,
        // including two registrations racing with the same email.
        builder.HasIndex(p => p.Email).IsUnique();

        builder.Property(p => p.PasswordHash).IsRequired();

        // Not unique: two players may share a name (BR-16).
        builder.Property(p => p.DisplayName).HasMaxLength(DisplayName.MaxLength).IsRequired();

        builder.Property(p => p.AvatarId).IsRequired();
        builder.Property(p => p.CreatedAt).IsRequired();
    }
}
