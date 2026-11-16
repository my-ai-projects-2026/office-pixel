using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PixelOffice.API.Data;

namespace PixelOffice.API.Tests.Fixtures;

/// <summary>
/// In-process API host connected to a real PostgreSQL database, with Development startup behaviour
/// (migrate and seed) switched on and the database health check left in place.
/// Pass a <see cref="TimeProvider"/> (usually a FakeTimeProvider) to control the API's clock.
/// </summary>
public sealed class DatabaseApiFactory : WebApplicationFactory<Program>
{
    private readonly string connectionString;
    private readonly TimeProvider? timeProvider;

    public DatabaseApiFactory(string connectionString, TimeProvider? timeProvider = null)
    {
        this.connectionString = connectionString;
        this.timeProvider = timeProvider;
        ApiFactory.UseHttps(ClientOptions);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:pixeloffice", connectionString);
        builder.UseSetting(DatabaseStartup.ApplyOnStartupKey, "true");
        builder.UseSetting("Jwt:SigningKey", ApiFactory.TestSigningKey);

        if (timeProvider is not null)
        {
            builder.ConfigureTestServices(services => services.Replace(ServiceDescriptor.Singleton(timeProvider)));
        }
    }
}
