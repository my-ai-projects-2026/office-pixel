using PixelOffice.API.Data;
using PixelOffice.API.Features.Auth;
using PixelOffice.API.Features.Players;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Connection string "pixeloffice" is injected by the AppHost; it is never kept in source.
builder.AddNpgsqlDbContext<AppDbContext>("pixeloffice");
builder.Services.AddScoped<DbSeeder>();

// All time goes through TimeProvider so tests can control the clock (CLAUDE.md).
builder.Services.AddSingleton(TimeProvider.System);

// Browser origins allowed to call the API and (later) the SignalR hub. The AppHost supplies the
// client's address as Cors:AllowedOrigins:0; with nothing configured, no cross-origin call is allowed.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

builder.AddPixelOfficeAuth();

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Before HTTPS redirection so preflight requests are answered, not redirected.
app.UseCors();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();
app.MapAuthEndpoints();
app.MapPlayerEndpoints();

await app.MigrateAndSeedAsync();

await app.RunAsync();
