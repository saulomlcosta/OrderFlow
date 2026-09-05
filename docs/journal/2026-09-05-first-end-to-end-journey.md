# 2026-09-05 - First End-to-End Journey

## Context

Unit, integration, and Angular component tests protected their respective
boundaries, but no automated test proved that a user could complete the main
business journey through the assembled system.

## Decision

Add Playwright and cover one customer purchase journey in Chromium:

1. open the Angular laboratory
2. create a product
3. add stock
4. start checkout and observe reserved inventory
5. complete checkout
6. observe the historical order and total

The test uses accessible roles and user-visible text instead of Angular state or
DOM implementation details. Playwright starts both ASP.NET Core and Angular,
waits for their readiness, and shuts them down after the run.

## Scope

Only Chromium is used because cross-browser behavior is not yet a demonstrated
problem. The administrative expiration journey is also excluded because it
would require waiting beyond the 15-minute reservation or introducing test-only
time control into the running application.

Detailed lifecycle combinations remain in unit and integration tests. The E2E
test answers one broader question: can a user complete the primary purchase
flow through the real system?

## CI

The workflow adds an `End-to-end` job after the faster `Backend` and `Frontend`
jobs. The runner installs Chromium and starts the two application processes. On
failure, the workflow retains the Playwright HTML report, screenshot, and trace
for seven days.

## Verification

- one Chromium journey passed locally
- browser interaction completed in approximately three seconds
- complete local Playwright execution completed in approximately seventeen seconds

## Next Investigation

Observe the remote Linux execution. The next product-infrastructure step is a
reproducible persistent environment with Docker and PostgreSQL; dependency
security warnings remain a separate maintenance investigation.
