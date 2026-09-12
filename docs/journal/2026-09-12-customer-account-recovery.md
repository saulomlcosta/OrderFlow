# 2026-09-12 - Customer Account Recovery

## Context

The storefront could create and complete an owned checkout, but its active
state lived only in the Angular component. A refresh, navigation, or completed
login redirect could make a valid reservation invisible to its owner. The API
already enforced ownership for a known Checkout or Order identifier, but it did
not offer a self-service way to discover a customer's own resources.

## Decision

Added authenticated `GET /me/checkouts` and `GET /me/orders` endpoints. They
derive the customer subject exclusively from the JWT `sub` claim and filter on
the persisted `CustomerSubject`; the browser never supplies a customer ID.

Added Angular `/account`, protected by the existing authentication pattern. It
loads the signed-in customer's checkouts, orders, and the public catalog needed
to label checkout items. An active checkout can be completed or cancelled from
that page, so recovery uses the same server-authorized operations as the
storefront.

An operationally expired Active checkout is visible as `Awaiting release`, but
has no customer action. This preserves the current lifecycle decision: time
does not release stock, and an administrator must explicitly persist
expiration.

## Evidence

- Integration tests distinguish anonymous, owner, other-customer, and
  administrator results for both `/me` queries.
- Angular behavioral tests render owned history, complete a recovered active
  checkout, and hide actions for an expired reservation.
- Playwright covers the recovery journey: a customer starts checkout, navigates
  to `My purchases`, and completes it from the account page.
- The browser journey also verifies that the local Angular proxy forwards the
  `/me` route to the API rather than returning the SPA HTML fallback.

## Trade-offs

The initial account page has no pagination, search, or status filter because
there is no demonstrated history volume yet. It makes three sequential requests
to keep the isolated SQLite browser tests free of parallel connection locking;
this is intentionally simple rather than a generalized client-side data layer.
The API applies the final display ordering after provider-neutral database
filtering because SQLite cannot translate `DateTimeOffset` ordering; pagination
is the point at which ordering must move back into a provider-specific query.

Checkout items preserve quantity but not a product-name snapshot. The account
page therefore resolves their labels from the public catalog, while completed
orders display their durable commercial snapshots. If products become mutable
or unavailable, that distinction may justify storing a checkout display
snapshot later.

## Next Step

Observe whether self-service recovery reveals a concrete need for automatic
expiration, richer history navigation, or checkout display snapshots before
introducing more lifecycle or frontend infrastructure.
