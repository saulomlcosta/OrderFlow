# 2026-09-09 - Repository Guidance for Development Agents

## Context

The diagrams-as-code discovery made OrderFlow easier for people and agents to
understand, but architecture description alone does not define how an autonomous
development agent should operate in the repository.

The project already had its principles, current state, roadmap, experiments, and
journal distributed across purposeful documents. A future agent still needed a
clear entry point connecting those sources to implementation and validation
behavior.

## Decision

Add a root `AGENTS.md` that applies to the whole repository. It defines:

- the required documentation reading order
- the current architecture baseline
- business invariants that changes must preserve
- the evidence-first change workflow
- backend, PostgreSQL, Angular, and browser validation commands
- persistence and migration safety rules
- open decisions that require clarification rather than inference

The file does not duplicate the complete system description or prescribe future
architecture. It points to maintained sources of truth and translates stable
decisions into operational guidance.

## Why This Matters

README diagrams help an agent understand what exists and how it behaves.
`AGENTS.md` adds the complementary question: how should the agent work without
silently changing the project's learning method or architecture?

This distinction prevents a common failure mode in AI-assisted development:
treating roadmap questions as approved solutions and adding speculative
complexity such as microservices, messaging, or generic abstractions.

## Trade-offs

Agent guidance can become stale or overly restrictive. The file therefore
records the current baseline while allowing future decisions to change it when a
concrete problem and supporting evidence exist.

The guidance must be updated whenever core invariants, validation commands,
repository structure, or architectural boundaries change.

## Next Investigation

Resume the controlled load-test baseline. Its results may provide the first
measured pressure for performance-related decisions currently left open.
