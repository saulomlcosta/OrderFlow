# EXP-003 - Stock Reservation Planning

## Why this experiment exists

The current `OrderFlow` model assumes that stock is decreased when an order is
created, and that creating an order represents the completion of the purchase.

That worked for the earlier V1 flow because there was no:

- cart
- checkout
- payment processing
- delayed confirmation step

The next natural question is:

**When would stock need to become unavailable before the final order exists?**

This experiment existed to plan stock reservation from the domain point of view
before introducing any technical implementation.

## Current hypothesis

I expect stock reservation to become necessary only when the business flow gains
an intermediate step between "customer wants to buy" and "order is finalized."

Possible examples:

- customer starts checkout but has not finished payment yet
- customer holds limited stock during a time window
- multiple customers compete for the last available units before final confirmation

This hypothesis proved useful: reservation became justified when `Start Checkout`
was introduced as an intermediate step before final order completion.

## Current state to respect

Today the model is still:

- product represents what is being sold now
- inventory represents current available quantity
- order represents a completed commercial fact

This means the earlier system was coherent without reservation.

Today the project now has a first reservation-oriented flow:

- `Start Checkout` reserves quantity
- `Cancel Checkout` releases quantity
- `Complete Checkout` converts reservation into a final order
- direct order creation is no longer the main purchase entrypoint

## Questions to answer

1. At what exact moment should stock stop being available to other customers?
2. Is reservation triggered by cart, checkout, payment attempt, or something else?
3. How long does a reservation live?
4. When is a reservation confirmed into a final stock decrement?
5. When is a reservation released?
6. Does reservation freeze only quantity, or also price?
7. Is a reservation an explicit business concept, or only a derived inventory state?

## Possible future concepts

Do not implement these yet just because they look reasonable.

- `OnHand`
- `Reserved`
- `Available`
- `StockReservation`
- expiration time
- release flow
- confirmation flow

## Example scenarios to reason about

### Scenario 1

- stock = `1`
- customer A starts checkout
- customer B starts checkout shortly after

Question:

- should B still see that unit as available?

### Scenario 2

- customer A reserves stock
- payment fails

Question:

- when and how is the reserved quantity released?

### Scenario 3

- customer A adds product to cart
- price changes before checkout

Question:

- does reservation affect quantity only, or should it also preserve price expectations?

## Success criteria for the investigation

This experiment is successful if it gives a clear answer to at least one of these:

- stock reservation is justified by a concrete future flow
- stock reservation is still premature for the current system
- the project now understands which business events would require reservation

The goal was not to implement reservation immediately.

The goal was to identify the domain conditions that would justify it.

## Candidate next step

- clarify expiration and payment-related reservation rules

## Outcome

The first implementation decision was:

- reservation starts at `Start Checkout`
- reservation protects quantity only
- price is still determined when the final order is created
- release is currently explicit through checkout cancellation

This keeps the concept smaller while still introducing a real intermediate
business step between purchase intent and purchase completion.
