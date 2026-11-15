using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

// Microsoft's ServiceDefaults convention: living in this namespace lets every service call
// builder.AddServiceDefaults() without a using directive.
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Microsoft.Extensions.Hosting;
#pragma warning restore IDE0130

/// <summary>
/// Adds common Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
/// Reference this project from each service project in the solution.
/// See https://aka.ms/aspire/service-defaults.
/// </summary>
public static class ServiceDefaultsExtensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";

    /// <summary>
    /// Adds OpenTelemetry, the default health checks, service discovery, and resilient HTTP clients.
    /// </summary>
    /// <typeparam name="TBuilder">The type of the application builder.</typeparam>
    /// <param name="builder">The application builder.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default
            http.AddStandardResilienceHandler();

            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });

        // To restrict the schemes service discovery may resolve, configure ServiceDiscoveryOptions.AllowedSchemes.

        return builder;
    }

    /// <summary>
    /// Configures OpenTelemetry logging, metrics, and tracing, and the OTLP exporter when an endpoint is set.
    /// </summary>
    /// <typeparam name="TBuilder">The type of the application builder.</typeparam>
    /// <param name="builder">The application builder.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(builder.Environment.ApplicationName)
                    .AddAspNetCoreInstrumentation(options =>
                        // Exclude health check requests from tracing
                        options.Filter = context =>
                            !context.Request.Path.StartsWithSegments(HealthEndpointPath,
                                StringComparison.OrdinalIgnoreCase)
                            && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath,
                                StringComparison.OrdinalIgnoreCase)
                    )
                    .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    /// <summary>
    /// Adds a default liveness check, tagged <c>live</c>, so the app reports whether it is responsive.
    /// </summary>
    /// <typeparam name="TBuilder">The type of the application builder.</typeparam>
    /// <param name="builder">The application builder.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    /// <summary>
    /// Maps the <c>/health</c> (all checks) and <c>/alive</c> (checks tagged <c>live</c>) endpoints in Development.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The same application, for chaining.</returns>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Adding health checks endpoints to applications in non-development environments has security implications.
        // See https://aka.ms/aspire/healthchecks for details before enabling these endpoints in non-development environments.
        if (app.Environment.IsDevelopment())
        {
            // All health checks must pass for app to be considered ready to accept traffic after starting
            app.MapHealthChecks(HealthEndpointPath);

            // Only health checks tagged with the "live" tag must pass for app to be considered alive
            app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
        }

        return app;
    }

    /// <summary>
    /// Adds the OTLP exporter when <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> is set. Add other exporters here, e.g. Azure Monitor
    /// with the Azure.Monitor.OpenTelemetry.AspNetCore package.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    private static void AddOpenTelemetryExporters(this IHostApplicationBuilder builder)
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }
    }
}
