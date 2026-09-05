# 2026-09-05 - Integration Test Isolation

## Context

All integration-test classes shared one xUnit collection fixture, one API
factory, one SQLite in-memory database, and one mutable clock. The collection
prevented parallel execution, but records remained in the database between test
cases. Existing tests passed because they mostly used generated identifiers and
local assertions, not because their data was isolated.

## Experiment

The existing suite passed repeatedly and when test classes were executed in
isolation. This demonstrated deterministic outcomes for the current scenarios,
but did not prove a clean starting state.

An explicit isolation theory then ran the same contract twice. Each case
expected an empty database and created one product. With the original factory,
one case passed and the other failed because it observed the product left by
the preceding case.

## Decision

Keep one shared `WebApplicationFactory` to avoid rebuilding the complete test
host for every case. Introduce an `IntegrationTestBase` that resets shared state
through xUnit's `IAsyncLifetime` before each test instance.

The reset performs two operations:

- restore the mutable clock to its initial instant
- recreate the SQLite in-memory database by closing and reopening its keeper connection

SQLite connection pooling is disabled for the test database so no pooled
connection accidentally keeps the previous in-memory database alive.

## Trade-offs

Recreating the small SQLite schema for every test costs more than retaining all
data, but provides deterministic isolation with negligible cost at the current
suite size. The approach is specific to the current in-memory SQLite test
infrastructure. A future PostgreSQL test environment may instead use database
reset tooling, schemas, or isolated containers.

The integration-test collection remains sequential because the database and
mutable clock are still shared resources. Per-test cleanup provides isolation
between cases, but it does not make concurrent test execution safe.

## Verification

- isolation contract failed before the change with one leaked product
- isolation contract passed after the change
- 15 integration tests passed in three consecutive full-suite runs
- checkout concurrency and time-dependent scenarios remained stable

## Next Investigation

Complete the checkout lifecycle transition matrix and inventory invariants,
then introduce one end-to-end browser journey across Angular, API, domain, and
persistence.
