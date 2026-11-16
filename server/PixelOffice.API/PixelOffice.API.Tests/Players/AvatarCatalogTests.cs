using PixelOffice.API.Features.Players;

namespace PixelOffice.API.Tests.Players;

// BR-17: a fixed set of 6 preset looks; BR-22: new accounts start with the first.
public sealed class AvatarCatalogTests
{
    [Fact]
    public void Catalogue_has_six_presets_with_ids_1_to_6_and_unique_keys()
    {
        Assert.Equal([1, 2, 3, 4, 5, 6], AvatarCatalog.All.Select(a => a.Id));
        Assert.Equal(6, AvatarCatalog.All.Select(a => a.Key).Distinct().Count());
    }

    [Fact]
    public void Default_avatar_is_the_first_preset()
    {
        Assert.Equal(AvatarCatalog.DefaultAvatarId, AvatarCatalog.All[0].Id);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(6, true)]
    [InlineData(0, false)]
    [InlineData(7, false)]
    [InlineData(-1, false)]
    [InlineData(int.MaxValue, false)]
    public void Membership_check_accepts_only_preset_ids(int avatarId, bool expected)
    {
        Assert.Equal(expected, AvatarCatalog.Contains(avatarId));
    }
}
