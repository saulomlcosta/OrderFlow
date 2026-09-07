# 2026-09-07 - Liveness and Readiness Health Checks

## Context

The CI previously waited for the OpenAPI document before running its PostgreSQL
smoke test. That proved the HTTP server had started, but it used documentation as
an operational signal and did not express whether the application could reach
the database required by its business endpoints.

Future container orchestration, deployment, and monitoring also need to
distinguish a running process from an application that should receive traffic.

## Decision

Expose two unauthenticated operational endpoints:

- `GET /health/live` checks only that the process can answer HTTP
- `GET /health/ready` checks that the configured database is reachable

The database check creates its own dependency-injection scope and calls EF
Core's `CanConnectAsync`. It does not apply migrations, modify data, or attempt
recovery. Health checks observe state; they do not repair it.

Registrations use a `ready` tag. The liveness endpoint deliberately executes no
dependency checks, while readiness executes checks carrying that tag. This
keeps the distinction extensible if another required dependency is introduced.

## Why Two Signals

If PostgreSQL becomes unavailable, restarting a healthy API process does not fix
the database. Liveness therefore remains successful and avoids a restart loop,
while readiness returns `503 Service Unavailable` so an orchestrator can stop
sending business traffic to that instance.

The endpoints answer different operational questions:

- liveness: should this process be restarted?
- readiness: should this instance receive requests now?

## Verification Strategy

Integration tests protect three behaviors:

1. a running application is live
2. an application with its database available is ready
3. an application configured with an unreachable database remains live but is
   not ready

The unavailable case runs in an isolated application factory with startup
migrations disabled and a deliberately invalid PostgreSQL port. It does not stop
or alter the developer's database.

The CI now waits on `/health/ready` before writing its smoke-test product. This
turns readiness from an unused endpoint into part of the executable delivery
path.

The final local run passed 29 unit tests and 24 integration tests with the two
PostgreSQL concurrency scenarios enabled. Against the persistent Development
database, both endpoints returned `200 Healthy`, and the readiness request
executed a real connectivity query.

## Trade-offs

`CanConnectAsync` proves connectivity, not that every table or migration required
by the application exists. Startup migration currently covers Development and
CI, while a future production release process must own schema compatibility.

No authentication protects these endpoints because probes need simple access
and the responses expose only aggregate health. Detailed exceptions remain in
application logs rather than the HTTP response.

## Next Investigation

Establish the first controlled load-test baseline. The experiment should measure
latency, throughput, and errors while continuing to assert stock and order
correctness under sustained checkout traffic.
