# 2026-09-10 - First Controlled Load-Test Baseline

## Context

PostgreSQL concurrency tests already protected atomic stock behavior under
specific collisions. The next question was different: what latency, throughput,
and error behavior does the complete checkout journey exhibit under a small
sustained local workload?

Performance targets and architecture changes would be speculation without an
initial observation.

## Decision

Introduce a manual k6 `v2.2.0` harness with a pinned Docker image. Run the API in
Release against an ephemeral PostgreSQL database, and keep the entire environment
separate from Development.

The default profile shares 100 independent journeys among 10 virtual users. A
journey creates a product, adds one unit, reserves it through Start Checkout,
completes the checkout, and verifies the final Product and Order.

Only correctness thresholds exist:

- zero failed HTTP requests
- 100 percent successful checks
- 100 percent successful journeys

Latency is measured but is not yet a pass/fail threshold or SLO.

## Verification

A 5-journey smoke first proved image download, networking, migrations, API
readiness, payloads, checks, and cleanup. The official baseline was then repeated
with the API in Release configuration.

Environment:

- Windows 11 Pro with AMD Ryzen 5 5600G
- 6 physical cores, 12 logical processors, and 31.9 GiB host memory
- Docker allocated 12 CPUs and 15.6 GiB memory
- .NET SDK `10.0.302`, Docker Engine `28.5.1`
- PostgreSQL `18.6-alpine`, k6 `v2.2.0`

Result:

- 100 of 100 complete journeys succeeded
- 1,101 of 1,101 checks succeeded
- 601 HTTP requests completed with zero failures
- throughput was 65.45 journeys/s and 393.36 requests/s
- aggregate HTTP median was 8.55 ms and p95 was 58.74 ms
- journey median was 57 ms, p90 was 157.6 ms, and p95 was 937 ms
- Start Checkout median was 11.33 ms and p95 was 237.69 ms
- Complete Checkout median was 15.67 ms and p95 was 157.2 ms

The runner removed the API, database container, and Docker network afterward.
The persistent Development database remained untouched.

## Interpretation

The system preserved every tested business invariant at this load. That is a
correctness result, not proof of production capacity.

The run completed in roughly 1.5 seconds, and journey p95 was much larger than
its median. A few early iterations likely dominate the tail, but the current
evidence cannot separate runtime warm-up, initial database work, scheduling, or
another cause. Repeated runs and a longer steady-state window are required before
diagnosis.

## Trade-offs

Unique product rows avoid turning the baseline into a hot-row contention test.
This measures ordinary journey capacity but does not model customers competing
for one popular product.

There is no think time, arrival-rate model, server resource telemetry, network
latency, production topology, or repeated-run variance. The result is local and
must not become a production expectation or CI threshold.

## Next Investigation

Resume the product roadmap with authentication discovery. Performance testing
can return as a repeated steady-state experiment when a concrete capacity
question or regression requires it.
