using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace LongPd.CleanArchitecture.Api.Extensions;

public static class OpenTelemetryExtensions
{
    public static IHostApplicationBuilder AddOpenTelemetryObservability(this IHostApplicationBuilder builder)
    {
        var serviceName = "LongPd.CleanArchitecture.Api";
        var serviceVersion = "1.0.0";

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
                .AddTelemetrySdk()
                .AddEnvironmentVariableDetector())
                .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(options =>
                {
                    // Ignore swagger/health endpoints from tracing
                    options.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health") && 
                                            !ctx.Request.Path.StartsWithSegments("/openapi") &&
                                            !ctx.Request.Path.StartsWithSegments("/scalar");
                })
                .AddHttpClientInstrumentation()
                .AddNpgsql() // Instrument Dapper and EF Core (via Npgsql)
                // Add Redis instrumentation when it's fully supported via StackExchange.Redis
                .AddOtlpExporter(opts =>
                {
                    opts.Endpoint = new Uri(builder.Configuration["Otlp:Endpoint"] ?? "http://localhost:4317");
                }))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation() // CPU, Memory
                // Export metrics to Prometheus for Grafana
                .AddPrometheusExporter());

        return builder;
    }
}
