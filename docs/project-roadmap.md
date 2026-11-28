# Project delivery roadmap

Start with telemetry and a browser smoke test in parallel. Instrument the runner before repeating the expensive load test. The owner can draft the three designs during that work. Ship lobby chat before private chat to learn from real delivery and reconnect behavior. Introduce the queue with a usable passage-generation flow. Compare replicas once the baseline and state-ownership design are available.

The local observability overlay already provides Prometheus, Grafana, Loki and Tempo. The runner currently authenticates and connects in batches of 100, with ten concurrent operations and a random delay, then joins lobbies sequentially. Wall-clock setup time therefore mixes client pacing and server response time. The reported latency at 5,000 players motivates a controlled rerun. A missing queue series leaves starvation unresolved.

## 1. Verify telemetry for a complete race and the load generator

Run a small race on the existing observability stack. In Grafana, correlate a slow interval with API logs and a representative trace. Verify per-instance ThreadPool queue depth, worker count and completed work under activity. Record units, sampling interval and how to distinguish zero from absent data.

## 2. Run one two-player race from browser login to persisted results

1. **Define the Docker environment.** Reuse the API image, Postgres, Redis and migration bundle. Give each E2E run its own Compose project, volumes, test credentials and available host ports. Replace the development stack's fixed container names and real environment-file reference in the E2E configuration.
2. **Build the current checkout and wait for readiness.** Start Postgres and Redis, run migrations against the test database and start the API. Poll `/api/health` with a timeout. Surface service logs immediately if migrations or startup fail.
3. **Prepare two players.** Create two verified accounts using the application's password hashing. Record their user IDs and starting `RacesTyped` and `Wins` values, treating a missing statistics row as zero.
4. **Install and configure Playwright.** Add `@playwright/test` to the frontend dev dependencies and install Chromium and its OS dependencies on the runner. Use a dedicated E2E test directory excluded from Vitest. Configure Playwright to start Vite on the test port, with its HTTP and WebSocket proxies targeting the E2E API. Check readiness through the frontend proxy.
5. **Exercise the lobby.** Create two isolated browser contexts and sign in through the UI in each. Alice creates a uniquely named lobby and Bob joins it. Wait until both screens show both players before Alice starts the race.
6. **Play the race.** Verify both players receive the same passage. Wait for typing to become enabled and send real keyboard events concurrently at realistic rates so server-side validation stays active. Assert that both result screens show the same two players in the same finishing order.
7. **Verify persistence.** Query Postgres by the fixture IDs with a bounded wait for the results write. Both players' `RacesTyped` must increase by one. Only the player shown first in the standings gains one win.
8. **Wrap the run in one command.** Expose `npm run test:e2e` from the frontend directory. Preserve the failing exit code, retain the Playwright trace and service logs on failure, and remove only this run's containers and volumes on exit. Verify two consecutive clean runs and one intentionally failing assertion before enabling the check.
9. **Use the same command in CI.** On a Docker-capable runner, install the locked frontend dependencies and Chromium, run the command and publish failure artifacts. Make the passing race a merge requirement for changes targeting `dev`.

## 3. Separate login, connection ramp and gameplay in the load runner

Expose bounded arrival rate and concurrency. Measure account setup, login, hub connection, lobby join and gameplay independently. Provide a cold-login scenario and a prepared-player race scenario. Report target and achieved rates, active players, failures and p50/p95/p99 latency per operation alongside generator CPU and memory.

## 4. Reproduce 5,000-player latency and evaluate ThreadPool starvation

After telemetry and runner changes, incorporate the existing starvation investigation. Repeat small, intermediate and 5,000-player runs with fixed resources and workload. Align latency with queue depth, workers, completed work, CPU, GC, database waits and generator saturation. Capture stacks during a spike. Report whether starvation is supported, contradicted or unresolved and which phase limits throughput.

## 5. Review the owner's lobby chat design

Owner drafts membership, delivery, history, reconnect and limits. Review against two members exchanging messages, an outsider attempting access and a member leaving during delivery. Record decisions and unresolved questions before implementation.

## 6. Deliver lobby chat across two browsers

After lobby chat design review, implement the agreed UI and server contract. Verify two lobby members exchange messages, a different lobby receives none, and leaving revokes access. Exercise agreed history and reconnect behavior.

## 7. Review the owner's friends and private chat design

Owner defines friendship transitions, who can initiate and read a conversation, history after removal, and offline delivery. Review duplicate and simultaneous requests, reconnects and unauthorized reads. Record agreed behavior before implementation.

## 8. Implement friendship requests and lifecycle

After friends design review, expose the agreed actions in the UI and persist relationship transitions. Verify independent sessions, duplicate requests, concurrent actions and authorization against the reviewed transition table.

## 9. Deliver private messaging between friends

After friendship lifecycle delivery, implement sending, receiving and history according to the reviewed design. Verify two friends can converse after reconnect, a third user cannot read or subscribe, and removal enforces the agreed access policy.

## 10. Review the owner's gateway, queue, database and cache architecture

Owner proposes service boundaries and ownership. Trace a browser request, a SignalR reconnect and a queued passage job through the proposed topology. Review routing, state ownership, failure recovery, deployment and migration. Record the accepted decision before topology changes.

## 11. Deploy the reviewed service topology through the gateway

After architecture review, deploy the approved services with gateway, queue, database and cache. Verify HTTP and hub traffic through the public entry point, broker publish/consume, database persistence and cache recovery. Run the browser smoke suite and document rollback.

## 12. Generate validated typing passages from queued topic requests

After topology delivery, let a player submit a topic and see pending, ready or failed status. Validate input length, content and per-player limits before enqueueing. Worker calls the model and independently checks output policy, language, length and typability before publication. Bound retries, deduplicate deliveries and preserve status on restart. In CI use a deterministic model substitute with the real browser, broker, worker and database. Verify a valid passage survives reload, rejected input creates no job, and invalid output never becomes playable.

## 13. Compare single-instance and multi-instance race capacity

After the baseline and architecture review, verify players on different replicas see consistent lobby and race state, including reconnects. Repeat the same workload with one and multiple API replicas, documenting total resources, routing, fan-out, p95/p99 latency, throughput and failures. Report the first limiting resource and the capacity gained per added replica.
