# Current State

## Version

V1 - Initial implementation

## Current Goal

Validate the simplest end-to-end order flow before adding architectural complexity.

## Current Architecture

Single ASP.NET Core Minimal API application with one .NET project for the main app.
Persistence still uses one shared EF Core DbContext, but entity mappings now live
close to their modules so logical boundaries remain visible inside the monolith.

## Current Capabilities

- Create a product
- Add stock to a product
- Get a product with its current stock quantity
- Create an order
- Validate stock before finalizing an order
- Decrease stock when an order is created
- Preserve product name and price in historical order items
- Get an order

## Current Domain Concepts

- Product
- Inventory
- Order
- OrderItem

## Current Investigation

Understand the basic order creation flow, persistence, and domain behavior
before introducing new abstractions or distributed concerns.

The active next investigation is `EXP-001 - Concurrent Orders Against the Same Stock`.

## Recent Adjustment

Kept one DbContext for transactional simplicity, but moved EF Core mapping details
closer to Products, Inventory, and Orders to better reflect module boundaries
without introducing extra projects or extra DbContexts.

## Known Limitations

- No authentication
- No authorization
- No product update endpoint
- No messaging
- No caching
- No observability
- No containerization
- No distributed architecture
- No frontend
- No concurrency strategy
- No stock reservation
- No payment processing
- No checkout workflow
- No cart

## Next Step

Run `EXP-001` to learn whether the current order flow remains correct when
multiple customers try to buy the same stock concurrently.
