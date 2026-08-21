# 2026-08-21 - Angular Laboratory UI

## Context

The backend flow is now centered on checkout-based stock reservation.
At this point, continuing only through HTTP files would still work, but it would
slow down learning once we want to observe the flow more frequently and from a
more product-like perspective.

## Decision

Add a small Angular 18 frontend in `src/OrderFlow.Web`.

This frontend is not intended to introduce a full client architecture yet.
It exists to:

- exercise the backend flow visually
- make inventory reservation behavior easier to observe
- create an initial surface for future product discussions
- begin frontend learning with Angular and TypeScript without over-designing

## What Was Added

- Angular 18 standalone application
- TypeScript API service for products, checkouts, and orders
- Guided UI for:
  - product creation
  - stock addition
  - checkout start
  - checkout cancellation
  - checkout completion
  - order lookup
- Local proxy configuration so the frontend can call the backend directly during development

## Why This Matters

This keeps the project aligned with the laboratory idea.

We are not adding frontend complexity because the system is "finished".
We are adding a thin frontend now because the current backend flow is already
coherent enough to benefit from repeated manual exploration.

That makes the next backend decisions easier to reason about, especially:

- reservation lifetime
- manual cleanup of expired reservations
- future admin flows
- transition from laboratory UI to actual product UI

## What We Deliberately Did Not Add

- global state library
- authentication
- route-heavy UI structure
- design system
- advanced form abstraction
- frontend tests beyond basic smoke coverage

## Verification

- `npm run build`
- `npm test -- --watch=false --browsers=ChromeHeadless`
