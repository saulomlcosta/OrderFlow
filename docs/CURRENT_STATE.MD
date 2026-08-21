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
- Complete a checkout into a final order
- Preserve product name and price in historical order items
- Get an order
- Exercise the full flow through a local Angular UI

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

The active next investigation is `EXP-003 - Stock Reservation Planning`.

## Recent Adjustment

Kept one DbContext for transactional simplicity, but moved EF Core mapping details
closer to Products, Inventory, and Orders to better reflect module boundaries
without introducing extra projects or extra DbContexts.

The local HTTP workflow now matches the checkout-based purchase flow and can be
exercised directly from `OrderFlow.Api.http`.

The frontend uses a local dev proxy so `npm start` can call the API directly
while the backend runs on `http://localhost:5216`.

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
- No automatic reservation expiration
- Stock reservation protects quantity only, not price
- No payment processing
- No cart

## Next Step

Use the new UI as a learning surface while clarifying the next domain rules
around reservation lifetime, release, manual administration, and future payment
integration.
