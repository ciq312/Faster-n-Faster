using FasterNFaster.Api.Presentation.Web.Telemetry;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog.Events;
using SerilogOtlpProtocol = Serilog.Sinks.OpenTelemetry.OtlpProtocol;

namespace FasterNFaster.Api.Presentation.Extensions;

internal static class ObservabilityExtensions
{
    private const string MetricsSignal = "metrics";
    private const string TracesSignal = "traces";

    public static void AddObservability(this WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;
        var section = configuration.GetSection(ObservabilityOptions.SectionName);
        services.AddOptions<ObservabilityOptions>()
            .Bind(section)
            .PostConfigure(options => options.ServiceName = configuration["OTEL_SERVICE_NAME"] ?? options.ServiceName)
            .Validate(options => !options.Enabled || !string.IsNullOrWhiteSpace(options.ServiceName),
                "Observability:ServiceName must be configured when telemetry is enabled.")
            .Validate(options => !options.Enabled || options.OtlpEndpoint is null or { IsAbsoluteUri: true, Scheme: "http" or "https" },
                "Observability:OtlpEndpoint must be an absolute HTTP or HTTPS URI.")
            .Validate(options => !options.Enabled || Enum.IsDefined(options.OtlpProtocol),
                "Observability:OtlpProtocol must be Grpc or HttpProtobuf.")
            .Validate(options => !options.Enabled || options.MetricExportInterval.TotalMilliseconds is >= 1 and <= int.MaxValue,
                "Observability:MetricExportInterval must fit in a positive 32-bit millisecond interval.")
            .ValidateOnStart();

        services.AddSerilog((provider, logger) => ConfigureLogging(logger, provider, configuration));

        if (!section.GetValue<bool>(nameof(ObservabilityOptions.Enabled)))
            return;

        ConfigureExporter(services, MetricsSignal);
        ConfigureExporter(services, TracesSignal);
        services.AddOptions<MetricReaderOptions>(MetricsSignal)
            .Configure<IOptions<ObservabilityOptions>, IConfiguration>((reader, options, config) =>
            {
                if (string.IsNullOrWhiteSpace(config["OTEL_METRIC_EXPORT_INTERVAL"]))
                {
                    reader.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds =
                        (int)options.Value.MetricExportInterval.TotalMilliseconds;
                }
            });

        services.ConfigureOpenTelemetryMeterProvider((provider, metrics) =>
            metrics.ConfigureResource(resource => resource.AddService(GetOptions(provider).ServiceName)));
        services.ConfigureOpenTelemetryTracerProvider((provider, tracing) =>
            tracing.ConfigureResource(resource => resource.AddService(GetOptions(provider).ServiceName)));
        services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics
                .AddMeter("Microsoft.AspNetCore.Hosting", "Microsoft.AspNetCore.Http.Connections", "System.Runtime", "FasterNFaster.Races")
                .AddOtlpExporter(MetricsSignal, configure: null))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.Filter = context => !IsReadinessRequest(context);
                    options.EnableAspNetCoreSignalRSupport = false;
                })
                .AddSource("Npgsql", SignalRTelemetry.SourceName)
                .AddProcessor(new SafeTraceAttributesProcessor())
                .AddOtlpExporter(TracesSignal, configure: null));
    }

    public static void UseRequestLogging(this WebApplication app)
    {
        var enabled = GetOptions(app.Services).Enabled;
        app.UseSerilogRequestLogging(options =>
        {
            if (enabled)
                options.EnrichDiagnosticContext = MarkSafeRequestLog;
        });
    }

    private static ObservabilityOptions GetOptions(IServiceProvider provider) =>
        provider.GetRequiredService<IOptions<ObservabilityOptions>>().Value;

    private static void ConfigureExporter(IServiceCollection services, string signal)
    {
        services.AddOptions<OtlpExporterOptions>(signal)
            .Configure<IOptions<ObservabilityOptions>, IConfiguration>((exporter, options, configuration) =>
            {
                var settings = options.Value;
                if (string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_PROTOCOL"]))
                    exporter.Protocol = settings.OtlpProtocol;

                if (settings.OtlpEndpoint is { } endpoint &&
                    string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
                {
                    // The SDK expects the full signal URL when a protobuf endpoint is set in code.
                    exporter.Endpoint = exporter.Protocol == OtlpExportProtocol.HttpProtobuf
                        ? new UriBuilder(endpoint) { Path = $"{endpoint.AbsolutePath.TrimEnd('/')}/v1/{signal}" }.Uri
                        : endpoint;
                }
            });
    }

    private static void ConfigureLogging(LoggerConfiguration logger, IServiceProvider provider, IConfiguration configuration)
    {
        logger.MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .WriteTo.Console();

        var settings = GetOptions(provider);
        if (settings.Enabled)
        {
            var exporter = provider.GetRequiredService<IOptionsMonitor<OtlpExporterOptions>>().Get(TracesSignal);

            logger.WriteTo.Logger(telemetry => telemetry
                .Filter.ByIncludingOnly(logEvent => logEvent.Exception is null &&
                    logEvent.Properties.ContainsKey(SignalRTelemetry.OperationProperty))
                .WriteTo.OpenTelemetry(options =>
                {
                    options.Endpoint = (settings.OtlpEndpoint ?? exporter.Endpoint).AbsoluteUri;
                    options.Protocol = exporter.Protocol == OtlpExportProtocol.HttpProtobuf
                        ? SerilogOtlpProtocol.HttpProtobuf
                        : SerilogOtlpProtocol.Grpc;
                    options.ResourceAttributes = new Dictionary<string, object> { ["service.name"] = settings.ServiceName };
                    options.OnBeginSuppressInstrumentation = SuppressInstrumentationScope.Begin;
                }, getConfigurationVariable: key => configuration[key]));
        }
    }

    private static void MarkSafeRequestLog(IDiagnosticContext diagnosticContext, HttpContext context)
    {
        // A parameterized path can contain a lobby invite code.
        if (context.GetEndpoint() is RouteEndpoint { RoutePattern.Parameters.Count: 0 } && !IsReadinessRequest(context))
            diagnosticContext.Set(SignalRTelemetry.OperationProperty, "http.request");
    }

    private static bool IsReadinessRequest(HttpContext context) => context.Request.Path.Equals("/api/health", StringComparison.OrdinalIgnoreCase);
}
