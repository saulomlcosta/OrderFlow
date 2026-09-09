# 2026-09-09 - Diagrams as Code Discovery

## Context

An academic exercise required a public Git repository to describe a real system
in natural language and use GenAI to produce at least one structural and one
behavioral Mermaid diagram. The result also needed to record what the model
inferred correctly, what required human correction, and what remained too
ambiguous for an implementation agent.

OrderFlow was selected instead of creating a disposable example. Its public
repository, evolving architecture, and existing engineering journal make the
exercise part of the laboratory's real documentation history.

## Discovery Boundary

The structural diagram uses only a C4-inspired container level:

- Angular SPA
- ASP.NET Core Minimal API
- PostgreSQL database
- customer and administrator actors

Products, Inventory, Checkouts, and Orders are intentionally described as
logical API boundaries rather than separate containers. CI, SQLite test
substitution, source-code components, endpoints, and future technologies are
outside that structural view.

The behavioral diagram covers the critical reservation and completion journey.
It shows the reservation and completion transactions, conditional stock writes,
the successful Order creation, and business-failure rollback paths.

## Human Review

The initial architectural interpretation correctly recognized the SPA, API,
database, synchronous communication, and transactional stock protections. Human
review prevented common speculative additions: microservices, payment, queues,
cart, external providers, and automatic expiration.

The sequence was checked against the implementation rather than generated from
the product idea alone. This exposed details that matter to future agents:

- reserving stock does not reduce physical quantity
- completing checkout reduces physical and reserved quantities atomically
- Order is created only during successful completion
- Order preserves its Checkout identifier and commercial product snapshot

## Agent-readiness Gaps

The diagrams describe what exists, but they cannot authorize undecided future
design. Identity, RBAC, payment, automatic expiration, production deployment,
schema release policy, observability, workload targets, and messaging guarantees
must be specified before an agent implements those capabilities.

## Result

The README now contains the natural-language discovery frame, both Mermaid
diagrams, generation assumptions, human adjustments, and open decisions. The
documentation remains versionable, reviewable, reproducible, and directly usable
as context for future development agents.

## Next Investigation

Resume the planned load-test baseline using the documented checkout journey and
stock invariants as correctness criteria.
