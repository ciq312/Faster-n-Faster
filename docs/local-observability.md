# Local observability runbook

The observability overlay adds Grafana, Prometheus, Loki and Tempo to the local profiling project. It reuses the project's test database, cache and bots. Starting with the overlay recreates the local API to enable OTLP export. Grafana listens on loopback at port 13000. The collector receives traffic inside the Compose network.

## 1. Create the local Grafana password

Run once:

```bash
(
  umask 077
  set -C
  mkdir -p "$HOME/.config/faster-n-faster"
  printf 'GRAFANA_ADMIN_PASSWORD=%s\n' "$(openssl rand -hex 24)" \
    > "$HOME/.config/faster-n-faster/observability.conf"
)
```

Keep this file on your machine. Sign in as `admin` with the value of `GRAFANA_ADMIN_PASSWORD`. Grafana uses it when it initializes the telemetry volume. To change an existing password, use the Grafana account settings and record the new value in your password manager. Optional `OBSERVABILITY_GRAFANA_PORT` in the same file changes the host port.

To show the initial password in your terminal:

```bash
sed -n 's/^GRAFANA_ADMIN_PASSWORD=//p' "$HOME/.config/faster-n-faster/observability.conf"
```

Local OTLP requires no API key. The profiling Compose file supplies the test JWT signing key, PostgreSQL password and dummy OAuth configuration. You can run the bots with those fixtures.

## 2. Start the stack and produce telemetry

Run from the repository root:

```bash
obs() {
  docker compose --env-file "$HOME/.config/faster-n-faster/observability.conf" -p fnf-profile \
    -f docker-compose.profiling.yml -f docker-compose.observability.yml "$@"
}

obs up -d --no-build --wait --wait-timeout 180 api
obs ps -a
PROFILE_USERS=5 PROFILE_WPM=120 obs run --rm --no-deps -T loadtest
```

These commands use existing local images. If an image is missing or the source changed, build first with `obs build api migrate diagnostics loadtest`. For a Docker installation whose default BuildKit cache has missing snapshots, select a clean builder. The locally created recovery builder is available through `obs build --builder fnf-profile-recovery api migrate diagnostics loadtest`. Check the available builders with `docker buildx ls`.

When switching an already running profiling stack to the overlay, run `obs stop api diagnostics` before `obs up`. The collector starts a fresh session before the API connects.

Keep the observability overlay on Compose commands during the session so the API retains its export settings. A successful bot run writes a summary under `profiling-runs/<run-id>/`.

## 3. Find a race in Grafana

Open [Grafana](http://127.0.0.1:13000) and choose **Dashboards → Faster-n-Faster API**. Set the range to **Last 15 minutes**. API metrics are exported every ten seconds. The process CPU panel shows cores used, and the RAM panel shows the API process working set.

1. In **Explore**, select **Tempo**, switch to the TraceQL editor and run:
   ```traceql
   { resource.service.name = "faster-n-faster-api" && name = "signalr.start_race" }
   ```
2. Open a trace, choose the `signalr.start_race` span and use **Logs for this span**. The correlated Loki event contains `Race started`. You can also start in **Explore → Loki** with:
   ```logql
   {service_name="faster-n-faster-api"} |= "Race started"
   ```
   Use the event's `trace_id` link to open Tempo.
3. Open **Alerting → Alert rules**. Readiness alerts after a two-minute window without a successful probe and another two minutes in the pending state. The HTTP rule alerts when at least 5% of at least 20 requests in five minutes return 5xx for two minutes. Set up a contact point separately if you want notifications.

If the dashboard is empty, verify the API, telemetry and bot statuses. Grafana may need two metric export intervals after a fresh start.

```bash
obs ps -a
obs logs --tail 60 telemetry api
```

If the API health probe reports `Connection refused` and the diagnostic collector is waiting on its socket, stop the API before restarting the collector. Then let Compose start the API after the new socket is healthy:

```bash
obs stop api
obs restart diagnostics
obs up -d --no-build --wait --wait-timeout 180 api
```

Use the local `.nettrace` and Speedscope workflow for CPU profiling by method. [Local profiling](local-profiling.md)

To exercise the readiness rule locally, pause Redis with `obs pause redis`. Watch the rule go pending and then fire. Resume it with `obs unpause redis` and check that the rule returns to normal.

## 4. Stop the stack

```bash
obs down
```

The named volumes retain the profiling database, cache and Grafana state. The overlay configures 24-hour retention for metrics and logs. To free local telemetry storage and initialize Grafana with a fresh password, remove only the telemetry volume after stopping the stack. This deletes the dashboard state, logs, traces and metrics stored there.

```bash
docker volume rm fnf-profile_profile_telemetry
```

To continue profiling with telemetry stopped, use the base Compose file:

```bash
docker compose --env-file /dev/null -p fnf-profile \
  -f docker-compose.profiling.yml up -d --no-build api
```

## Before enabling telemetry on the VPS

Choose where to store the signals after measuring local CPU, RAM and disk use. The local all-in-one image and the profiling fixtures are for development. For a deployed collector, set `Observability__Enabled=true` and set `Observability__OtlpEndpoint` to the collector's base URL. The `Observability` section in [appsettings.json](../FasterNFaster.Api.Presentation/appsettings.json) supplies the service name, transport and export interval defaults. Use `Observability__OtlpProtocol` for `Grpc` or `HttpProtobuf` and `Observability__MetricExportInterval` for a TimeSpan such as `00:00:30`.

When `OtlpEndpoint` is unspecified, exporters use the SDK's protocol defaults. That means loopback port 4317 for gRPC and 4318 for HTTP/protobuf.

Existing `OTEL_SERVICE_NAME`, `OTEL_EXPORTER_OTLP_ENDPOINT`, `OTEL_EXPORTER_OTLP_PROTOCOL` and `OTEL_METRIC_EXPORT_INTERVAL` overrides take precedence. If the destination requires authentication, supply its write token through the environment's secret store and use the provider's `OTEL_EXPORTER_OTLP_HEADERS` format. Use TLS for a collector outside the private network and separate Grafana credentials for its users.

Set trace sampling and retention for the deployment. Connect the readiness and HTTP error rules to the intended contact point. Add host filesystem metrics and a free-space alert using the actual VPS mountpoints.
