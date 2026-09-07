# Current State

## Version

V1 - Initial implementation

## Current Goal

Validate the simplest end-to-end order flow before adding architectural complexity.

The current business and architecture map is maintained in
[`reports/SYSTEM_FLOW.md`](reports/SYSTEM_FLOW.md).

## Current Architecture

Single ASP.NET Core Minimal API application with one .NET project for the main app.
Persistence still uses one shared EF Core DbContext, but entity mappings now live
close to their modules so logical boundaries remain visible inside the monolith.

For local development runs, the API now uses PostgreSQL 18 in Docker with a
named volume. EF Core applies pending migrations at Development startup, so data
survives API and container restarts. SQLite in-memory remains an explicit test
provider for fast and isolated integration and browser tests.

The repository now also contains a small Angular 18 frontend written in
TypeScript. It is intentionally a thin laboratory UI that exercises the current
backend flow rather than a full product experience.

## Current Capabilities

- Create a product
- Add stock to a product
- Get a product with its current stock quantity
- Start a checkout and reserve quantity
- Cancel a checkout and release reserved quantity
- Get a checkout
- List checkouts by lifecycle filter
- Expire an overdue checkout and release reserved quantity
- Complete a checkout into a final order
- Trace a final order back to its originating checkout
- Preserve product name and price in historical order items
- Get an order
- Exercise the full flow through a local Angular UI
- Operate checkout reservations through `/admin/checkouts`
- Inspect active, overdue, completed, and cancelled checkout queues
- Manually expire overdue reservations and confirm released inventory
- Validate backend, frontend, and PostgreSQL startup through GitHub Actions CI
- Validate the complete purchase journey through a real browser
- Validate critical reservation and completion concurrency against PostgreSQL

## Current Domain Concepts

- Product
- Inventory
- Checkout
- CheckoutItem
- Order
- OrderItem

## Current Investigation

Understand the basic order creation flow, persistence, and domain behavior
before introducing new abstractions or distributed concerns.

`EXP-003 - Stock Reservation Planning` established the first complete reservation
lifecycle. The active investigation now moves to test strategy and isolation as
the number of business states and user-facing flows grows.

`EXP-004 - Automated Test Strategy` now covers behavioral component tests for
both Angular features, isolated backend integration tests, an executable domain
transition matrix, and one minimal end-to-end browser journey.

Integration tests now share the application factory for speed while resetting
the SQLite in-memory database and mutable clock before every test case. An
explicit isolation contract proves that data created by one test is not visible
to the next.

The checkout lifecycle transition matrix is now executable at the domain level.
Integration tests additionally protect the highest-risk effects: a premature
expiration preserves reservations, repeated terminal operations do not release
another checkout's stock, and repeated completion neither consumes inventory
nor creates a second order.

The repository now has a minimal continuous integration quality gate. Pushes
and pull requests targeting `master` build and test backend and frontend in
independent clean Linux jobs. Continuous deployment remains intentionally out
of scope until hosting, database, migration, secret, and rollback decisions
exist. The first remote execution completed successfully for both jobs.

The backend job now also starts PostgreSQL, applies the current migration through
API startup, and writes one product as a persistence smoke test.

Playwright now protects one end-to-end customer journey across Angular, the
development proxy, ASP.NET Core, domain behavior, and isolated SQLite persistence. The
test creates a product, adds stock, reserves quantity, completes checkout, and
observes the resulting order. It runs only after the faster backend and frontend
CI jobs pass. The first remote Linux execution passed all three jobs: backend,
frontend, and end-to-end.

Two opt-in PostgreSQL integration tests now exercise the highest-risk concurrent
writes against the real provider. Twenty simultaneous checkout requests against
five units produce exactly five reservations, while ten simultaneous completion
requests against one checkout produce exactly one order linked to that checkout.
The default local test run skips these two tests unless
`ORDERFLOW_RUN_POSTGRESQL_TESTS=true` is set.

The second EF Core migration links each new Order to the Checkout that originated
it. The database foreign key prevents invalid references, and a filtered unique
index enforces at most one order per checkout. The column remains nullable only
for compatibility with orders created before this relationship existed; the
domain requires the identifier for every new order.

The migration was applied to the existing Development volume without losing its
legacy order. Its `Down` path was exercised against the isolated test database,
then the migration was applied again successfully.

## Recent Adjustment

Kept one DbContext for transactional simplicity, but moved EF Core mapping details
closer to Products, Inventory, and Orders to better reflect module boundaries
without introducing extra projects or extra DbContexts.

The local HTTP workflow now matches the checkout-based purchase flow and can be
exercised directly from `OrderFlow.Api.http`.

The frontend uses a local dev proxy so `npm start` can call the API directly
while the backend runs on `http://localhost:5216`.

The Angular application now has two explicit user experiences:

- `/` for the purchase laboratory
- `/admin/checkouts` for reservation operations

Checkout reservations now have an explicit lifecycle boundary:

- `Active` reservations can be cancelled or completed before `ExpiresAt`
- overdue reservations can no longer be completed
- overdue reservations are listed operationally as expired
- stock is returned only when an explicit expiration action persists `Expired`

## Known Limitations

- No authentication
- No authorization
- No product update endpoint
- No messaging
- No caching
- No observability
- No explicit application or dependency health endpoints
- API and frontend are not containerized
- No distributed architecture
- No authentication-aware frontend flows
- No broader concurrency strategy beyond protected reservation and stock confirmation flows
- No automatic reservation expiration worker
- Stock reservation protects quantity only, not price
- No payment processing
- No cart
- Frontend dependency audit reports transitive vulnerabilities that still require a deliberate upgrade plan

## Next Step

Add health checks that distinguish whether the application process is alive
from whether it is ready to serve requests with PostgreSQL available. Protect
their behavior with integration tests and expose a useful Docker health signal.
