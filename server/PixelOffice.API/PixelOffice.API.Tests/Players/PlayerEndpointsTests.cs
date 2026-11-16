using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using PixelOffice.API.Features.Auth;
using PixelOffice.API.Features.Players;
using PixelOffice.API.Tests.Fixtures;

namespace PixelOffice.API.Tests.Players;

// T09: the signed-in player's profile and avatar choice, against a real PostgreSQL database (BR-12, BR-17).
// Telling other players in the room about a change (AC-22) comes with the room in T18.
[Collection(PostgresCollection.Name)]
public sealed class PlayerEndpointsTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Me_returns_the_signed_in_players_profile() // BR-12
    {
        await using var factory = NewApi();
        using var client = factory.CreateClient();
        var ana = await RegisterAsync(client, "ana@example.com", "Ana");

        var me = await GetMeAsync(client, ana.AccessToken);

        Assert.Equal(ana.Player, me);
    }

    [Fact]
    public async Task Each_player_sees_only_their_own_profile() // BR-01
    {
        await using var factory = NewApi();
        using var client = factory.CreateClient();
        var ana = await RegisterAsync(client, "ana@example.com", "Ana");
        var ben = await RegisterAsync(client, "ben@example.com", "Ben");

        Assert.Equal("Ana", (await GetMeAsync(client, ana.AccessToken)).DisplayName);
        Assert.Equal("Ben", (await GetMeAsync(client, ben.AccessToken)).DisplayName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-token")]
    public async Task Me_without_a_valid_token_is_refused(string? token)
    {
        await using var factory = NewApi();
        using var client = factory.CreateClient();

        using var response = await SendAsync(client, HttpMethod.Get, "/api/me", token);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Avatars_lists_the_six_presets_without_signing_in() // BR-17
    {
        await using var factory = NewApi();
        using var client = factory.CreateClient();

        var avatars = await client.GetFromJsonAsync<AvatarOption[]>("/api/avatars");

        Assert.Equal([1, 2, 3, 4, 5, 6], avatars!.Select(a => a.Id));
        Assert.All(avatars!, a => Assert.False(string.IsNullOrEmpty(a.Key)));
    }

    [Fact]
    public async Task Choosing_a_preset_avatar_is_saved_and_returned_by_me() // BR-17
    {
        await using var factory = NewApi();
        using var client = factory.CreateClient();
        var ana = await RegisterAsync(client, "ana@example.com", "Ana");

        using var response = await SendAsync(client, HttpMethod.Put, "/api/me/avatar", ana.AccessToken, new SetAvatarRequest(4));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(4, (await GetMeAsync(client, ana.AccessToken)).AvatarId);

        // Saved with the account: still there after signing in again.
        using var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("ana@example.com", Password));
        Assert.Equal(4, (await login.Content.ReadFromJsonAsync<AuthResponse>())!.Player.AvatarId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(-1)]
    public async Task Choosing_an_avatar_outside_the_set_is_refused_and_keeps_the_current_look(int avatarId) // BR-17
    {
        await using var factory = NewApi();
        using var client = factory.CreateClient();
        var ana = await RegisterAsync(client, "ana@example.com", "Ana");
        (await SendAsync(client, HttpMethod.Put, "/api/me/avatar", ana.AccessToken, new SetAvatarRequest(3))).Dispose();

        using var response = await SendAsync(client, HttpMethod.Put, "/api/me/avatar", ana.AccessToken, new SetAvatarRequest(avatarId));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>())!;
        Assert.Equal(["avatarId"], problem.Errors.Keys);
        Assert.Equal(3, (await GetMeAsync(client, ana.AccessToken)).AvatarId);
    }

    [Fact]
    public async Task Changing_an_avatar_changes_only_the_callers_look() // BR-01
    {
        await using var factory = NewApi();
        using var client = factory.CreateClient();
        var ana = await RegisterAsync(client, "ana@example.com", "Ana");
        var ben = await RegisterAsync(client, "ben@example.com", "Ben");

        (await SendAsync(client, HttpMethod.Put, "/api/me/avatar", ana.AccessToken, new SetAvatarRequest(5))).Dispose();

        Assert.Equal(5, (await GetMeAsync(client, ana.AccessToken)).AvatarId);
        Assert.Equal(AvatarCatalog.DefaultAvatarId, (await GetMeAsync(client, ben.AccessToken)).AvatarId);
    }

    [Fact]
    public async Task Changing_an_avatar_without_a_token_is_refused()
    {
        await using var factory = NewApi();
        using var client = factory.CreateClient();

        using var response = await SendAsync(client, HttpMethod.Put, "/api/me/avatar", token: null, new SetAvatarRequest(2));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private const string Password = "correct-horse-1";

    private DatabaseApiFactory NewApi() => new(postgres.NewDatabaseConnectionString());

    private static async Task<AuthResponse> RegisterAsync(HttpClient client, string email, string displayName)
    {
        using var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, Password, displayName));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    private static async Task<PlayerProfile> GetMeAsync(HttpClient client, string token)
    {
        using var response = await SendAsync(client, HttpMethod.Get, "/api/me", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PlayerProfile>())!;
    }

    private static Task<HttpResponseMessage> SendAsync(
        HttpClient client, HttpMethod method, string path, string? token, object? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return client.SendAsync(request);
    }
}
