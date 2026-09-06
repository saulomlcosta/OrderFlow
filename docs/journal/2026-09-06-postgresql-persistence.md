# 2026-09-06 - PostgreSQL Persistence

## Context

Development and automated tests both used SQLite in-memory. This made the
feedback loop fast, but every API restart erased the developer's data and no
execution validated the model against the database intended for durable use.

## Decision

Use PostgreSQL 18 in Docker for Development while retaining SQLite in-memory as
an explicit test substitution.

The local environment now includes:

- the official `postgres:18.6-alpine` image
- a named volume that survives container recreation
- a health check that reports database readiness
- Npgsql as the EF Core PostgreSQL provider
- an initial PostgreSQL migration for the existing model
- automatic migration application only when
  `Persistence:InitializeOnStartup` is enabled

Local credentials are committed for convenience and are not suitable for
production. Non-Development environments keep startup migration disabled by
default because schema deployment, permissions, rollback, and multiple-instance
coordination require a deliberate release strategy.

## Test Boundary

Unit tests do not use persistence. Integration and Playwright tests keep SQLite
in-memory because they need speed, deterministic cleanup, and no Docker
prerequisite. They select SQLite before EF Core registers provider services, so
each application host contains exactly one database provider.

The shared in-memory database keeps one anchor connection open, while concurrent
DbContexts create separate connections from the same connection string. Sharing
one physical connection caused the concurrent reservation test to fail and was
rejected.

## Verification

- PostgreSQL became healthy through Docker Compose
- the initial migration created the complete schema
- a product with stock was written through the HTTP API
- the API and PostgreSQL container were restarted
- the same product and stock were read after both restarts
- 27 unit tests passed
- 19 integration tests passed
- 12 Angular behavioral tests passed
- one Playwright purchase journey passed

The CI backend job now repeats a smaller PostgreSQL smoke test by starting the
API, applying migrations, and creating a product.

## Trade-offs

Keeping two providers creates a small risk that SQLite tests do not reproduce a
PostgreSQL-specific behavior. The fast suite remains valuable, but high-risk
locking and concurrency scenarios should next be executed against PostgreSQL.

Applying migrations at application startup is convenient for this single-instance
Development environment. It is not yet the production deployment strategy.
