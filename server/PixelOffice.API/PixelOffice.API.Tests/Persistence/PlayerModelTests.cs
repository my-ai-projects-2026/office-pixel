using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using PixelOffice.API.Data;
using PixelOffice.API.Features.Players;

namespace PixelOffice.API.Tests.Persistence;

// Checks the EF Core model for Player without a database (the migration and real-database tests come in T07).
public sealed class PlayerModelTests
{
    private static IEntityType PlayerEntity()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=unused.invalid;Database=pixeloffice")
            .Options;
        using var context = new AppDbContext(options);
        // The design-time model keeps everything migrations use (check constraints included).
        return context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(Player))!;
    }

    [Fact]
    public void Email_has_a_unique_index() // BR-19
    {
        var index = Assert.Single(PlayerEntity().GetIndexes(), i => i.Properties.Single().Name == nameof(Player.Email));
        Assert.True(index.IsUnique);
    }

    [Fact]
    public void Display_name_is_not_unique() // BR-16
    {
        Assert.DoesNotContain(
            PlayerEntity().GetIndexes(),
            i => i.IsUnique && i.Properties.Any(p => p.Name == nameof(Player.DisplayName)));
    }

    [Fact]
    public void Column_lengths_match_the_registration_limits() // BR-20
    {
        var entity = PlayerEntity();
        Assert.Equal(EmailAddress.MaxLength, entity.FindProperty(nameof(Player.Email))!.GetMaxLength());
        Assert.Equal(DisplayName.MaxLength, entity.FindProperty(nameof(Player.DisplayName))!.GetMaxLength());
    }

    [Fact]
    public void Avatar_id_is_limited_to_the_preset_range() // BR-17
    {
        var check = Assert.Single(PlayerEntity().GetCheckConstraints());
        Assert.Equal("\"AvatarId\" BETWEEN 1 AND 6", check.Sql);
    }
}
