using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace Container.Template.Api.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Logs to the console only -- in a container, stdout is collected by the runtime
        /// (kubectl logs, Docker, a log shipper). Development gets a readable text format;
        /// every other environment gets one JSON object per line.
        /// </summary>
        public static IServiceCollection AddSeriLogLogging(this IServiceCollection services, IHostEnvironment environment)
        {
            services.AddSerilog(c =>
            {
                c.MinimumLevel.Information()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                    .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
                    .Filter.ByExcluding(e => e.Properties.TryGetValue("RequestPath", out var path) && path.ToString().Contains("healthz"))
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("Application", "container-template");

                if (environment.IsDevelopment())
                {
                    c.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:lj}{NewLine}{Exception}");
                }
                else
                {
                    c.WriteTo.Console(new JsonFormatter(renderMessage: true));
                }
            });

            return services;
        }
    }
}
