using PixelOffice.API.Features.Players;

namespace PixelOffice.API.Tests.Players;

public sealed class PlayerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 16, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void New_player_starts_with_the_default_avatar() // BR-22
    {
        var player = Player.Create("ana@example.com", "hash", "Ana", Now);

        Assert.Equal(AvatarCatalog.DefaultAvatarId, player.AvatarId);
    }

    [Fact]
    public void New_player_stores_normalised_email_and_trimmed_name() // BR-19, BR-20
    {
        var player = Player.Create(" Ana@Example.com ", "hash", "  Ana  ", Now);

        Assert.Equal("ana@example.com", player.Email);
        Assert.Equal("Ana", player.DisplayName);
        Assert.Equal(Now, player.CreatedAt);
        Assert.NotEqual(Guid.Empty, player.Id);
    }

    [Fact]
    public void Two_players_may_share_a_display_name_and_get_different_ids() // BR-16
    {
        var first = Player.Create("ana@example.com", "hash", "Ana", Now);
        var second = Player.Create("ana.b@example.com", "hash", "Ana", Now);

        Assert.Equal(first.DisplayName, second.DisplayName);
        Assert.NotEqual(first.Id, second.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abcdefghijklmnopqrstuvwxy")]
    public void Creating_with_an_invalid_display_name_throws(string displayName)
    {
        Assert.Throws<ArgumentException>(() => Player.Create("ana@example.com", "hash", displayName, Now));
    }

    [Fact]
    public void Changing_to_a_preset_avatar_succeeds() // BR-17
    {
        var player = Player.Create("ana@example.com", "hash", "Ana", Now);

        Assert.True(player.TryChangeAvatar(4));
        Assert.Equal(4, player.AvatarId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void Changing_to_an_avatar_outside_the_set_is_refused_and_keeps_the_current_look(int avatarId) // BR-17
    {
        var player = Player.Create("ana@example.com", "hash", "Ana", Now);
        player.TryChangeAvatar(3);

        Assert.False(player.TryChangeAvatar(avatarId));
        Assert.Equal(3, player.AvatarId);
    }
}
