using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace PixelOffice.API.Features.Auth;

public static class AuthSetup
{
    /// <summary>Allowance for small clock differences when checking a token's lifetime.</summary>
    private static readonly TimeSpan ClockSkew = TimeSpan.FromSeconds(30);

    /// <summary>JWT bearer authentication for the API (and, from T15, the hub) plus the services sign-in needs.</summary>
    public static IHostApplicationBuilder AddPixelOfficeAuth(this IHostApplicationBuilder builder)
    {
        builder.Services.AddOptions<JwtOptions>()
            .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddSingleton<JwtTokenService>();
        builder.Services.AddSingleton<PasswordHashing>();

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>, TimeProvider>((bearer, jwt, timeProvider) =>
            {
                var settings = jwt.Value;
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = settings.Issuer,
                    ValidAudience = settings.Audience,
                    IssuerSigningKey = JwtTokenService.SigningKey(settings),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    NameClaimType = "sub",
                    RequireExpirationTime = true,
                    ValidateLifetime = true,
                    ClockSkew = ClockSkew,
                    // Lifetime is checked against TimeProvider so tests can move the clock (BR-23, AC-30).
                    LifetimeValidator = (notBefore, expires, _, _) =>
                    {
                        var now = timeProvider.GetUtcNow().UtcDateTime;
                        return expires is { } exp && now < exp + ClockSkew
                            && (notBefore is not { } nbf || now >= nbf - ClockSkew);
                    },
                };
            });

        builder.Services.AddAuthorization();
        return builder;
    }
}
