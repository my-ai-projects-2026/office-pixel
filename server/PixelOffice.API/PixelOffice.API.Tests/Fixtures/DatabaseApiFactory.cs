using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using PixelOffice.API.Data;

namespace PixelOffice.API.Tests.Fixtures;

/// <summary>
/// In-process API host connected to a real PostgreSQL database, with Development startup behaviour
/// (migrate and seed) switched on and the database health check left in place.
/// </summary>
public sealed class DatabaseApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:pixeloffice", connectionString);
        builder.UseSetting(DatabaseStartup.ApplyOnStartupKey, "true");
    }
}
