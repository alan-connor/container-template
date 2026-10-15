using Container.Template.Api.Extensions;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;

Serilog.Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.
    builder.Services.AddSeriLogLogging(builder.Environment);

    if (builder.Configuration.GetValue<bool>("Swagger:Enable"))
    {
        builder.Services.AddSwaggerGen();
    }

    // "live" = the process is up and serving requests; "ready" = it can take traffic.
    // Add dependency checks (database, downstream APIs) with the "ready" tag.
    builder.Services.AddHealthChecks()
        .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live", "ready"]);

    builder.Services.AddControllers()
        .AddJsonOptions(o =>
        {
            o.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            o.JsonSerializerOptions.WriteIndented = true;
        });

    var app = builder.Build();

    if (builder.Configuration.GetValue<bool>("Swagger:Enable"))
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.MapHealthChecks("/healthz", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
    app.MapHealthChecks("/healthz/live", new HealthCheckOptions { Predicate = registration => registration.Tags.Contains("live") }).AllowAnonymous();
    app.MapHealthChecks("/healthz/ready", new HealthCheckOptions { Predicate = registration => registration.Tags.Contains("ready") }).AllowAnonymous();

    app.UseAuthorization();
    app.MapControllers();

    await app.RunAsync();

    return 0;
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

// Exposed so integration tests can host the app with WebApplicationFactory<Program>.
public partial class Program;
