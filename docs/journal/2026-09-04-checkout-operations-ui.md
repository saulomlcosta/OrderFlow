# 2026-09-04 - Checkout Operations UI

## Context

The backend can identify overdue checkouts and explicitly expire them, but the
operation was available only through HTTP calls. The reservation lifecycle
needed a visible operating surface before considering background automation.

## Decision

Keep customer and administrative experiences in the same Angular application.

The application now uses routes to establish logical boundaries:

- `/` remains the guided purchase laboratory
- `/admin/checkouts` becomes the reservation operations area

A separate frontend is not justified yet because both experiences share the
same deployment lifecycle, API, and development team.

## What Was Added

- application shell with route navigation
- checkout queues for active, overdue, completed, and cancelled states
- checkout detail with reservation timeline and items
- current product availability for each reserved item
- explicit expiration action for overdue active checkouts
- refreshed inventory state after reservation release
- loading, empty, success, and failure states

## Angular Learning Surface

This step introduces:

- standalone routed components
- feature-oriented folders
- signals and computed state
- HTTP query parameters
- orchestration of related API calls
- lifecycle-specific UI actions
- separation between customer journey and backoffice operations

No global state library or separate admin application was introduced because
the current complexity does not require either one.

## Verification

- Angular production build
- four Angular tests covering the shell and administrative HTTP contracts
- visual verification of the overdue queue and reservation detail against the local API

The dependency installation also exposed transitive security alerts in the
Angular 18 toolchain. They were recorded for a deliberate dependency review;
no forced automatic upgrade was applied as part of this feature.
