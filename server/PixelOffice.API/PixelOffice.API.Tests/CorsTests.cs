using Microsoft.AspNetCore.Hosting;
using PixelOffice.API.Tests.Fixtures;

namespace PixelOffice.API.Tests;

// T39: the API accepts browser calls only from the origin it is configured with (the AppHost passes the client's address).
public sealed class CorsTests(CorsTests.ClientOriginFactory factory) : IClassFixture<CorsTests.ClientOriginFactory>
{
    private const string ClientOrigin = "http://localhost:3000";

    public sealed class ClientOriginFactory : ApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseSetting("Cors:AllowedOrigins:0", ClientOrigin);
        }
    }

    [Fact]
    public async Task Preflight_from_the_configured_client_origin_is_allowed()
    {
        using var response = await SendPreflightAsync(ClientOrigin);

        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values));
        Assert.Equal(ClientOrigin, Assert.Single(values));
        Assert.Contains("true", response.Headers.GetValues("Access-Control-Allow-Credentials"));
    }

    [Fact]
    public async Task Preflight_from_any_other_origin_is_not_allowed()
    {
        using var response = await SendPreflightAsync("http://evil.example");

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    private async Task<HttpResponseMessage> SendPreflightAsync(string origin)
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/health");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");
        return await client.SendAsync(request);
    }
}
