using Microsoft.EntityFrameworkCore;

namespace PixelOffice.API.Data;

/// <summary>
/// Applies pending migrations and seeds the default room and map before the API starts serving.
/// Only runs when <see cref="ApplyOnStartupKey"/> is true, which appsettings.Development.json sets; other
/// environments apply migrations with <c>dotnet ef database update</c> (plan.md, Persistence).
/// </summary>
public static class DatabaseStartup
{
    public const string ApplyOnStartupKey = "Database:ApplyMigrationsOnStartup";

    public static async Task MigrateAndSeedAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        if (!app.Configuration.GetValue<bool>(ApplyOnStartupKey))
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
        await scope.ServiceProvider.GetRequiredService<DbSeeder>().SeedAsync(cancellationToken);
    }
}
