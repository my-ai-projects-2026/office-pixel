using PixelOffice.API.Features.Auth;
using PixelOffice.API.Features.Players;

namespace PixelOffice.API.Tests.Auth;

// BR-20 rule details, without a database.
public sealed class RegistrationRulesTests
{
    [Theory]
    [InlineData("ana@example.com", true)]
    [InlineData("ana.maria+office@mail.example.com", true)]
    [InlineData("ana@example", false)]
    [InlineData("ana@.example.com", false)]
    [InlineData("ana@example.com.", false)]
    [InlineData("ana example@example.com", false)]
    [InlineData("Ana <ana@example.com>", false)]
    [InlineData("ana@@example.com", false)]
    [InlineData("@example.com", false)]
    [InlineData("", false)]
    public void Email_must_look_like_an_address(string normalizedEmail, bool expected)
    {
        Assert.Equal(expected, EmailAddress.IsValid(normalizedEmail));
    }

    [Fact]
    public void Email_of_254_characters_is_accepted_and_255_refused()
    {
        var at254 = new string('a', 242) + "@example.com";
        var at255 = new string('a', 243) + "@example.com";

        Assert.Equal(254, at254.Length);
        Assert.True(EmailAddress.IsValid(at254));
        Assert.False(EmailAddress.IsValid(at255));
    }

    [Theory]
    [InlineData(7, false)]
    [InlineData(8, true)]
    [InlineData(128, true)]
    [InlineData(129, false)]
    public void Password_must_be_8_to_128_characters(int length, bool expected)
    {
        Assert.Equal(expected, PasswordRules.IsValid(new string('p', length)));
    }

    [Fact]
    public void Missing_password_is_refused()
    {
        Assert.False(PasswordRules.IsValid(null));
    }

    [Fact]
    public void Password_hash_is_not_the_password_and_verifies()
    {
        var hashing = new PasswordHashing();

        var hash = hashing.Hash("correct-horse-1");

        Assert.NotEqual("correct-horse-1", hash);
        Assert.Equal(PasswordCheck.Valid, hashing.Verify(hash, "correct-horse-1"));
        Assert.Equal(PasswordCheck.Invalid, hashing.Verify(hash, "wrong"));
    }
}
