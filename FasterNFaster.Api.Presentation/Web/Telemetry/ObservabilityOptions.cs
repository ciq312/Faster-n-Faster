using OpenTelemetry.Exporter;

namespace FasterNFaster.Api.Presentation.Web.Telemetry;

internal sealed class ObservabilityOptions
{
    public const string SectionName = "Observability";

    public bool Enabled { get; set; }
    public string ServiceName { get; set; } = "";
    public Uri? OtlpEndpoint { get; set; }
    public OtlpExportProtocol OtlpProtocol { get; set; }
    public TimeSpan MetricExportInterval { get; set; }
}
