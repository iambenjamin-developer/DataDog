using Npgsql;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Products.Api.Telemetry;

public static class OpenTelemetryExtensions
{
    /// <summary>
    /// Configura trazas, métricas y logs con OpenTelemetry y los exporta por OTLP al Datadog Agent.
    /// El endpoint se toma de OTEL_EXPORTER_OTLP_ENDPOINT (por defecto http://localhost:4317, gRPC).
    /// </summary>
    public static WebApplicationBuilder AddObservability(this WebApplicationBuilder builder)
    {
        var serviceName = builder.Configuration["OpenTelemetry:ServiceName"] ?? "products-api";
        var serviceVersion = builder.Configuration["OpenTelemetry:ServiceVersion"] ?? "1.0.0";
        var environment = builder.Environment.EnvironmentName.ToLowerInvariant();

        builder.Services.AddOpenTelemetry()
            // Datadog mapea estos atributos a sus tags de Unified Service Tagging: service, version y env.
            .ConfigureResource(resource => resource
                .AddService(serviceName, serviceVersion: serviceVersion)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment.name"] = environment,
                    ["deployment.environment"] = environment // convención anterior, por compatibilidad
                }))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.Filter = ctx =>
                        !ctx.Request.Path.StartsWithSegments("/health") &&
                        !ctx.Request.Path.StartsWithSegments("/swagger");
                })
                .AddHttpClientInstrumentation()
                .AddNpgsql()
                .AddSource(ProductsTelemetry.SourceName)
                .AddOtlpExporter())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter("Npgsql")
                .AddMeter(ProductsTelemetry.SourceName)
                // Datadog recomienda temporalidad Delta para métricas OTLP.
                .AddOtlpExporter((_, reader) =>
                    reader.TemporalityPreference = MetricReaderTemporalityPreference.Delta))
            .WithLogging(
                logging => logging.AddOtlpExporter(),
                options =>
                {
                    options.IncludeFormattedMessage = true;
                    options.IncludeScopes = true;
                });

        return builder;
    }
}
