# Local profiling

The performance stack uses a separate Compose project and does not read the development or production `.env`. It contains the API, test-only PostgreSQL/Redis, the existing SignalR load-test bots and a diagnostics sidecar with pinned `dotnet-counters` and `dotnet-trace` 10.0.750501. Database/cache ports are private to the Compose network; the API is published on `127.0.0.1:18080`. The local API has `RateLimiting:Enabled=false`; the normal application and production settings remain enabled. Anti-cheat stays enabled.

The diagnostics socket uses an explicit runtime `connect,nosuspend` port so API startup does not wait for the collector. The `profiling-runs` bind mount uses Docker's shared SELinux label (`:z`) because both the collector and load-test container write there.

## Run profiling and graphs

The full local workflow is orchestrated from the neighboring `Faster-n-Faster-profiling` repository. It builds and starts this Compose stack, runs the bots, collects runtime counters, then saves the raw data and graphs under `Faster-n-Faster-profiling/profiling/<label>/`.

```bash
cd ../Faster-n-Faster-profiling
python automation.py --users 5 --label local-5-users --wpm 120
```

Omit `--label` to generate a timestamped name. Use `--skip-build` to reuse the existing images. The default run collects `counters.csv`, the bot phase data and summary, and PNG graphs. The isolated Compose project is named `fnf-profile`; the orchestrator leaves it running so later runs can reuse its database and cache. Stop it when finished:

```bash
docker compose --env-file /dev/null -p fnf-profile \
  -f ../Faster-n-Faster/docker-compose.profiling.yml down
```

## Capture and view a trace

Use a trace run to investigate where the application spends time. The workflow runs one collector per capture. Use regular counter runs for performance comparisons because tracing adds overhead.

```bash
cd ../Faster-n-Faster-profiling
python automation.py --users 5 --label inspect-hot-path --wpm 120 --trace
```

The trace lasts 45 seconds by default. Set another duration in `DD:HH:MM:SS` format, for example one minute:

```bash
python automation.py --users 5 --label inspect-hot-path-long --trace \
  --trace-duration 00:00:01:00
```

Each trace run saves the original `.nettrace`, a per-thread `.speedscope.json` for **Time Order**, and an aggregated `.managed.speedscope.json` in `profiling/<label>/traces/`. For the aggregate, the renderer sums managed sample weights across threads and uses the bot phase timestamps to select typing and authentication windows. Import the managed file into [Speedscope](https://www.speedscope.app) and press **3** for **Sandwich**. Its default view shows application stacks during typing when those samples are available.

To rebuild the report from an existing run:

```bash
python trace_report.py profiling/inspect-hot-path/traces/trace-inspect-hot-path.nettrace
```

The command uses the existing per-thread JSON. For an older run with only a `.nettrace`, install the host converter with `dotnet tool install --global dotnet-trace`. The automatic workflow uses the converter in the diagnostics image. Pass `--no-graph` to collect raw artifacts without generating reports.

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

The bot command rejects non-local targets unless `--allow-remote` is explicitly supplied. It schedules typing against a monotonic clock, buffers keystrokes at the frontend's 100 ms cadence, flushes the final batch, and records late characters and schedule lateness. The sidecar writes one-second .NET runtime samples to `profiling-runs/counters.csv`. Run artifacts are written under the ignored `profiling-runs/<run-id>/` directory. `run-summary.json` uses schema version 2 and records pass/fail, UTC phase timestamps plus monotonic offsets, bot outcomes, Ping RTT and sampled send-to-observed-progress latency. A compatibility `phases.json` is also written at the output root for the existing graph renderer; its RTT values are labelled Ping RTT in the summary, not progress latency. Failures return a non-zero exit code and preserve partial bot artifacts.

## Race latency metrics

Counter runs also collect the `FasterNFaster.Races` meter. Use `typing_start` and `race_ended` from the run summary to compare the same phase across runs.

| Instrument | What it measures |
| --- | --- |
| `race.tick.duration` | Time spent processing a tick with registered lobbies. |
| `race.frame.publish.duration` | Time inside the call that hands the latest frame to the conflator. |
| `race.frame.age_at_send` | Time from publication to the start of the broadcast. |
| `race.frame.broadcast.duration` | Time until a successful server broadcast completes. |
| `race.frame.replaced` | Number of pending frames replaced by a newer frame. |
| `aggregate.gate.wait`, `aggregate.gate.hold` | Time to acquire the gate and time spent inside its critical section. Use `aggregate=Race` for race state. |

The CSV contains percentile estimates for each one-second interval. The median of those interval estimates describes a typical interval. The bot summary reports percentiles across the run's client samples. For a comparison of `RaceState` delivery, exclude the last sampled milestone for each bot. The runner can confirm that milestone with `RaceEnded`, which arrives after the entire lobby finishes.

Stop and remove only this profiling stack when finished:

```bash
docker compose --env-file /dev/null -p fnf-profile -f docker-compose.profiling.yml down
```

This removes the containers and network but preserves the profiling database, Redis and diagnostic socket volumes. Add `-v` only when intentionally resetting local profiling state. No automatic capacity claim is made from a run: generator and host saturation still need to be checked. Container/host metrics and targeted traces are the next implementation steps.

Resource limits can be adjusted through `PROFILE_API_CPUS`, `PROFILE_API_MEMORY`, `PROFILE_POSTGRES_CPUS`, `PROFILE_POSTGRES_MEMORY`, `PROFILE_REDIS_CPUS`, `PROFILE_REDIS_MEMORY`, `PROFILE_BOTS_CPUS`, `PROFILE_BOTS_MEMORY`, `PROFILE_DIAGNOSTICS_CPUS` and `PROFILE_DIAGNOSTICS_MEMORY`. Defaults total about 4.75 logical CPUs and 3.75 GiB of container memory. Keep limits fixed when comparing runs and record any override with the run. Check free host memory and swap before a run; host pressure makes a capacity result inconclusive.
