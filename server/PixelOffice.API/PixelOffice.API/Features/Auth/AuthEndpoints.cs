using Microsoft.EntityFrameworkCore;
using Npgsql;
using PixelOffice.API.Data;
using PixelOffice.API.Features.Players;

namespace PixelOffice.API.Features.Auth;

/// <summary>Registration and sign-in (US-01, US-09; BR-01, BR-19–BR-23).</summary>
public static class AuthEndpoints
{
    // Messages shown to the player (AC-26, AC-27, AC-29).
    internal const string EmailMessage = "Enter a valid email address (at most 254 characters).";
    internal const string PasswordMessage = "Password must be 8 to 128 characters.";
    internal const string DisplayNameMessage = "Display name must be 1 to 24 characters.";
    internal const string EmailInUseTitle = "Email already in use";
    internal const string SignInFailedTitle = "Email or password is incorrect";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").AllowAnonymous();
        group.MapPost("/register", RegisterAsync);
        group.MapPost("/login", LoginAsync);
        return app;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        AppDbContext db,
        PasswordHashing passwords,
        JwtTokenService tokens,
        TimeProvider timeProvider,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(typeof(AuthEndpoints));

        // BR-20: check every field and report each one that is wrong; nothing is created on failure.
        var errors = new Dictionary<string, string[]>();
        var email = EmailAddress.Normalize(request.Email ?? string.Empty);
        if (!EmailAddress.IsValid(email))
        {
            errors["email"] = [EmailMessage];
        }

        if (!PasswordRules.IsValid(request.Password))
        {
            errors["password"] = [PasswordMessage];
        }

        if (!DisplayName.TryNormalize(request.DisplayName, out var displayName))
        {
            errors["displayName"] = [DisplayNameMessage];
        }

        if (errors.Count > 0 || displayName is null || request.Password is null)
        {
            logger.LogInformation("Registration refused: invalid {Fields}", string.Join(",", errors.Keys));
            return TypedResults.ValidationProblem(errors);
        }

        // BR-19: one account per email. The unique index settles two registrations racing each other.
        if (await db.Players.AnyAsync(p => p.Email == email, cancellationToken))
        {
            logger.LogInformation("Registration refused: email already in use");
            return EmailInUse();
        }

        var player = Player.Create(email, passwords.Hash(request.Password), displayName, timeProvider.GetUtcNow());
        db.Players.Add(player);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            logger.LogInformation("Registration refused: email already in use (concurrent registration)");
            return EmailInUse();
        }

        logger.LogInformation("Player {PlayerId} registered", player.Id);

        // BR-22: creating an account signs the player in.
        return TypedResults.Created("/api/me", ToResponse(player, tokens.Issue(player)));
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        AppDbContext db,
        PasswordHashing passwords,
        JwtTokenService tokens,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var logger = loggerFactory.CreateLogger(typeof(AuthEndpoints));
        var email = EmailAddress.Normalize(request.Email ?? string.Empty);
        var password = request.Password ?? string.Empty;

        var player = await db.Players.SingleOrDefaultAsync(p => p.Email == email, cancellationToken);
        if (player is null)
        {
            // BR-21: same work and same answer as a wrong password, so the response does not reveal
            // whether the email has an account.
            passwords.VerifyAgainstDummy(password);
            logger.LogInformation("Sign-in refused");
            return SignInFailed();
        }

        var check = passwords.Verify(player.PasswordHash, password);
        if (check == PasswordCheck.Invalid)
        {
            logger.LogInformation("Sign-in refused");
            return SignInFailed();
        }

        if (check == PasswordCheck.ValidButRehash)
        {
            player.ChangePasswordHash(passwords.Hash(password));
            await db.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation("Player {PlayerId} signed in", player.Id);
        return TypedResults.Ok(ToResponse(player, tokens.Issue(player)));
    }

    private static AuthResponse ToResponse(Player player, IssuedToken token) =>
        new(token.AccessToken, token.ExpiresAt, new PlayerProfile(player.Id, player.DisplayName, player.AvatarId));

    private static IResult EmailInUse() =>
        TypedResults.Problem(
            title: EmailInUseTitle,
            detail: "An account with this email already exists. Sign in instead.",
            statusCode: StatusCodes.Status409Conflict);

    private static IResult SignInFailed() =>
        TypedResults.Problem(title: SignInFailedTitle, statusCode: StatusCodes.Status401Unauthorized);
}
