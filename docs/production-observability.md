# Production observability runbook

The monitoring stack lives in a separate Compose project on the VPS. Alloy joins the application's Docker network to receive OTLP at `fnf-alloy:4317`. Grafana runs on loopback and is available through an SSH tunnel. The public uptime check and its delivery to Telegram run outside the VPS.

## Prepare the VPS

Deploy a release that includes dependency readiness, the Caddy access-log volume, and the API telemetry changes through the normal SHA-pinned release workflow. Confirm that `https://faster-n-faster.com/api/health` returns HTTP `200` with body `Healthy`. The API reads the telemetry flag from its deployment settings.

Check resource headroom before starting the monitoring containers. Check the filesystem that actually stores Docker's data:

```bash
free -h
docker stats --no-stream
docker_root=$(docker info --format '{{.DockerRootDir}}')
df -hT / "$docker_root"
df -i / "$docker_root"
test -S /run/containerd/containerd.sock
```

If Docker manages its own containerd instance, its socket may be at `/var/run/docker/containerd/containerd.sock`. Set `OBSERVABILITY_CONTAINERD_SOCKET` in the monitoring settings to the socket Docker uses. This gives cAdvisor access to container metadata for the Compose panels.

On the VPS, copy the configuration from the deployed commit into a directory outside the application's release checkout:

```bash
monitoring_dir=/opt/Faster-n-Faster-monitoring
sudo install -d -m 0750 -o "$(id -u)" -g "$(id -g)" "$monitoring_dir"
git -C /opt/Faster-n-Faster archive HEAD \
  docker-compose.monitoring.prod.yml observability/production observability/grafana \
  | tar -xf - -C "$monitoring_dir"
```

Keep this directory mounted and its named volumes intact across monitoring upgrades. The separate copies let application rollback switch the worktree by SHA while Grafana stays running.

## Set up Grafana's password and Compose

Run this once. Grafana runs as UID 472 and reads the password from the mounted file. Store the generated value in your password manager before starting Grafana.

```bash
app_project=$(docker inspect --format '{{ index .Config.Labels "com.docker.compose.project" }}' backend)
app_network="${app_project}_default"
caddy_logs=$(docker inspect --format '{{range .Mounts}}{{if eq .Destination "/var/log/caddy"}}{{.Name}}{{end}}{{end}}' caddy)
docker network inspect "$app_network" --format '{{.Name}}'
docker volume inspect "$caddy_logs" --format '{{.Name}}'

(
  set -euC
  umask 077
  openssl rand -hex 24 > "$monitoring_dir/grafana-admin-password"
  printf 'FNF_APP_DOCKER_NETWORK=%s\nFNF_CADDY_LOG_VOLUME=%s\nGRAFANA_ADMIN_PASSWORD_FILE=%s\nOBSERVABILITY_DOCKER_ROOT=%s\n' \
    "$app_network" "$caddy_logs" "$monitoring_dir/grafana-admin-password" "$docker_root" \
    > "$monitoring_dir/settings.conf"
)
sudo chown 472:0 "$monitoring_dir/grafana-admin-password"
sudo chmod 0400 "$monitoring_dir/grafana-admin-password"
```

For an application using a custom Docker network, use the actual shared network name in `FNF_APP_DOCKER_NETWORK`. The output of `docker inspect backend --format '{{json .NetworkSettings.Networks}}'` shows the current networks.

The Compose file documents retention, trace sampling, CPU and memory defaults as environment settings. The defaults cap monitoring containers at a combined 1,984 MiB of RAM and 2.45 CPUs. Prometheus keeps up to seven days or 2 GB of compacted data, Loki keeps 72 hours, and Tempo keeps 48 hours. Loki and Tempo also limit ingestion rates. Check how fast storage grows after real traffic arrives.

