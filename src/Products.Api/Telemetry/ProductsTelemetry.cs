using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Products.Api.Telemetry;

/// <summary>
/// Instrumentación propia de la API: spans de negocio y métricas custom que se envían a Datadog vía OTLP.
/// </summary>
public static class ProductsTelemetry
{
    public const string SourceName = "Products.Api";

    public static readonly ActivitySource ActivitySource = new(SourceName);

    static readonly Meter Meter = new(SourceName);

    public static readonly Counter<long> ProductsCreated =
        Meter.CreateCounter<long>("products.created", unit: "{product}", description: "Productos creados");

    public static readonly Counter<long> ProductsUpdated =
        Meter.CreateCounter<long>("products.updated", unit: "{product}", description: "Productos actualizados");

    public static readonly Counter<long> ProductsDeleted =
        Meter.CreateCounter<long>("products.deleted", unit: "{product}", description: "Productos eliminados");

    public static readonly Histogram<double> ProductPrice =
        Meter.CreateHistogram<double>("products.price", unit: "USD", description: "Precio de los productos creados o actualizados");
}
