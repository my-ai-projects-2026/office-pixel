var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL runs in a Docker container. The data volume keeps accounts, avatar choices,
// maps and diagnostic records across AppHost restarts; live positions are never stored (BR-13).
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

// The API reads this as ConnectionStrings:pixeloffice (AddNpgsqlDbContext("pixeloffice")).
var database = postgres.AddDatabase("pixeloffice");

var api = builder.AddProject<Projects.PixelOffice_API>("api")
    .WithReference(database)
    .WaitFor(database)
    .WithHttpHealthCheck("/health");

// The Next.js client (its own git repository at /app) runs with `npm run dev`.
// Only NEXT_PUBLIC_* variables reach browser code, so the API address is passed under that name.
#pragma warning disable ASPIREJAVASCRIPT001 // AddNextJsApp is experimental in Aspire 13.6.
var web = builder.AddNextJsApp("web", "../../../app")
    .WithEnvironment("NEXT_PUBLIC_API_URL", api.GetEndpoint("https"))
    .WaitFor(api);
#pragma warning restore ASPIREJAVASCRIPT001

// The API allows browser calls only from the client's own address (see Program.cs CORS policy).
api.WithEnvironment("Cors__AllowedOrigins__0", web.GetEndpoint("http"));

builder.Build().Run();
