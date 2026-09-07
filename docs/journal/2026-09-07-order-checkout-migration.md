# 2026-09-07 - Order to Checkout Migration

## Context

Checkout completion created an Order and returned both identifiers, but the
Order itself did not persist which Checkout originated it. The API could not
reconstruct that relationship later, and the database could not enforce that a
Checkout produced at most one Order.

This was selected as the second migration because it solves a real traceability
gap while providing a small, controlled schema-evolution exercise on a
PostgreSQL volume that already contains data.

## Decision

Add `CheckoutId` to Order and require a non-empty checkout identifier whenever a
new Order is created. Persist the relationship with:

- a foreign key from `Orders.CheckoutId` to `Checkouts.Id`
- `RESTRICT` delete behavior so a completed purchase cannot silently lose its
  origin
- a filtered unique index that permits at most one Order for each non-null
  Checkout

The database column is nullable even though the current domain creation method
requires it. This is an expand-compatible decision: orders created before the
relationship existed remain valid with `NULL`, while all new application writes
produce linked orders.

## Migration Exercise

Before applying the migration, the Development database contained one Order and
only the initial migration. Applying `LinkOrdersToCheckouts` added the nullable
column, unique filtered index, and foreign key. The existing Order remained
present with a null `CheckoutId`, proving that the forward migration preserved
legacy data.

The `Down` migration was then exercised only against the isolated
`orderflow_tests` database. It removed the foreign key, index, and column and
returned the migration history to the initial migration. Applying the latest
migration again restored the current schema successfully.

## Verification

- 29 unit tests passed
- 21 integration tests passed with PostgreSQL concurrency enabled
- the concurrent completion winner persisted the correct Checkout identifier
- Angular behavioral tests passed after extending the Order response contract
- the Development database retained its existing legacy Order
- rollback and forward reapplication succeeded on the isolated test database

## Trade-offs

The nullable column means the database alone cannot require a Checkout for every
Order while legacy rows exist. The domain prevents new null relationships, and
the API exposes `checkoutId` as nullable to represent history honestly.

A future migration could backfill or archive legacy rows and make the column
non-nullable. Doing that now would require inventing a checkout for historical
data or deleting it, neither of which is justified.

## Next Investigation

Introduce liveness and readiness health checks so local orchestration and future
deployment infrastructure can distinguish a running process from an application
that is able to reach PostgreSQL and serve traffic.