Alloy retains error traces, traces of at least 500 ms, and 5% of other traces, subject to the collector's buffer and memory limits. Caddy bounds its rotated access-log files before Alloy reads them. cAdvisor uses privileged host access and Docker and containerd sockets to collect container metrics. Its metrics endpoint is on the internal metrics network. The OOM panel depends on kernel events available to cAdvisor. The restart panel observes changes in container start times and may miss a replacement.

```bash
monitoring() {
  docker compose --env-file "$monitoring_dir/settings.conf" -p fnf-monitoring \
    -f "$monitoring_dir/docker-compose.monitoring.prod.yml" "$@"
}

monitoring config --quiet
monitoring up -d --wait --wait-timeout 180
monitoring ps
```

From your laptop, open an SSH tunnel using the host name or alias you use for the VPS:

```bash
ssh -N -L 13001:127.0.0.1:13000 YOUR_VPS
```

Visit [Grafana](http://127.0.0.1:13001). The initial user is `admin`. The password file initializes an empty Grafana volume. Use the account settings in Grafana to rotate an established password.

## Enable API export and verify

Add these entries to the application deployment environment on the VPS. Keep the existing database, auth and SMTP values in place:

```dotenv
Observability__Enabled=true
Observability__OtlpEndpoint=http://fnf-alloy:4317
```

Recreate the API under its current release:

```bash
cd /opt/Faster-n-Faster
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --no-deps api
```

The production Compose file attaches the deployed `IMAGE_TAG` to API telemetry as `service.version`. Prometheus exposes the SHA as `service_version` on `target_info{job="faster-n-faster-api"}`. Check **Faster-n-Faster API** and **Faster-n-Faster VPS** in Grafana. The API dashboard includes request rate, p95 and error rate by route, race frames and persistence, and the Npgsql connection pool. The VPS dashboard includes disk space and inodes, Docker resource usage, and Redis availability and evictions.

In **Explore → Loki**, look for HTTP and background errors using these queries. Expand a log entry and follow its trace ID to Tempo when the collector retained that trace. Tempo's span view links back to the logs for the same trace.

```logql
{service_name="faster-n-faster-api"} | TelemetryOperation="http.error"
{service_name="faster-n-faster-api"} | TelemetryOperation=~"race[.].*[.]failed"
{service_name="caddy", status=~"502|504"}
```

## Route internal alerts to Telegram

In Grafana, add a Telegram **Contact point** with your bot token and chat ID. Send a test notification from Grafana, then select that contact point in the notification policy for the provisioned rules. Keep the token in Grafana or in deployment secrets. The rules for readiness, HTTP 5xx and host disk availability are visible under **Alerting → Alert rules**.

## Set up the independent uptime check

In [HetrixTools](https://hetrixtools.com/uptime-monitor/), create one **Website** monitor named `Faster-n-Faster readiness`. Use `https://faster-n-faster.com/api/health`, HTTP GET, and accepted HTTP code `200`. Set the keyword to `Healthy` after verifying the deployed readiness response. Choose the outside probe locations for this monitor. Connect a Telegram contact list using the HetrixTools bot and assign it to the monitor. Verify that the monitor shows **Online** and that its contact list delivers a test notification. A free account needs a sign-in at least once every 90 days. [Website monitor](https://docs.hetrixtools.com/add-a-website-uptime-monitor/), [HTTP status configuration](https://docs.hetrixtools.com/website-uptime-monitoring-accepted-http-codes/), [Telegram integration](https://docs.hetrixtools.com/telegram-integration/)

This monitor checks the public HTTPS path through Caddy and application readiness. Its alert delivery remains available during a VPS outage.

## Stop or roll back

To disable API export, set `Observability__Enabled=false` in the application deployment environment and recreate the API as above. Run `monitoring down` to stop the monitoring services and keep their storage.

For an application rollback, use the usual Deploy workflow with the previous SHA. Monitoring keeps its own Compose project and data. A release from before the telemetry and Caddy log additions provides the signals available in that release. The external HTTP check continues to validate the publicly visible status.
