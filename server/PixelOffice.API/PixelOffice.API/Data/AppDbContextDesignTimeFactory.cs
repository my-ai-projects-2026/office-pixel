using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PixelOffice.API.Data;

/// <summary>
/// Used only by <c>dotnet ef</c> (migrations add / script). Builds the context without starting the
/// web host, so creating a migration never needs a database or the AppHost. The connection string is a
/// placeholder; <c>dotnet ef database update</c> should be given a real one with <c>--connection</c>.
/// </summary>
internal sealed class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=pixeloffice")
            .Options);
}
