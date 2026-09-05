# 2026-08-26 - Reservation Lifecycle

## Context

The project already supported:

- starting a checkout
- reserving stock
- cancelling a checkout
- completing a checkout into an order

That was enough to prove why reservation exists, but not enough to describe the
full lifecycle of a reservation once time becomes part of the model.

## Decision

Clarify overdue reservation behavior without introducing a background worker yet.

The model now distinguishes:

- `Cancelled`: the checkout was actively cancelled before expiry
- `Expired`: the reservation became overdue and was explicitly released

## Rules

- `Active` checkouts can be completed or cancelled only before `ExpiresAt`
- once `UtcNow > ExpiresAt`, completion is rejected as a business rule
- once `UtcNow > ExpiresAt`, cancellation is also rejected
- overdue stock is returned only by an explicit expiration action
- expiration persists the checkout as `Expired`

## Operational consequence

The API now exposes:

- `GET /checkouts/{id}`
- `GET /checkouts?status=active`
- `GET /checkouts?status=expired`
- `POST /checkouts/{id}/expire`

The `expired` filter is intentionally operational:

- persisted `Expired` checkouts are included
- overdue `Active` checkouts are also included so an administrator can see what
  still needs to be processed

## Why this matters

This keeps the design honest.

Time-based invalidation and stock release are related, but they are not the same
thing. A reservation can become overdue by rule before the system performs the
state transition and stock release that finalize its lifecycle.

That distinction prepares the project for a future worker without forcing one
into the design too early.

## Verification

- unit tests cover active, expired, and transition rules
- integration tests cover:
  - overdue completion rejection
  - overdue cancellation rejection
  - explicit expiration releasing stock
  - expired listing behavior
