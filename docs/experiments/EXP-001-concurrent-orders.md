# EXP-001 - Concurrent Orders Against the Same Stock

## Why this experiment exists

The current V1 flow works well under simple conditions:

- create a product
- add stock
- create an order
- decrease stock

This is enough to establish a behavioral baseline.

Now the next important question is not "does it work?" but:

**Does it remain correct when multiple customers try to buy the same stock at the same time?**

This experiment exists to discover whether the current implementation can oversell,
and to understand why that happens before introducing any solution.

## Current hypothesis

I expect the current implementation to behave correctly with one request at a time,
but to be vulnerable when concurrent requests try to purchase the same product.

Possible reason:

- two requests may read the same inventory quantity before either one saves its changes
- both requests may conclude that stock is available
- both requests may try to complete

If that happens, the system may violate an important business rule:

**Never sell more units than are available in stock.**

## Questions to answer

1. What is the critical invariant of this flow?
2. Where is that invariant protected today?
3. Is the protection happening at the right moment?
4. What does the database guarantee today, and what does it not guarantee?
5. Can two requests observe the same old stock value and both proceed?
6. If the system fails, is the problem caused by missing validation or by a race condition?
7. What is the smallest possible correction if the problem is confirmed?

## Experiment setup

Use one product with a small stock quantity.

Suggested baseline:

- create one product
- add stock quantity `1`
- send multiple order requests concurrently for quantity `1`

Possible variations:

- stock `2`, three concurrent requests for quantity `1`
- stock `5`, ten concurrent requests for quantity `1`
- one request for quantity `2` competing with two requests for quantity `1`

## What to observe

- how many requests return success
- how many requests return failure
- final stock quantity
- number of created orders
- whether success responses match real stock availability

## Success criteria for the investigation

This experiment is successful if it gives a clear answer to at least one of these:

- the current implementation is safe under this scenario
- the current implementation oversells
- the current implementation fails in some other inconsistent way

The goal is not to fix the issue immediately.

The goal is to produce evidence, understand the failure mode, and only then discuss
possible solutions.

## Possible next solutions to compare later

- optimistic concurrency
- database locking strategies
- redesign of the order creation flow

## Notes after running the experiment

Result:

- the current implementation can oversell under concurrency

Evidence:

- with stock quantity `1`
- two concurrent order requests for quantity `1` both returned success
- two orders were created
- final stock quantity was `0`

This means the system accepted two purchases for the same single unit.

Surprise:

- the final stock quantity did not become negative
- the inconsistency appears in the number of successful orders, not in a visibly invalid stock value

This is an important lesson: concurrency bugs do not always leave obviously broken data.
Sometimes the data still looks plausible while the business rule has already been violated.

New question:

- what is the smallest correction that prevents two requests from completing based on the same previously observed stock state?

Candidate next step:

- implement a smaller correction and rerun the same experiment

## Follow-up after the first fix

Implemented:

- atomic stock update
- transaction covering stock decrement and order creation

Follow-up result:

- with stock quantity `1`
- two concurrent order requests for quantity `1`
- one request returned success
- one request returned failure
- one order was created
- final stock quantity was `0`

What this teaches:

- the critical guarantee became stronger when it moved closer to the real data update
- the transaction and the atomic stock update solve different concerns
- the atomic update decides whether stock can still be decremented
- the transaction ensures stock decrement and order creation succeed or fail together
