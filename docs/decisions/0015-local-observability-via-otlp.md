# 0015. Local observability via OTLP

- **Status:** Accepted
- **Date:** 2026-11-19
- **Sources:** [Local profiling and race instrumentation](https://github.com/ciq312/Faster-n-Faster/pull/66), the [OpenTelemetry .NET SDK](https://opentelemetry.io/docs/languages/dotnet/) and the [Grafana LGTM development image](https://github.com/grafana/docker-otel-lgtm).

## Context

The profiling project captures runtime counters and sampled stacks for repeatable performance work. Diagnosing a slow request or SignalR operation also calls for service latency, operation spans and related logs.

## Decision

Use dependency inversion at the telemetry boundary. The API emits OpenTelemetry metrics, traces and selected Serilog events. OTLP configuration chooses the destination outside application code. The local profiling project supplies a preconfigured Grafana LGTM destination for development.

## How it works

With `Observability:Enabled` set, the API subscribes to hosting, HTTP connection, .NET runtime and race meters. The ASP.NET Core and Npgsql activity sources provide request and database spans. SignalR connection initialization and race start have spans with correlated log events. The application filters trace attributes and log events before export.

The overlay sends OTLP to the Collector over the Compose network. Prometheus stores metrics, Tempo stores traces and Loki stores logs. Grafana links logs and traces by trace ID and provisions an API dashboard and alert rules. Its HTTP port is bound to loopback.

## Alternatives considered

- Local counters and sampled stacks support performance attribution. They provide no continuous request or operation correlation.
- Separate local backends provide more control over each store. The bundled development image reduces setup for exercising the OTLP pipeline.

## Consequences

The profiling stack can verify the pipeline locally. The application can use another OTLP backend through configuration. Retention, sampling, host metrics and alert routing for a deployment need their own resource budget and credentials. [Runbook](../local-observability.md)
