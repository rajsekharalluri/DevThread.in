using System.Text.Json.Serialization;
using DeveloperEngineeringAcademy.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Content root resolution:
// - In production (a published deployment), the app runs from a flat publish folder, so there is no
//   reliable relative path back to the repository's /content directory. Set "Content:RootPath"
//   (or the Content__RootPath environment variable) to an absolute path on the server instead.
// - In development, dotnet run uses the project directory as the content root, so the repo's
//   /content folder can still be found two levels up as a convenient default.
var configuredContentRoot = builder.Configuration["Content:RootPath"];
var contentRoot = !string.IsNullOrWhiteSpace(configuredContentRoot)
    ? Path.GetFullPath(configuredContentRoot)
    : Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "..", "content"));

if (!Directory.Exists(contentRoot))
{
    throw new DirectoryNotFoundException(
        $"Content directory not found at '{contentRoot}'. Set the Content:RootPath configuration value " +
        "(or the Content__RootPath environment variable) to the absolute path of the content directory.");
}

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:4200"];

builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddContentInfrastructure(contentRoot);
builder.Services.AddCors(options => options.AddPolicy("frontend", policy => policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

// Behind a reverse proxy (Nginx) on the same host, trust its X-Forwarded-* headers so the app sees the
// real client IP/scheme instead of "localhost/http" for every request. Restricting to loopback keeps
// this safe: only a proxy running on the same machine can supply these headers.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownProxies.Add(System.Net.IPAddress.Loopback);
    options.KnownProxies.Add(System.Net.IPAddress.IPv6Loopback);
});

var app = builder.Build();

if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.UseForwardedHeaders();
// HTTPS redirection is intentionally not enabled yet: with only an IP address (no domain/certificate),
// there is no HTTPS to redirect to. Once a domain and a TLS certificate are added (see DEPLOYMENT.md),
// terminate TLS at Nginx and it will handle the http -> https redirect there.
app.UseCors("frontend");
app.UseExceptionHandler(exceptionApp => exceptionApp.Run(async context =>
{
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    context.Response.ContentType = "application/problem+json";
    await context.Response.WriteAsJsonAsync(new { title = "Unexpected error", status = 500, traceId = context.TraceIdentifier });
}));
app.MapControllers();

app.Run();

public partial class Program { }
