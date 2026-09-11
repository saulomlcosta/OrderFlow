# OrderFlow Agent Guidance

This file defines how development agents should understand and change the
OrderFlow repository. It applies to the entire repository.

## Read Before Changing Code

Read these sources in order:

1. `README.md` for the system description, architecture discovery, and runtime
   diagrams.
2. `docs/PROJECT.md` for the laboratory purpose and learning cycle.
3. `docs/CURRENT_STATE.MD` for implemented capabilities and the active step.
4. `docs/ROADMAP.md` for questions worth investigating, not pre-approved
   solutions.
5. `docs/reports/SYSTEM_FLOW.md` for the current business and infrastructure
   flow.
6. The most recent relevant entries in `docs/journal/` and `docs/experiments/`
   before revisiting an architectural decision.

Treat documentation as evidence of current decisions, not permission to invent
missing requirements. If sources conflict, prefer executable behavior and the
most recent documented decision, then surface the conflict.

## Core Engineering Principle

Do not introduce technology, abstraction, or physical separation without a
concrete problem that justifies it.

Reproduce and understand the problem before proposing a solution. When a change
has meaningful architectural trade-offs, document the problem, alternatives,
decision, and evidence.

## Current Architecture Baseline

- The backend is one ASP.NET Core Minimal API application.
- Products, Inventory, Checkouts, and Orders are logical modules inside that
  application, not microservices.
- One EF Core DbContext and one relational database coordinate cross-module
  transactions.
- PostgreSQL is the Development persistence provider.
- SQLite in-memory is an explicit substitution for fast isolated tests; it is
  not proof of PostgreSQL-specific concurrency behavior.
- The frontend is one Angular 18+ application written in TypeScript.
- Communication between Angular and the API is synchronous HTTP/JSON.
- Keycloak is the Development identity provider. Angular uses Authorization
  Code with PKCE, and the API validates JWT bearer tokens.
- Product creation, stock addition, checkout listing, and manual expiration
  require the `administrator` role. Customer operations remain anonymous until
  resource ownership is implemented.
- There is no payment provider, message broker, or cart yet.

Do not add repositories, generic abstractions, CQRS, MediatR, new DbContexts,
queues, services, or deployment units merely because they may be useful later.
They remain valid options only after the laboratory demonstrates the problem
they would solve.

## Business Invariants

- `Quantity` is physical stock.
- `ReservedQuantity` is stock assigned to active checkouts.
- Available stock is `Quantity - ReservedQuantity` and must never be negative.
- Starting a checkout increases only reserved quantity and persists an Active
  checkout with an expiration time.
- Cancelling or explicitly expiring a checkout releases reserved quantity
  without reducing physical stock.
- Completing a checkout atomically reduces physical and reserved quantities,
  marks the checkout Completed, and creates exactly one Order.
- An overdue checkout cannot be completed. Time alone does not release stock;
  the Expired transition must be persisted.
- Every new Order references its originating Checkout and preserves the product
  name and price used at completion.
- Legacy Orders may have a null `CheckoutId` because the relationship was added
  through a backward-compatible migration.

Protect these invariants with tests whenever behavior changes.

## Change Workflow

1. Inspect the working tree and relevant implementation before deciding.
2. Preserve unrelated user changes and generated local files.
3. Make the smallest coherent change that solves the demonstrated problem.
4. Add or update tests at the lowest useful level, then cover high-risk provider
   or browser behavior where appropriate.
5. Update `docs/CURRENT_STATE.MD` when capabilities or the next step change.
6. Add a journal entry for meaningful experiments, trade-offs, migrations, or
   architectural decisions.
7. Update diagrams when a business flow, runtime container, dependency, or
   architectural boundary changes.
8. Use English for code, identifiers, documentation, and commit messages.

Never rewrite history, remove persistent data, or revert changes not created by
the current task unless the user explicitly authorizes it.

## Validation

Run the smallest relevant checks while iterating and the complete affected suite
before delivery.

Backend baseline:

```powershell
dotnet test OrderFlow.slnx
```

PostgreSQL-specific concurrency, with the local database running:

```powershell
$env:ORDERFLOW_RUN_POSTGRESQL_TESTS = "true"
dotnet test OrderFlow.slnx
Remove-Item Env:ORDERFLOW_RUN_POSTGRESQL_TESTS
```

Frontend behavioral checks:

```powershell
cd src/OrderFlow.Web
npm run build
npm test -- --watch=false --browsers=ChromeHeadless
```

Browser journey when an end-to-end behavior changes:

```powershell
cd src/OrderFlow.Web
npm run test:e2e
```

Controlled local load baseline, with Docker Desktop running:

```powershell
.\tests\OrderFlow.LoadTests\run.ps1
```

Do not convert local latency observations into service-level objectives or CI
thresholds without an explicit workload and environment decision.

For documentation-only changes, at minimum verify Markdown structure, Mermaid
blocks when changed, links, and `git diff --check`. CI remains the final clean
Linux validation.

## Persistence Safety

- Generate EF Core migrations from deliberate model changes; inspect both `Up`
  and `Down` before applying them.
- Preserve existing Development data unless data removal is the explicit goal.
- Exercise destructive migration or rollback scenarios only against the
  isolated `orderflow_tests` database.
- Do not delete the Docker volume or reset the Development database without
  explicit user approval.

## Decisions That Require Clarification

Do not choose these on behalf of the project without a task-specific decision:

- customer ownership and whether customer operations require authentication
- production identity hardening and Keycloak deployment topology
- payment workflow and failure semantics
- automatic reservation expiration ownership
- production hosting and deployment topology
- production migration and rollback process
- service-level objectives and expected workload
- observability platform and retention policy
- caching strategy
- messaging, delivery guarantees, and idempotency boundaries
- physical module or service separation

When work reaches one of these gaps, explain the concrete pressure, viable
options, and trade-offs before implementation.
