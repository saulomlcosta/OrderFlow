# 2026-09-10 - First Authentication Boundary

## Context

The Angular application had visually separate customer and administration
areas, but the API treated every caller equally. The project needed a real trust
boundary before turning the laboratory UI into a customer experience.

## Learning

Authentication and authorization are different responsibilities. Keycloak
proves who the caller is and emits claims; OrderFlow still decides whether those
claims permit a business operation. Angular guards shape navigation, but they
cannot protect an endpoint from direct HTTP calls.

The external provider also makes token validation explicit. The API trusts only
tokens with the expected issuer, audience, signature, and lifetime. The
`administrator` role answers a capability question, while future `sub` ownership
will answer whether a customer may access one particular Checkout or Order.

## Result

Keycloak and its PostgreSQL store now run in Docker, with a versioned realm for
repeatable Development setup. Angular uses Authorization Code with PKCE and
keeps tokens in memory. The API protects checkout listing and manual expiration
through an administrator policy, with integration evidence for `401`, `403`,
and successful administrative access.

## Next Step

Separate storefront behavior from product administration, then persist customer
ownership before requiring identity on checkout and order operations.
