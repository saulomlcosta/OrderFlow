# 2026-09-05 - Continuous Integration

## Context

The repository now has meaningful automated protection across domain behavior,
HTTP and persistence integration, and Angular user interactions. Running every
command manually leaves room for skipped checks and does not prove that a fresh
machine can restore and build the committed repository.

## Decision

Introduce a minimal GitHub Actions workflow for pushes and pull requests that
target `master`. Keep backend and frontend in independent jobs so their failures
are easy to identify and both stacks can execute in parallel.

The backend job:

- installs the .NET 10 SDK
- restores the solution
- builds in Release configuration
- runs unit and integration tests without rebuilding

The frontend job:

- installs Node.js 24
- restores the exact dependency tree with `npm ci`
- creates the Angular production build
- runs Angular tests in headless Chrome

The workflow has read-only repository permissions, a ten-minute timeout per job,
manual dispatch support, and concurrency cancellation for superseded runs on the
same branch or pull request.

## Why CI Now

CI now protects executable business contracts rather than compilation alone.
It also verifies that lockfiles and declared dependencies are sufficient on a
clean Linux runner, exposing accidental reliance on local caches or files.

## Why Not CD Yet

No deployment target, production database, migration strategy, secret model,
health check, or rollback procedure has been selected. Automating deployment
before those decisions would create infrastructure without a defined operating
model.

## Initial Quality Gate

- backend restore and Release build
- 27 unit tests
- 19 integration tests
- frontend clean dependency installation and production build
- 12 Angular tests

Known dependency vulnerability warnings are not converted into a failing gate
in this increment. Dependency remediation will be investigated separately so
the initial workflow represents the repository's current reproducible baseline.

## Next Investigation

Add one minimal end-to-end browser journey without duplicating the lifecycle
matrix already covered below the UI.

## Remote Verification

The first push-triggered workflow completed successfully on a clean Linux
runner. Backend and frontend jobs both passed, confirming that the repository
can be restored, built, and tested without relying on the local development
machine.

- run: `33943950387`
- backend: passed
- frontend: passed
