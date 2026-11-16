using PixelOffice.API.Features.Players;

namespace PixelOffice.API.Tests.Players;

// BR-19: case and surrounding spaces are ignored when comparing emails.
public sealed class EmailAddressTests
{
    [Theory]
    [InlineData("ana@example.com", "ana@example.com")]
    [InlineData(" Ana@Example.com ", "ana@example.com")]
    [InlineData("\tANA@EXAMPLE.COM\n", "ana@example.com")]
    public void Normalize_trims_and_lower_cases(string input, string expected)
    {
        Assert.Equal(expected, EmailAddress.Normalize(input));
    }

    [Fact]
    public void Normalize_keeps_inner_characters()
    {
        Assert.Equal("ana.maria+office@example.com", EmailAddress.Normalize(" Ana.Maria+Office@Example.com"));
    }
}
