using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using PixelOffice.API.Data;
using PixelOffice.API.Features.Auth;
using PixelOffice.API.Features.Players;
using PixelOffice.API.Tests.Fixtures;

namespace PixelOffice.API.Tests.Auth;

// T08: registration and sign-in against a real PostgreSQL database (US-01, US-09; BR-19–BR-23).
// Each test gets its own database through its own API host.
[Collection(PostgresCollection.Name)]
public sealed class AuthTests(PostgresFixture postgres)
{
    private const string Password = "correct-horse-1";

    private readonly FakeTimeProvider clock = new(new DateTimeOffset(2026, 11, 16, 9, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task AC_25_Registration_creates_account_signs_in_and_uses_first_avatar()
    {
        await using var factory = NewApi();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("ana@example.com", Password, "  Ana  "));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        Assert.Equal("Ana", body.Player.DisplayName);
        Assert.Equal(AvatarCatalog.DefaultAvatarId, body.Player.AvatarId);
        Assert.Equal(clock.GetUtcNow() + TimeSpan.FromHours(12), body.ExpiresAt);

        // Signed in: the token names this player and the API accepts it.
        var validation = await ValidateAsync(factory, body.AccessToken);
        Assert.True(validation.IsValid);
        Assert.Equal(body.Player.Id.ToString(), validation.Claims["sub"]);

        var stored = await SingleStoredPlayerAsync(factory);
        Assert.Equal(body.Player.Id, stored.Id);
        Assert.Equal("ana@example.com", stored.Email);
        Assert.NotEqual(Password, stored.PasswordHash);
    }

    [Fact]
    public async Task AC_26_Duplicate_email_ignoring_case_and_spaces_is_refused()
    {
        await using var factory = NewApi();
        using var client = factory.CreateClient();
        (await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("ana@example.com", Password, "Ana"))).Dispose();

        using var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(" Ana@Example.com ", Password, "Other Ana"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("Email already in use", (await response.Content.ReadFromJsonAsync<ProblemDetails>())!.Title);
        await SingleStoredPlayerAsync(factory);
    }

    [Fact]
    public async Task AC_26_Two_registrations_with_the_same_email_at_the_same_moment_create_one_account()
    {
        await using var factory = NewApi();
        using var client = factory.CreateClient();

        var responses = await Task.WhenAll(
            client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("ana@example.com", Password, "Ana")),
            client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("ANA@example.com", Password, "Ana")));

        Assert.Equal(
            [HttpStatusCode.Created, HttpStatusCode.Conflict],
            responses.Select(r => r.StatusCode).OrderBy(s => (int)s));
        await SingleStoredPlayerAsync(factory);
        foreach (var r in responses)
        {
            r.Dispose();
        }
    }

    public static TheoryData<string?, string?, string?, string> InvalidRegistrations => new()
    {
        { "not-an-email", Password, "Ana", "email" },
        { "ana@localhost", Password, "Ana", "email" },
        { "", Password, "Ana", "email" },
        { null, Password, "Ana", "email" },
        { new string('a', 245) + "@example.com", Password, "Ana", "email" }, // 257 characters
        { "ana@example.com", "short7!", "Ana", "password" },
        { "ana@example.com", new string('p', 129), "Ana", "password" },
        { "ana@example.com", null, "Ana", "password" },
        { "ana@example.com", Password, "", "displayName" },
        { "ana@example.com", Password, "    ", "displayName" },
        { "ana@example.com", Password, new string('n', 25), "displayName" },
        { "ana@example.com", Password, null, "displayName" },
    };

    [Theory]
    [MemberData(nameof(InvalidRegistrations))]
    public async Task AC_27_Invalid_registration_names_the_field_and_creates_nothing(
        string? email, string? password, string? displayName, string field)
    {
        await using var factory = NewApi();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password, displayName));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>())!;
        Assert.Equal([field], problem.Errors.Keys);
        Assert.NotEmpty(problem.Errors[field].Single());
        await AssertNoPlayersAsync(factory);
    }

    [Fact]
    public async Task AC_27_Every_wrong_field_is_reported_at_once()
    {
        await using var factory = NewApi();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest("x", "short", " "));

        var problem = (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>())!;
        Assert.Equal(["displayName", "email", "password"], problem.Errors.Keys.Order());
        await AssertNoPlayersAsync(factory);
    }

    [Fact]
    public async Task AC_28_Correct_credentials_sign_in()
    {
        await using var factory = NewApi();
        using var client = factory.CreateClient();
        var registered = await RegisterAsync(client, "ana@example.com");

        using var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(" ANA@example.com", Password));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        Assert.Equal(registered.Player, body.Player);
        var validation = await ValidateAsync(factory, body.AccessToken);
        Assert.True(validation.IsValid);
        Assert.Equal(registered.Player.Id.ToString(), validation.Claims["sub"]);
    }

    [Fact]
    public async Task AC_29_Wrong_password_and_unknown_email_get_the_same_refusal()
    {
        await using var factory = NewApi();
        using var client = factory.CreateClient();
        await RegisterAsync(client, "ana@example.com");

        using var wrongPassword = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("ana@example.com", "wrong-password"));
        using var unknownEmail = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("nobody@example.com", Password));

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);
        var first = await ProblemWithoutTraceIdAsync(wrongPassword);
        var second = await ProblemWithoutTraceIdAsync(unknownEmail);
        Assert.Equal(first, second);
        Assert.Contains("Email or password is incorrect", first, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AC_30_Token_older_than_12_hours_is_refused_by_the_api()
    {
        // The hub refusing the same token is covered when the hub exists (T15).
        await using var factory = NewApi();
        using var client = factory.CreateClient();
        var token = (await RegisterAsync(client, "ana@example.com")).AccessToken;

        clock.Advance(TimeSpan.FromHours(12) - TimeSpan.FromMinutes(1));
        Assert.True((await ValidateAsync(factory, token)).IsValid);

        clock.Advance(TimeSpan.FromMinutes(2));
        var expired = await ValidateAsync(factory, token);
        Assert.False(expired.IsValid);
    }

    private DatabaseApiFactory NewApi() => new(postgres.NewDatabaseConnectionString(), clock);

    private static async Task<AuthResponse> RegisterAsync(HttpClient client, string email)
    {
        using var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, Password, "Ana"));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    /// <summary>Validates a token exactly as the API's JWT bearer handler is configured to.</summary>
    private static async Task<Microsoft.IdentityModel.Tokens.TokenValidationResult> ValidateAsync(DatabaseApiFactory factory, string token)
    {
        var bearer = factory.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);
        return await new JsonWebTokenHandler().ValidateTokenAsync(token, bearer.TokenValidationParameters);
    }

    private static async Task<Player> SingleStoredPlayerAsync(DatabaseApiFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().Players.SingleAsync();
    }

    private static async Task AssertNoPlayersAsync(DatabaseApiFactory factory)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        Assert.False(await scope.ServiceProvider.GetRequiredService<AppDbContext>().Players.AnyAsync());
    }

    private static async Task<string> ProblemWithoutTraceIdAsync(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return string.Join(
            "|",
            json.RootElement.EnumerateObject()
                .Where(p => p.Name != "traceId")
                .OrderBy(p => p.Name, StringComparer.Ordinal)
                .Select(p => $"{p.Name}={p.Value}"));
    }
}
