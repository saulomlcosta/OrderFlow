# 2026-09-11 - Customer Resource Ownership

## Context

Authentication identified callers, but customer endpoints still authorized by
possession of a resource GUID. Anyone who learned another checkout or order ID
could read it, cancel its reservation, or complete its purchase. The system
needed object-level authorization before adding customer history.

## Decision

Every new Checkout captures the authenticated OpenID Connect `sub` as
`CustomerSubject`. Completion copies that value into the resulting Order so the
commercial record preserves ownership independently and can be queried without
joining through Checkout.

The storefront remains public, but starting checkout requires authentication.
Owners may read, cancel, and complete their own Checkouts and read their Orders.
Cross-customer access returns `404` so the API does not reveal whether a guessed
identifier exists. Administrators may inspect any Checkout or Order and expire
overdue reservations, but cannot complete a purchase on behalf of its owner.

## Migration Strategy

The indexed columns are nullable in PostgreSQL solely for backward compatibility.
Existing rows remain ownerless instead of receiving fabricated identities. New
domain factories reject missing subjects, so nullability at rest does not weaken
the invariant for new purchases. Ownerless legacy resources remain available
only through administrator reads and expiration operations.

The migration was applied, rolled back, and reapplied against the isolated
`orderflow_tests` database. It was then applied additively to the Development
volume without deleting existing data.

## Evidence

- Domain tests require ownership on new Checkout and Order instances.
- Integration tests distinguish anonymous, owner, other customer, and
  administrator behavior and verify the persisted subject.
- All PostgreSQL concurrency tests continue to pass with authenticated buyers.
- Angular redirects an anonymous visitor to login only when checkout begins.
- Playwright provisions stock as administrator, signs out, signs in as customer,
  and completes an owned purchase.
- A short k6 run completed 4 of 4 journeys and 46 of 46 checks using an owned
  machine identity.

## Trade-offs

Copying the subject onto Order duplicates a value already reachable through
Checkout, but it makes historical ownership explicit and supports direct
customer order queries. The subject is an opaque provider identifier, not a
username or email. Recreating an identity realm changes subjects and can orphan
business ownership, so a production realm and its identity data must be treated
as durable infrastructure.

## Next Step

Expose customer-scoped Checkout and Order history and build an Angular account
area. This will exercise the new ownership indexes and make the authenticated
experience useful beyond a single browser session.
