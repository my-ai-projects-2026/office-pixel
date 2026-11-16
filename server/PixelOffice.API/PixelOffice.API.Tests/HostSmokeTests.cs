using System.Net;
using PixelOffice.API.Tests.Fixtures;

namespace PixelOffice.API.Tests;

// T01/T38: proves the API host boots in-process and exposes the ServiceDefaults health endpoints.
// WebApplicationFactory runs the host in the Development environment, where MapDefaultEndpoints maps them.
// The database health check is covered by running the AppHost (T02), not here.
public sealed class HostSmokeTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Theory]
    [InlineData("/health")]
    [InlineData("/alive")]
    public async Task Host_boots_and_reports_healthy(string path)
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
