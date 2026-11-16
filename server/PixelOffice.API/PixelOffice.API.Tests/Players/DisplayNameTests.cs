using PixelOffice.API.Features.Players;

namespace PixelOffice.API.Tests.Players;

// BR-20: display name is 1–24 characters after removing surrounding spaces; only-spaces counts as empty.
public sealed class DisplayNameTests
{
    [Theory]
    [InlineData("Ana", "Ana")]
    [InlineData("  Ana  ", "Ana")]
    [InlineData("A", "A")]
    [InlineData("Ana Maria", "Ana Maria")]
    [InlineData("abcdefghijklmnopqrstuvwx", "abcdefghijklmnopqrstuvwx")] // 24
    [InlineData("  abcdefghijklmnopqrstuvwx  ", "abcdefghijklmnopqrstuvwx")] // 24 after trim
    public void Valid_names_are_accepted_and_trimmed(string input, string expected)
    {
        Assert.True(DisplayName.TryNormalize(input, out var normalized));
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    [InlineData("abcdefghijklmnopqrstuvwxy")] // 25
    public void Empty_blank_or_too_long_names_are_refused(string? input)
    {
        Assert.False(DisplayName.TryNormalize(input, out var normalized));
        Assert.Null(normalized);
    }
}
