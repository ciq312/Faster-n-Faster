# 0014. Local load testing and profiling with a measured generator

- **Status:** Proposed
- **Date:** 2026-11-16
- **Sources:** Local performance-testing design discussion (2026-11-16); current [`loadtest/`](../../loadtest/), [profiling orchestrator](https://github.com/ciq312/Faster-n-Faster-profiling), and [race tick design](0007-race-tick-with-latest-frame-conflation.md).

## Context

There is no separate test VPS. We need to repair the performance tooling, establish repeatable measurements on a laptop, and identify inefficient code before considering remote capacity tests. This record is an implementation plan; the environment and measurements below are not implemented yet.

The existing .NET bots use the official SignalR client. Python orchestration starts both the bots and `dotnet-counters` on the remote backend host, then downloads data and renders graphs locally. This mixes generator resource consumption with the system under test. It also assumes `/opt/tools/dotnet-counters` exists in the backend image, although the current application Dockerfile does not install it.

The current evidence is insufficient to establish capacity:

- `Ping` RTT is labelled `UpdateRaceState RTT` in the graph renderer. It does not measure when a player sees accepted progress.
- Some bot errors are only logged, and the race-end timeout does not fail the run.
- There is no measurement of whether bots sustain their intended message rate.
- Guest authentication and lobby creation are rate-limited per IP, so a single generator can hit admission limits before exercising the race workload.
- The suspected contract drift must be verified against a running build. `UpdateRaceState(int index, int mistakes, string typed)` still matches the local hub; batched keystrokes remain a proposal in [0011](0011-batched-keystroke-events-replayed-on-the-server.md).

## Decision

Use an isolated local Docker Compose environment, retain the .NET SignalR bots, and adapt the Python scripts to local orchestration and reporting. Apply separation of concerns: generating load, validating game outcomes, collecting metrics, recording diagnostic traces, and rendering reports are separate steps with explicit outputs.

Measure the maximum **sustainable workload of the recorded local configuration**, subject to agreed latency and correctness limits. Do not infer production VPS capacity from laptop results or from matching CPU/RAM limits. A run limited by the generator or the host is inconclusive about backend capacity.

### Execution environment

| Component | Where it runs | Purpose |
|---|---|---|
| API | Dedicated container, Release build, no debugger | Same application build path as production, pinned to the tested revision |
| PostgreSQL and Redis | Separate containers with test-only volumes | Reproducible data and cache state, isolated from development and production |
| Bots | Separate .NET container on the test network | HTTP + SignalR clients, workload scheduling, outcome validation and client measurements |
| Diagnostics | Separate tools container | Pinned `dotnet-counters` and `dotnet-trace` versions, attached to the API; bot counters when diagnosing the generator |
| Orchestrator | Python on the laptop in a virtual environment | Start/stop Compose services and collectors, write run metadata, gather artifacts |
| Graph renderer | Existing Python renderer, optionally its container, after collection stops | Produce charts and comparisons without competing with the measured workload |

The intended flow is:

```text
laptop: Python orchestrator
  └─ isolated Compose project
       ├─ bots ── HTTP / SignalR ──> API ──> PostgreSQL + Redis
       └─ diagnostics ── diagnostic socket ──> API

bot results + runtime metrics + container/host metrics + optional traces
  └─ one run directory on the laptop ──> summary and graphs
```

- Add a dedicated performance Compose configuration in the application repo, alongside the bots. Do not inherit fixed container names, development volumes or production environment files blindly from the existing Compose files. Use a distinct project name, network, volumes and test configuration; bind any host-facing ports to loopback.
- Keep orchestration and reporting in `Faster-n-Faster-profiling` initially. Record both repository revisions so artifacts remain reproducible across the two repositories.
- Start with direct API traffic and label it accordingly. A later, separate scenario can include Caddy/TLS; frontend rendering is not part of the initial backend test. Playwright can validate a small browser journey separately.
- Set and verify explicit CPU, memory and swap settings for the API and dependencies. Leave capacity for the generator, diagnostics and OS. Select concrete limits after inspecting the laptop, then keep them fixed for comparisons. CPU sets may reduce contention where the host supports them; CPU quotas alone do not reserve physical cores.
- Record host CPU model/core topology, RAM, OS/kernel, Docker/runtime versions and any Docker VM limits. Avoid builds and graph rendering during collection. Record host pressure; containers still share physical resources.
- Attach diagnostics using the supported Linux sidecar setup: share the API PID namespace and diagnostic socket directory, and use a UID permitted to access the socket. Keep tools out of the application image. Verify attachment before applying load. The sidecar still creates some work and buffers inside the target process.
- Native execution of the bots is allowed for debugging a small scenario. Reference measurements use the container configuration consistently. No additional VM is required.

### Workload and validity

First establish a small contract smoke test: authenticate, connect, create/join a lobby, receive its passage, start a race, observe server progress, finish, and validate results. Run this against the exact application image before a larger test. Use typed contract adapters where practical and keep the wire-level smoke test so stale deployed images cannot silently pass.

The race workload should reproduce the current frontend's send cadence, final flush, typing rules and connection lifecycle. Specify players per lobby, active versus idle players, typing-speed distribution, passage length, random seed and reconnect/error behaviour. Begin with clean typing; add mistakes, corrections and reconnects as separate scenarios. Reset bot state between races and continue racing for a timed steady phase rather than stopping after one passage.

For the local race-capacity profile, start the API with the existing `RateLimiting:Enabled` switch set to `false`. The planned performance Compose configuration sets this environment variable on the API service:

```yaml
environment:
  RateLimiting__Enabled: "false"
```

This bypasses the global and named HTTP rate-limit policies, allowing many bots from one IP to authenticate and create lobbies without artificial admission failures. Apply the override only to the dedicated performance configuration; keep the shared application and production defaults enabled. Record the effective value in each run manifest and verify it during preflight. A 429 in this profile is an unexpected failure to investigate, not an error for the bots to hide or retry indefinitely.

Prepare guest sessions outside the measured steady phase. Test authentication/admission limits in a separate profile with `RateLimiting__Enabled: "true"` and normal policy values. The HTTP switch does not disable the race progress budget or anti-cheat; keep those active for valid player traffic. A guest-only run does not establish the cost of registered-player statistics persistence; add seeded registered users in a later scenario.

Schedule intended sends independently of completion time and record scheduling lateness. Do not silently reduce offered load when sends get slow. Bound pending work and record backpressure or missed sends instead of creating an unlimited queue. Do not emit catch-up bursts that violate the intended player behaviour.

A slow send alone does not prove that the generator is exhausted: it can reflect server or network backpressure. Correlate bot scheduling/resource measurements with API and host measurements. Preserve a server-side overload finding when the generator has headroom; mark attribution inconclusive when the evidence cannot separate the causes.

A run must report separately:

- **Correctness:** expected membership, accepted progress and final results; errors, bans, disconnects and timeouts; missing results are failures rather than missing samples.
- **Load achieved:** requested versus actual active racers, send rate and schedule adherence. If bots or the host cannot sustain the target, report an inconclusive capacity result and stop increasing load.
- **Performance:** phase-specific latency distributions, throughput and resource usage at the achieved workload.

Thresholds belong to the versioned scenario. Start with zero unexpected protocol/game failures; choose latency limits and allowed scheduling drift after a low-load baseline, before a capacity sweep. A 200 ms broadcast tick is part of the latency budget. Keep thresholds fixed throughout a comparison. Invalid runs, threshold failures and missing mandatory artifacts produce a non-zero exit code with a reason.

### Metrics to retain

Collect time series at a configurable interval, initially one second, and retain raw data with units and phase boundaries. Preserve latency histogram counts/buckets per phase and bounded diagnostic samples; never average p95 values from different bots or runs.

| Area | Measurements |
|---|---|
| Workload | Requested, connected and actively racing players; active lobbies; lobby size; planned/actual progress sends per second; completed player-races and lobby-races per minute |
| HTTP and connections | Auth/create/join/connect duration distributions; status/error counts, including 429; hub invocation failures; reconnect attempts and unexpected disconnects |
| Progress latency | Send-to-observed-server-progress p50/p95/p99/max, sample count and timeouts; separately named Ping RTT on a small fixed sample of bots; received `RaceState` interval distribution |
| Server throughput | Progress updates received, accepted, deferred and rejected per second; frames produced, replaced by conflation and sent; recipient count/deliveries distinguished from frame count |
| Race tick | Target interval (currently 200 ms), actual ticks per second, tick duration and lateness/overruns; per-lobby timing distributions where useful |
| API runtime | CPU user/system time and rate, process working set, managed heap size, allocation bytes/s, GC collections/pause time, thread-pool queue/threads/completed work, exception rate |
| Containers | CPU usage and throttling, memory usage/limit, OOM/restarts, network bytes/s and block I/O for API, database, cache, bots and diagnostics |
| Dependencies | PostgreSQL connections, query duration/errors and pool waits where available; Redis command latency/errors and memory; race-result backlog and time to persist for registered-player scenarios |
| Generator | CPU/RAM, allocations/GC and thread-pool queue; send-scheduling lateness, missed sends, pending sends, send duration, receive-handler lag and actual connection count |
| Host | Overall CPU, available memory, swap activity and disk pressure; thermal/frequency indicators when available, with unsupported metrics marked explicitly |

Avoid an ambiguous `TPS` label. Record **progress updates/s**, **race ticks/s**, **broadcast frames/s**, **HTTP requests/s**, and **completed races/min** separately, with their scopes. A server tick, a player's update and a completed race are different units of work. Store raw CPU time and the normalization denominator so percentages can be interpreted against the CPU quota. Process working set, managed heap and container memory are distinct measurements.

Use a small sample of progress milestones to measure time from a send to observing server-confirmed progress on the same generator's monotonic clock. For clean typing, a later observed index can confirm that a milestone was reached; retain the correlation rule. Test observation by another player separately if needed. Latest-frame conflation intentionally skips intermediate snapshots, so do not expect an acknowledgement for every keystroke. Count unobserved milestones/timeouts and record the sampling rate. Wall-clock UTC timestamps align artifacts; they are not a substitute for a monotonic latency clock.

Use bounded metric labels such as operation, outcome and phase. Do not create a time series per player or lobby, or log every keystroke during capacity runs. Add missing application metrics incrementally at the hub, tick/conflator and persistence boundaries.

### Traces for code investigation

Normal comparison runs collect metrics without full tracing. When a repeatable slowdown appears, rerun that workload with a bounded diagnostic window, initially 30–60 seconds during the steady phase:

- Capture a CPU sampling trace with `dotnet-trace` and inspect stacks/flame graphs for hot methods.
- Capture GC/allocation or threading/contention events in a separate targeted trace when the metrics suggest those problems. Record the exact providers, profile and tool version; a CPU trace alone does not identify all allocation or async-wait causes.
- Preserve the original `.nettrace` and matching application symbols/build identity, plus any derived report. Record dropped events or collection errors and cap duration/file growth.
- Profile the bots separately if their scheduling or resource metrics indicate a generator bottleneck.

Mark every run as `metrics` or `diagnostic`. Tracing overhead can change the result; use the diagnostic run to explain behaviour, then validate an optimization in repeated metrics-only runs. Memory dumps are an on-demand follow-up, not a default artifact.

### Run artifacts

Use a unique run ID rather than overwriting a label such as `500users`. The planned output contract is:

```text
runs/<timestamp>-<run-id>/
  manifest.json          # revisions/dirty state, image IDs, tools, host, effective limits
  scenario.json          # workload, seed, thresholds, data/cache preparation
  phases.json            # phase boundaries, UTC start and monotonic offsets
  summary.json           # pass/fail/inconclusive, reasons, achieved load, per-phase stats
  metrics/               # API runtime, containers, host, generator, application metrics
  latencies/             # mergeable histograms and bounded correlated samples
  logs/                  # bounded bot/application/collector logs and failures
  traces/                # diagnostic runs only: raw traces and collection metadata
  graphs/                # derived after collection; can be regenerated
```

The manifest includes both source revisions, local change identity when dirty, runtime/GC configuration, image digests or local image IDs, topology (direct/proxy), instrumentation settings, resource limits and test-data/reset policy. Store effective settings without credentials or auth cookies. Version the artifact schema; retain legacy captures as historical data rather than silently relabelling them.

Keep bulk metrics, traces and graphs outside Git; commit scenarios, schemas and small deliberate comparison summaries. Bound disk use, retain baseline and investigation runs explicitly, and make cleanup a separate operation. Preserve partial artifacts on failure. The orchestrator must stop collectors and bots in `finally`, wait for output to flush, and clean up only resources belonging to its test run.

## Implementation sequence

1. **Repair correctness.** Run a few bots locally; reconcile the current auth/lobby/hub contracts and frontend cadence; validate progress/results; make exceptions, bans and timeouts visible in the result and exit code. Add a small contract smoke test.
2. **Build the local environment.** Add isolated Compose configuration, deterministic test data and fixed resource profiles. Adapt the host Python runner, pin images/tools, verify sidecar attachment and implement reliable cleanup/partial-artifact collection.
3. **Establish measurements.** Implement the artifact schema, bot throughput/scheduling metrics, server runtime and container/host metrics, correctly named RTT and progress latency. Add game/tick metrics needed to interpret the first baseline.
4. **Find the local operating range.** Start with 2–10 players; run explicit warm-up, ramp, steady and cooldown phases. Initial comparison defaults are 30–60 seconds of warm-up and 3–5 minutes steady load, extended when insufficient for JIT/cache stabilization or enough completed races. Increase load in steps, then repeat fixed-load runs around the observed boundary at least three times from equivalent state. Inspect recovery after disconnect and repeat a longer run for memory/backlog growth before calling the load sustainable.
5. **Investigate and verify.** Record a short trace at a reproducible problem load, identify the expensive path, change one factor, and compare against the baseline using the same environment/scenario and repeated metrics-only runs.
6. **Broaden coverage.** Add registered-player persistence, reconnects, mistakes/corrections and proxy/TLS scenarios. CI can run contract smoke tests and publish artifacts later; shared CI timings are not a stable capacity baseline.

## Alternatives considered

- **Stress-test the live VPS.** Risks disturbing players and mixes unknown real traffic with synthetic work. Production metrics and targeted incident diagnostics are separate from this local plan.
- **Rent a test VPS now.** Not available and not necessary to repair tooling or find code bottlenecks. It remains the next step for production-like capacity validation when resources permit.
- **Run everything natively.** Convenient for debugging, but makes dependencies, versions and resource accounting less reproducible. Keep this as a small-scenario development option.
- **Put the setup in a laptop VM.** Adds another resource layer without removing contention on the same physical machine. Docker is sufficient for the initial environment.
- **Replace the bots with k6 immediately.** k6 provides useful load scenarios, WebSocket metrics and thresholds, but SignalR still requires protocol-aware handling. Preserve the working official .NET client and first fix workload validity. k6 remains an option for independent HTTP scenarios.
- **Adopt NBomber immediately.** A possible C# scenario/reporting engine around the existing client if scheduling and reporting become costly to maintain. It is not a prerequisite for the first reliable baseline.
- **Install diagnostic tools inside the API image or trace every run.** Simpler attachment in the former case, but mixes tooling with the measured container; the latter changes measurement overhead. Prefer a tools sidecar and explicit diagnostic runs.

## Consequences

- Local comparisons become reviewable: the code, workload, achieved load, resource configuration and raw evidence travel together.
- We can distinguish application degradation, dependency bottlenecks and generator exhaustion, while acknowledging that one laptop cannot fully isolate them.
- Sustainable capacity is a measured range under named conditions, not a universal player count or a production guarantee.
- Some custom instrumentation and bot validation are required; adopting a load-testing framework would not remove that work.
- The first implementation needs only Docker Compose, the existing .NET client, Python and .NET diagnostic tools. A permanent monitoring platform is optional later.

## References

- [Collect .NET diagnostics in Linux containers](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/diagnostics-in-containers) and [diagnostic ports](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/diagnostic-port): sidecar attachment and access requirements.
- [dotnet-counters](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-counters) and [dotnet-trace](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace): runtime measurements and diagnostic collection.
- [Docker resource constraints](https://docs.docker.com/engine/containers/resource_constraints/): explicit CPU, memory and swap limits.
- [k6 WebSockets](https://grafana.com/docs/k6/latest/using-k6/protocols/websockets/), [k6 thresholds](https://grafana.com/docs/k6/latest/using-k6/thresholds/), [SignalR hub protocol](https://github.com/dotnet/aspnetcore/blob/main/src/SignalR/docs/specs/HubProtocol.md), and [NBomber overview](https://nbomber.com/docs/getting-started/overview/): alternatives for driving and evaluating load.
