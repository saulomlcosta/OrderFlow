# 2026-09-06 - PostgreSQL Concurrency

## Context

The atomic stock conditions were covered by SQLite integration tests, but the
application now uses PostgreSQL for Development. SQLite could not prove how the
same SQL and transactions behave with PostgreSQL row locking and concurrent
connections.

## Question

Do the current stock guards preserve the business invariants when requests truly
compete through the PostgreSQL provider?

## Experiment

Add two opt-in integration scenarios:

1. release twenty checkout requests together against five available units
2. release ten completion requests together against one active checkout

A `TaskCompletionSource` acts as a start gate so the HTTP requests reach the API
at nearly the same time. The test does not coordinate application internals or
database operations after release.

The tests use the same ASP.NET Core application and PostgreSQL migration as
Development. Tables are truncated before each scenario to preserve isolation.

## Result

The reservation scenario produced exactly five successful checkouts and fifteen
business validation failures. Physical stock remained five, all five units were
reserved, and available stock became zero.

The completion scenario produced exactly one successful order and nine business
validation failures. Physical and reserved stock became zero, and the database
contained exactly one order.

## Why It Works

Reservation uses one conditional database update. PostgreSQL locks the inventory
row while updating it; waiting transactions reevaluate the availability
condition after the previous transaction commits. Once five units are reserved,
the remaining updates affect zero rows and become business failures.

Completion uses the reserved quantity as a second atomic guard. The first
transaction consumes physical and reserved stock. Later updates affect zero rows,
so they cannot create additional orders.

## Test Strategy

These scenarios are slower and require Docker, so they run only when
`ORDERFLOW_RUN_POSTGRESQL_TESTS=true`. The CI enables them because its Backend job
already provisions PostgreSQL. The ordinary local suite remains fast and reports
the provider-specific scenarios as skipped.

This is not a load test. It verifies correctness for a controlled collision but
does not measure throughput, latency percentiles, connection-pool saturation, or
behavior under sustained traffic.

## Next Investigation

Create a small second migration and apply it to the PostgreSQL volume that
already contains data. This will move the laboratory from proving initial schema
creation to learning safe schema evolution.
