# 2026-09-04 - Angular Behavioral Tests

## Context

The Angular application had tests for its shell and HTTP service contracts, but
the two user-facing features were not protected by behavioral component tests.
Regressions in loading, selection, lifecycle actions, and rendered feedback
could therefore pass unnoticed even when the API service remained correct.

## Decision

Test each feature through observable user behavior. Replace the
`OrderflowApiService` with Jasmine spies so every scenario controls its API
responses without starting the backend or database.

The tests interact with buttons and inspect the rendered DOM instead of reading
or changing the components' protected signals directly. This keeps the tests
focused on outcomes and allows internal state implementation to evolve.

## Scenarios Added

The checkout operations feature now verifies:

- loading the overdue queue by default
- presenting an empty queue
- selecting a reservation and showing product and inventory details
- expiring a reservation and refreshing both queue and inventory state

The purchase laboratory now verifies:

- creating a product and presenting its initial inventory
- starting checkout and showing its reservation impact
- completing checkout and rendering the historical order
- presenting backend validation errors

## Learning

`TestBed` creates standalone components in an Angular testing environment.
Jasmine spies define controlled service behavior, RxJS `of` and `throwError`
represent successful and failed asynchronous responses, and `whenStable`
waits for component promises before the DOM is inspected again.

The tests assert rendered text, input values, available actions, and service
interactions. They do not assert signal values directly. During implementation,
this distinction exposed that an input's current value belongs to its `value`
property rather than the element's `textContent`.

## Verification

- 12 Angular tests passing
- 8 behavioral component tests added
- no backend process required by the component test suite

## Next Investigation

Evaluate isolation in backend integration tests, then add one minimal browser
journey that exercises Angular, API, domain behavior, and persistence together.
