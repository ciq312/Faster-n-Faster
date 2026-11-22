using System.Diagnostics.Metrics;
using FasterNFaster.Api.Infrastructure.Shared.Telemetry;
using FasterNFaster.Api.Presentation.Web.Telemetry;
using FasterNFaster.Api.UseCases.Shared;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog.Core;
using Serilog.Events;
using SerilogOtlpProtocol = Serilog.Sinks.OpenTelemetry.OtlpProtocol;

namespace FasterNFaster.Api.Presentation.Extensions;

internal static class ObservabilityExtensions
{
    private const string MetricsSignal = "metrics";
    private const string TracesSignal = "traces";
    private const string NpgsqlTelemetry = "Npgsql";
    private const string HttpRequestOperation = "http.request";

    public static void AddObservability(this WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;
        var section = configuration.GetSection(ObservabilityOptions.SectionName);
        services.AddOptions<ObservabilityOptions>()
            .Bind(section)
            .PostConfigure(options =>
            {
                options.ServiceName = configuration["OTEL_SERVICE_NAME"] ?? options.ServiceName;
                if (string.IsNullOrWhiteSpace(options.ServiceVersion))
                    options.ServiceVersion = null;
            })
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
            metrics.ConfigureResource(resource => ConfigureResource(resource, GetOptions(provider))));
        services.ConfigureOpenTelemetryTracerProvider((provider, tracing) =>
            tracing.ConfigureResource(resource => ConfigureResource(resource, GetOptions(provider))));
        services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics
                .AddMeter("Microsoft.AspNetCore.Hosting", "Microsoft.AspNetCore.Http.Connections", "System.Runtime",
                    RacePerformanceMetrics.MeterName, NpgsqlTelemetry)
                .AddView(ConfigureNpgsqlMetric)
                // Filtered pool attributes can still appear as exemplar attributes.
                .SetExemplarFilter(ExemplarFilterType.AlwaysOff)
                .AddOtlpExporter(MetricsSignal, configure: null))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.Filter = context => !IsReadinessRequest(context);
                    options.EnableAspNetCoreSignalRSupport = false;
                })
                .AddSource(NpgsqlTelemetry, SignalRTelemetry.SourceName)
                .AddProcessor(new SafeTraceAttributesProcessor())
                .AddOtlpExporter(TracesSignal, configure: null));
    }

    public static void UseRequestLogging(this WebApplication app)
    {
        var enabled = GetOptions(app.Services).Enabled;
        app.UseSerilogRequestLogging(options =>
        {
            if (enabled)
                options.GetMessageTemplateProperties = GetRequestLogProperties;
        });
    }

    private static ObservabilityOptions GetOptions(IServiceProvider provider) =>
        provider.GetRequiredService<IOptions<ObservabilityOptions>>().Value;

    private static void ConfigureResource(ResourceBuilder resource, ObservabilityOptions settings) =>
        resource.AddService(settings.ServiceName, serviceVersion: settings.ServiceVersion);

    private static MetricStreamConfiguration? ConfigureNpgsqlMetric(Instrument instrument) =>
        instrument.Meter.Name == NpgsqlTelemetry
            ? instrument.Name switch
            {
                "db.client.connection.count" => new MetricStreamConfiguration { TagKeys = ["db.client.connection.state"] },
                "db.client.connection.max" or
                "db.client.connection.npgsql.pending_requests" or
                "db.client.connection.npgsql.timeouts" or
                "db.client.operation.duration" or
                "db.client.operation.failed" => new MetricStreamConfiguration { TagKeys = [] },
                _ => MetricStreamConfiguration.Drop,
            }
            : null;

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
        logger.ReadFrom.Configuration(configuration)
            .WriteTo.Logger(console => console
                .Filter.ByExcluding(logEvent => logEvent.Exception is null && SafeTelemetryLog.IsErrorOperation(GetOperation(logEvent)))
                .WriteTo.Console());

        var settings = GetOptions(provider);
        if (settings.Enabled)
        {
            var exporter = provider.GetRequiredService<IOptionsMonitor<OtlpExporterOptions>>().Get(TracesSignal);
            var resourceAttributes = new Dictionary<string, object> { ["service.name"] = settings.ServiceName };
            if (settings.ServiceVersion is { } serviceVersion)
                resourceAttributes["service.version"] = serviceVersion;

            logger.WriteTo.Logger(telemetry => telemetry
                .Filter.ByIncludingOnly(IsExportableLog)
                .Enrich.With<SafeLogPropertiesEnricher>()
                .WriteTo.OpenTelemetry(options =>
                {
                    options.Endpoint = (settings.OtlpEndpoint ?? exporter.Endpoint).AbsoluteUri;
                    options.Protocol = exporter.Protocol == OtlpExportProtocol.HttpProtobuf
                        ? SerilogOtlpProtocol.HttpProtobuf
                        : SerilogOtlpProtocol.Grpc;
                    options.ResourceAttributes = resourceAttributes;
                    options.OnBeginSuppressInstrumentation = SuppressInstrumentationScope.Begin;
                }, getConfigurationVariable: key => configuration[key]));
        }
    }

    private static bool IsExportableLog(LogEvent logEvent)
    {
        if (logEvent.Exception is not null)
            return false;

        var operation = GetOperation(logEvent);
        return !string.IsNullOrWhiteSpace(operation);
    }

    private static string? GetOperation(LogEvent logEvent) =>
        logEvent.Properties.TryGetValue(SafeTelemetryLog.OperationProperty, out var value) &&
            value is ScalarValue { Value: string operation }
            ? operation
            : null;

    private static IEnumerable<LogEventProperty> GetRequestLogProperties(HttpContext context, string path, double elapsedMs, int statusCode)
    {
        var routeTemplate = context.GetEndpoint() is RouteEndpoint endpoint && !IsReadinessRequest(context)
            ? endpoint.RoutePattern.RawText
            : null;

        yield return new LogEventProperty("RequestMethod", new ScalarValue(context.Request.Method));
        yield return new LogEventProperty("RequestPath", new ScalarValue(routeTemplate ?? path));
        yield return new LogEventProperty("StatusCode", new ScalarValue(statusCode));
        yield return new LogEventProperty("Elapsed", new ScalarValue(elapsedMs));
        if (routeTemplate is not null)
            yield return new LogEventProperty(SafeTelemetryLog.OperationProperty, new ScalarValue(HttpRequestOperation));
    }

    private static bool IsReadinessRequest(HttpContext context) => context.Request.Path.Equals("/api/health", StringComparison.OrdinalIgnoreCase);

    private sealed class SafeLogPropertiesEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var operation = GetOperation(logEvent);
            foreach (var property in logEvent.Properties.Keys.ToArray())
            {
                if (property is SafeTelemetryLog.OperationProperty or "SourceContext" ||
                    operation == HttpRequestOperation && property is "RequestMethod" or "RequestPath" or "StatusCode" or "Elapsed" ||
                    SafeTelemetryLog.IsErrorOperation(operation) && property is "ErrorKind" or "RouteTemplate" or "ErrorCode" or "StatusCode" ||
                    GetAllowedProperties(operation).Contains(property))
                {
                    continue;
                }

                logEvent.RemovePropertyIfPresent(property);
            }
        }

        private static string[] GetAllowedProperties(string? operation) => operation switch
        {
            TelemetryOperations.RaceCountdownStarted or TelemetryOperations.RaceFinished => ["RaceId", "LobbyId"],
            TelemetryOperations.PlayerKicked => ["TargetPlayerId", "UserId", "LobbyId"],
            TelemetryOperations.PassageChanged => ["LobbyId", "UserId"],
            TelemetryOperations.PlayerJoined => ["PlayerId", "LobbyId"],
            TelemetryOperations.HostTransferred => ["TargetPlayerId", "HostId", "LobbyId"],
            TelemetryOperations.PlayerDisconnected => ["PlayerId", "LobbyId"],
            TelemetryOperations.UserCredentialsIssued or TelemetryOperations.UserCredentialsRevoked => ["UserId"],
            TelemetryOperations.UserRegisteredAnonymously => ["UserId", "Nick"],
            "signalr.connect" => ["UserId"],
            _ => [],
        };
    }
}
