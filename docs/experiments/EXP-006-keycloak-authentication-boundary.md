# EXP-006 - Keycloak Authentication Boundary

## Problem

OrderFlow exposes customer and administrative operations through the same API
and Angular SPA. Before this experiment, every caller could list all checkouts
or release an overdue reservation. A hidden Angular link would not protect the
HTTP boundary.

## Alternatives

ASP.NET Core Identity would keep credential storage and login responsibilities
inside OrderFlow. It offers a smaller local topology, but couples the laboratory
to operating users, passwords, recovery, and sessions.

An external OpenID Connect provider separates identity from business behavior.
It adds infrastructure and availability concerns, but exposes transferable
concepts such as issuer, audience, claims, roles, Authorization Code, PKCE, and
JWT validation.

A custom JWT issuer was rejected because it would make OrderFlow responsible for
security-sensitive protocol and credential behavior without a demonstrated need.

## Decision

Use Keycloak `26.7.3` as the Development identity provider. Angular is a public
client and uses Authorization Code with PKCE `S256`; no client secret is placed
in browser code. Access and refresh tokens remain in memory. The API is a
separate audience and validates bearer tokens through ASP.NET Core middleware.

Start with one narrow authorization boundary:

- `GET /checkouts` requires the `administrator` realm role
- `POST /checkouts/{id}/expire` requires the `administrator` realm role
- the Angular `/admin/checkouts` route uses a matching navigation guard
- the API policy remains authoritative if browser code is bypassed

## Reproducibility

The Docker environment imports `infra/keycloak/orderflow-realm.json`. The file
defines the `orderflow-web` public client, `orderflow-api` audience, `customer`
and `administrator` roles, and local demonstration users. Keycloak stores its
state in a PostgreSQL database separate from OrderFlow business data.

The committed passwords and HTTP configuration are Development defaults. The
container uses `start-dev`; none of these choices represent production
hardening or a production deployment decision.

## Evidence

Integration tests replace external token validation with a deterministic test
authentication scheme. They prove the administrative endpoint returns:

- `401 Unauthorized` without an identity
- `403 Forbidden` for an authenticated customer
- `200 OK` for an administrator

The real Keycloak container imported the realm successfully and published an
OpenID Connect discovery document whose issuer is
`http://localhost:8080/realms/orderflow`.

Keycloak's token evaluation projected `orderflow-api` as the access-token
audience and `administrator` in the flat `roles` claim consumed by ASP.NET Core.
A browser smoke reached the OrderFlow login page through Authorization Code and
included a PKCE `S256` code challenge before any credentials were submitted.

## Deliberate Boundary

Customer checkout and order operations remain anonymous in this increment.
Protecting them before persisting the Keycloak `sub` on Checkout and Order would
authenticate callers without proving resource ownership. The k6 baseline also
remains valid because it exercises only this still-anonymous purchase boundary.

## Next Experiment

Create a public product catalog and move product provisioning to an
administrator feature. Then introduce a backward-compatible ownership migration
for Checkout and Order and protect customer operations by both authentication
and subject equality.
