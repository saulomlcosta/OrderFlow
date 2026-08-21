# EXP-002 - Partial Failure During Order Creation

## Why this experiment exists

`EXP-001` taught us that stock protection under concurrency needed to move closer
to the real database update.

The current order flow now uses:

- atomic stock update
- transaction covering stock decrement and order creation

That improves correctness under concurrent purchase attempts.

The next natural question is different:

**If order creation fails after stock handling has started, does the system return
to a consistent state?**

This experiment exists to verify the consistency guarantees of the current flow
when something goes wrong in the middle.

## Current hypothesis

I expect the current implementation to remain consistent if a failure happens
after the transaction starts but before the transaction is committed.

Possible reason:

- stock decrement and order persistence are now part of the same transaction
- if the transaction is not committed, both changes should be discarded

If this hypothesis is wrong, the system may create one of these inconsistent states:

- stock decreased without a created order
- order created without the expected stock change
- part of the operation visible while another part is missing

## Questions to answer

1. What exactly must remain consistent when an order is created?
2. Which steps of the flow are part of the current transaction?
3. What happens if an exception is thrown after stock is decremented but before commit?
4. Is rollback explicit, implicit, or both in the current implementation?
5. Can the system leave behind a state that looks valid locally but is wrong globally?
6. What evidence would prove that the transaction is really protecting the full operation?

## Experiment setup

Use a product with known stock and trigger a controlled failure after stock
decrement succeeds but before the transaction commits.

Suggested baseline:

- create one product
- add stock quantity `3`
- trigger order creation for quantity `1`
- force a failure after stock handling but before commit

## What to observe

- HTTP response from the failed order attempt
- final stock quantity
- total number of created orders
- whether any partial change survived the failure

## Success criteria for the investigation

This experiment is successful if it gives a clear answer to at least one of these:

- the transaction protects the operation as expected
- the current flow still leaks partial changes
- the failure happens in a different place than expected

The goal is not to add infrastructure immediately.

The goal is to verify whether the transaction now protects exactly what we think
it protects.

## Possible next solutions to compare later

Do not implement these yet just because they exist.

- more explicit transaction boundary patterns
- failure injection test infrastructure
- outbox or messaging reliability patterns
- broader operational error handling

## Notes after running the experiment

Result:

- the current transaction protected the operation as expected

Evidence:

- with stock quantity `3`
- a controlled failure was triggered after stock decrement and before commit
- the HTTP response returned `500 Internal Server Error`
- no new order was created
- final stock quantity remained `3`

This means the partial failure did not leave behind a decremented stock or a
half-finished order.

Surprise:

- the rollback guarantee was easier to validate by observing business facts
  than by inspecting transaction code alone

This is an important lesson: transactional consistency becomes much clearer when
it is tested through persisted outcomes, not just through implementation intent.

New question:

- which failures are still outside the protection of this transaction boundary?

Candidate next step:

- compare failures that happen inside the transaction with failures that happen
  before it starts or after it commits
