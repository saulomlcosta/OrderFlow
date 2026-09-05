# Current State

## Version

V1 - Initial implementation

## Current Goal

Validate the simplest end-to-end order flow before adding architectural complexity.

## Current Architecture

Single ASP.NET Core Minimal API application with one .NET project for the main app.
Persistence still uses one shared EF Core DbContext, but entity mappings now live
close to their modules so logical boundaries remain visible inside the monolith.

For local development runs, the API now uses SQLite in-memory so the application
starts with a clean transient dataset each time `dotnet run` is executed in the
Development environment.

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
- Preserve product name and price in historical order items
- Get an order
- Exercise the full flow through a local Angular UI
- Operate checkout reservations through `/admin/checkouts`
- Inspect active, overdue, completed, and cancelled checkout queues
- Manually expire overdue reservations and confirm released inventory

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

`EXP-004 - Automated Test Strategy` has started with behavioral component tests
for both Angular features. The frontend now verifies user-visible behavior with
controlled API collaborators, while the next test investigation will focus on
backend integration-test isolation and a minimal end-to-end journey.

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
- No containerization
- No distributed architecture
- No authentication-aware frontend flows
- No broader concurrency strategy beyond protected reservation and stock confirmation flows
- No automatic reservation expiration worker
- Stock reservation protects quantity only, not price
- No payment processing
- No cart
- Frontend dependency audit reports transitive vulnerabilities that still require a deliberate upgrade plan

## Next Step

Strengthen the automated test strategy around checkout lifecycle behavior,
frontend orchestration, and test data isolation before introducing
observability or authorization.
