# Local profiling

The performance stack uses a separate Compose project and does not read the development or production `.env`. It contains the API, test-only PostgreSQL/Redis, the existing SignalR load-test bots and a diagnostics sidecar with pinned `dotnet-counters` 10.0.750501. Database/cache ports are private to the Compose network; the API is published on `127.0.0.1:18080`. The local API has `RateLimiting:Enabled=false`; the normal application and production settings remain enabled. Anti-cheat stays enabled.

The diagnostics socket uses an explicit runtime `connect,nosuspend` port so API startup does not wait for the collector. The `profiling-runs` bind mount uses Docker's shared SELinux label (`:z`) because both the collector and load-test container write there.

Start the isolated API and dependencies:

```bash
mkdir -p profiling-runs
export PROFILE_UID="$(id -u)" PROFILE_GID="$(id -g)"
docker compose --env-file /dev/null -p fnf-profile -f docker-compose.profiling.yml up --build -d api
```

Run a small scenario (the default is five bots at 120 WPM):

```bash
docker compose --env-file /dev/null -p fnf-profile -f docker-compose.profiling.yml run --rm loadtest
```

Or override the bot count and speed:

```bash
PROFILE_USERS=10 PROFILE_WPM=120 docker compose --env-file /dev/null -p fnf-profile -f docker-compose.profiling.yml run --rm loadtest
```

The bot command rejects non-local targets unless `--allow-remote` is explicitly supplied. It schedules typing against a monotonic clock, coalesces progress sends to the frontend's 100 ms cadence, flushes the final progress, and records late characters and schedule lateness. The sidecar writes one-second .NET runtime samples to `profiling-runs/counters.csv`. Run artifacts are written under the ignored `profiling-runs/<run-id>/` directory. `run-summary.json` uses schema version 2 and records pass/fail, UTC phase timestamps plus monotonic offsets, bot outcomes, Ping RTT and sampled send-to-observed-progress latency. A compatibility `phases.json` is also written at the output root for the existing graph renderer; its RTT values are labelled Ping RTT in the summary, not progress latency. Failures return a non-zero exit code and preserve partial bot artifacts.

Stop and remove only this profiling stack when finished:

```bash
docker compose --env-file /dev/null -p fnf-profile -f docker-compose.profiling.yml down
```

This removes the containers and network but preserves the profiling database, Redis and diagnostic socket volumes. Add `-v` only when intentionally resetting local profiling state. No automatic capacity claim is made from a run: generator and host saturation still need to be checked. Container/host metrics and targeted traces are the next implementation steps.

Resource limits can be adjusted through `PROFILE_API_CPUS`, `PROFILE_API_MEMORY`, `PROFILE_POSTGRES_CPUS`, `PROFILE_POSTGRES_MEMORY`, `PROFILE_REDIS_CPUS`, `PROFILE_REDIS_MEMORY`, `PROFILE_BOTS_CPUS`, `PROFILE_BOTS_MEMORY`, `PROFILE_DIAGNOSTICS_CPUS` and `PROFILE_DIAGNOSTICS_MEMORY`. Defaults total about 4.75 logical CPUs and 3.75 GiB of container memory. Keep limits fixed when comparing runs and record any override with the run. Check free host memory and swap before a run; host pressure makes a capacity result inconclusive.
