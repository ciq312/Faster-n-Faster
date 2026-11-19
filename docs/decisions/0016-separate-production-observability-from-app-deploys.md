# 0016. Separate production observability from app deploys

- **Status:** Accepted, in progress
- **Date:** 2026-11-19
- **Sources:** [Local observability and OTLP](0015-local-observability-via-otlp.md), [SHA-pinned Deploy workflow](../../.github/workflows/deploy.yml), [Alloy pipeline documentation](https://grafana.com/docs/alloy/latest/collect/opentelemetry-to-lgtm-stack/).

## Context

Production uses a single VPS and deploys the application at a Git SHA. Operations needs release-correlated telemetry that survives application restarts and an availability notification that works when the VPS itself is unavailable.

## Decision

Apply separation of concerns to deployment and failure detection. Operate the telemetry backends in a separate Compose project on the VPS. Use the OTLP boundary to connect the application release to the collector. Put the public readiness probe and its notification delivery outside the VPS.

## How it works

The application exports over its private Compose network to Alloy. Alloy sends metrics, filtered logs and sampled traces to Prometheus, Loki and Tempo. Host exporters collect machine and container metrics on the monitoring network. Caddy writes redacted access events to a rotating named volume that Alloy reads. Grafana is exposed on loopback and reached by SSH tunnel.

The monitoring configuration and data live outside the application release checkout. A separate uptime service performs a single HTTPS check through Caddy to `/api/health`. It has its own Telegram delivery path.

## Alternatives considered

- The development LGTM bundle gives a fast local setup. Separate production components provide explicit retention, resource budgets and network boundaries.
- A hosted telemetry backend provides a separate failure domain for telemetry storage. Keeping telemetry on the VPS reduces external services to the uptime check.

## Consequences

Dashboards and stored telemetry survive application rollbacks. Telemetry history shares the VPS failure domain. Resource headroom, retention and the host filesystem used by Docker are part of deployment verification. The [production runbook](../production-observability.md) describes configuration, notifications and rollback.
