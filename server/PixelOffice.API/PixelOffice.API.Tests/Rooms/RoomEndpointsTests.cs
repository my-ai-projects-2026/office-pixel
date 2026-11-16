using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using PixelOffice.API.Features.Auth;
using PixelOffice.API.Tests.Fixtures;

namespace PixelOffice.API.Tests.Rooms;

// T10: the default room's definition and office map for the client (US-04), against a real PostgreSQL database.
[Collection(PostgresCollection.Name)]
public sealed class RoomEndpointsTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Signed_in_player_gets_the_seeded_default_room_and_office_map()
    {
        await using var factory = new DatabaseApiFactory(postgres.NewDatabaseConnectionString());
        using var client = factory.CreateClient();
        var token = await RegisterAsync(client);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/rooms/default");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var room = json.RootElement;
        Assert.Equal("default", room.GetProperty("slug").GetString());
        Assert.Equal("Office", room.GetProperty("name").GetString());
        Assert.Equal(10, room.GetProperty("maxPlayers").GetInt32()); // BR-02
        Assert.Equal(1, room.GetProperty("mapVersion").GetInt32());

        // The map is the seeded Tiled file, sent as a JSON object the client can use directly.
        var map = room.GetProperty("map");
        Assert.Equal(JsonValueKind.Object, map.ValueKind);
        Assert.Equal(30, map.GetProperty("width").GetInt32());
        Assert.Equal(20, map.GetProperty("height").GetInt32());
        Assert.Equal(
            ["floor", "walls", "furniture", "spawn", "zones"],
            map.GetProperty("layers").EnumerateArray().Select(l => l.GetProperty("name").GetString()));
    }

    [Fact]
    public async Task Anonymous_call_is_refused()
    {
        await using var factory = new DatabaseApiFactory(postgres.NewDatabaseConnectionString());
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/rooms/default");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task<string> RegisterAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/auth/register", new RegisterRequest("ana@example.com", "correct-horse-1", "Ana"));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!.AccessToken;
    }
}
