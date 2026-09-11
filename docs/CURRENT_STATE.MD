# Current State

## Version

V1 - Initial implementation

## Current Goal

Use the stable end-to-end checkout flow to investigate operational, performance,
and security concerns without adding architectural complexity prematurely.

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

The repository now also contains an Angular 18 frontend written in TypeScript.
One SPA exposes a customer storefront and separate authenticated administration
workspaces without creating independent frontend deployments.

## Current Capabilities

- Create a product
- List the public product catalog with current availability
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
- Report process liveness and database-backed readiness through health endpoints
- Execute an isolated, correctness-aware k6 baseline of the complete checkout journey
- Authenticate Angular users through Keycloak with Authorization Code and PKCE
- Validate Keycloak JWT access tokens in the API
- Restrict checkout listing and manual expiration to administrators
- Restrict product creation and stock addition to administrators
- Provision products and stock through `/admin/products`
- Browse the catalog and complete purchases through the customer-oriented `/`
- Authenticate non-browser local automation through Client Credentials

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

Operational health is now explicit. `/health/live` verifies only that the API
process can answer requests, while `/health/ready` also checks database
connectivity. Readiness performs no writes, schema changes, or recovery actions.
The CI uses readiness to wait for the real PostgreSQL-backed API before its smoke
write, replacing the previous indirect OpenAPI check.

`EXP-005 - Controlled Load-Test Baseline` now provides a reproducible local k6
harness. It uses an ephemeral PostgreSQL database and temporary Release API, so
load data never touches the persistent Development volume. The first run
completed 100 of 100 journeys and 1,101 of 1,101 checks without HTTP failures.
It observed about 65.45 journeys/s, 393.36 requests/s, 58.74 ms aggregate HTTP
p95, and 937 ms complete-journey p95 on the documented workstation.

These values are a single local observation, not an SLO. The short 1.5-second
run showed a wide gap between median and p95 journey latency, so warm-up and
run-to-run variance must be understood before optimization or CI thresholds.

`EXP-006 - Keycloak Authentication Boundary` selected an external OpenID
Connect provider over an application-owned credential store. Keycloak runs with
its own PostgreSQL database, imports a versioned Development realm, and issues
tokens for the Angular public client with the OrderFlow API as audience.

Angular keeps tokens in memory, renews them before API calls, and guards the
administrative route. The API remains the security authority: anonymous callers
receive `401` from checkout administration, and authenticated non-administrators
receive `403`. The remaining customer endpoints are intentionally anonymous
until Checkout and Order can be associated with the authenticated subject.

The storefront now starts from public `GET /products` instead of exposing data
provisioning controls. Product creation and stock addition moved to
`/admin/products` and require the same server-side administrator policy as
checkout operations. The E2E test enters through the real Keycloak login before
provisioning inventory and then completes the purchase in the storefront.

The authenticated E2E exposed JWT claim remapping between Keycloak and ASP.NET
Core. Disabling inbound claim mapping preserves the emitted `roles` claim, so
the authorization policy and token contract now use the same name explicitly.

The k6 harness now starts an additional ephemeral Keycloak environment and uses
the `orderflow-automation` service account. This keeps human PKCE authentication
separate from machine-to-machine Client Credentials while preserving complete
test isolation.

## Recent Adjustment

Kept one DbContext for transactional simplicity, but moved EF Core mapping details
closer to Products, Inventory, and Orders to better reflect module boundaries
without introducing extra projects or extra DbContexts.

The local HTTP workflow now matches the checkout-based purchase flow and can be
exercised directly from `OrderFlow.Api.http`.

The frontend uses a local dev proxy so `npm start` can call the API directly
while the backend runs on `http://localhost:5216`.

The Angular application now has three explicit user experiences:

- `/` for the public storefront and purchase journey
- `/admin/products` for catalog and inventory operations
- `/admin/checkouts` for reservation operations

Checkout reservations now have an explicit lifecycle boundary:

- `Active` reservations can be cancelled or completed before `ExpiresAt`
- overdue reservations can no longer be completed
- overdue reservations are listed operationally as expired
- stock is returned only when an explicit expiration action persists `Expired`

## Known Limitations

- No customer resource ownership
- No product update endpoint
- No messaging
- No caching
- No metrics, tracing, or structured observability backend
- API and frontend are not containerized
- No distributed architecture
- No authenticated customer history or account area
- No broader concurrency strategy beyond protected reservation and stock confirmation flows
- No repeated steady-state load profile or production service-level objectives
- No automatic reservation expiration worker
- Stock reservation protects quantity only, not price
- No payment processing
- No cart
- Frontend dependency audit reports transitive vulnerabilities that still require a deliberate upgrade plan

## Next Step

Define and persist customer ownership by associating Checkout and Order with the
authenticated Keycloak `sub`. Then protect customer operations without losing
the separate administrator-wide operational view.
