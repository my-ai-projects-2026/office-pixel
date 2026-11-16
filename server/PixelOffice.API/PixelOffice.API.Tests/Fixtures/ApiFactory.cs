using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PixelOffice.API.Tests.Fixtures;

// In-process API host for tests that need no database.
// The API gets its connection string from the AppHost; tests run without the AppHost (plan.md),
// so this supplies a placeholder that is never opened and turns off the database health check.
// Persistence tests (T07 onward) use a PostgreSQL Testcontainers fixture instead.
public class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // UseSetting is applied before Program.cs reads configuration, unlike ConfigureAppConfiguration.
        builder.UseSetting("ConnectionStrings:pixeloffice", "Host=unused.invalid;Database=pixeloffice");
        builder.UseSetting("Aspire:Npgsql:EntityFrameworkCore:PostgreSQL:DisableHealthChecks", "true");
        builder.UseSetting(PixelOffice.API.Data.DatabaseStartup.ApplyOnStartupKey, "false");
    }
}
