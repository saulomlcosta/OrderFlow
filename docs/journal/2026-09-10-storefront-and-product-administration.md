# 2026-09-10 - Storefront and Product Administration

## Context

The root Angular page still behaved like a technical laboratory: the same user
could create a product, add stock, and purchase it. Keycloak had established an
administrator boundary for checkout operations, exposing the next inconsistency:
catalog provisioning remained both publicly callable and visible to customers.

## Decision

Keep one Angular application, but separate experiences by responsibility. The
root route now lists a public catalog and conducts the checkout journey.
`/admin/products` owns product creation and physical-stock addition, while
`/admin/checkouts` continues to own reservation operations. The API protects
both product commands with the existing `administrator` policy.

Human administrators continue to use Authorization Code with PKCE. The HTTP
workflow and k6 cannot perform an interactive browser redirect, so the
Development realm now includes a confidential automation client that uses
Client Credentials and receives the administrator role. The k6 environment
runs its own ephemeral Keycloak and PostgreSQL stores.

## Evidence

- Integration tests prove public catalog access, anonymous `401`, customer
  `403`, and successful administrator commands.
- Angular behavioral tests cover catalog loading, product provisioning, stock
  addition, reservation, and completion.
- Playwright logs in through the real Keycloak UI, provisions inventory in the
  administrative area, and completes the purchase in the storefront.
- A short k6 verification completed 4 of 4 authenticated journeys and 46 of 46
  correctness checks against ephemeral infrastructure.

The browser test revealed that ASP.NET Core inbound claim mapping changed the
Keycloak `roles` claim before authorization evaluated it. Disabling inbound
mapping made the JWT contract explicit and restored the administrator policy.

## Trade-offs

One SPA is simpler to build and deploy, but it does not create a security
boundary by itself; API policies remain mandatory. The automation client makes
non-browser tests repeatable, but its committed secret is suitable only for the
local laboratory and must never become a production credential pattern.

## Next Step

Associate Checkout and Order with the authenticated Keycloak subject. This will
let customer endpoints require authentication and enforce resource ownership
without taking away the administrator's global operational view.
