# Partial Failure and Cleanup

## What we investigated

Today we focused on a different question from concurrency:

How can we verify that order creation stays consistent when a failure happens
after stock handling has started but before the transaction is committed?

## Why temporary instrumentation was used

To make this failure reproducible, we introduced a temporary failure injection
hook in the order creation flow used only by integration tests.

The purpose of this hook was not to become part of the design.

Its only purpose was to force a controlled failure after stock decrement and
before commit so we could verify whether the transaction really protected the
operation.

## What the experiment showed

The experiment confirmed the current transaction behavior:

- the HTTP request failed
- no order was created
- stock remained unchanged

This means the operation rolled back correctly instead of leaving partial state.

## What I want to preserve from this cycle

- reproduce the failure intentionally
- observe the persisted outcome
- confirm the consistency guarantee using business facts
- remove the temporary test instrumentation after learning from it

## Why cleanup matters

The hook was useful for the investigation, but it does not belong to the
business flow long term.

The repository should preserve the learning in commits and docs, while keeping
the production code focused on the actual behavior of the system.

After validating the rollback behavior, the temporary hook and the test that
depended on it were removed from the main codebase.

## Reservation follow-up

Later on Friday, August 21, 2026, the project took its first step into stock
reservation by introducing a `Start Checkout` flow.

The initial rule is intentionally narrow:

- reservation protects quantity only
- price is still resolved when the final order is created

This keeps the first implementation smaller and lets availability concerns
evolve before commercial guarantees are introduced.

After that, the direct `Create Order` purchase entrypoint was removed so the
main purchase behavior would be centered on checkout instead of keeping two
different ways to buy the same product.

## Local developer flow

To make manual exploration easier, the Development environment now runs with
SQLite in-memory and the `.http` file was updated to follow the real product
flow:

- create product
- add stock
- start checkout
- cancel or complete checkout
- inspect final order

This keeps local testing lightweight while still letting the product be used
manually before a frontend exists.
