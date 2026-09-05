# 2026-09-05 - Checkout Transition Matrix

## Context

Checkout behavior was covered through its main successful paths and some
expiration failures, but the complete lifecycle contract was implicit. As the
same inventory can support multiple reservations, repeating a terminal action
could be especially harmful if it released or consumed stock belonging to a
different checkout.

## Matrix

| Current state | Complete | Cancel | Expire |
| --- | --- | --- | --- |
| Active before expiry | Completed | Cancelled | Rejected |
| Active after expiry | Rejected | Rejected | Expired |
| Completed | Rejected | Rejected | Rejected |
| Cancelled | Rejected | Rejected | Rejected |
| Expired | Rejected | Rejected | Rejected |

An action rejected by this matrix must preserve the current checkout state and
must not change inventory or create an order.

## Decision

Express the complete state matrix in unit tests because it belongs to the
checkout domain and can be verified quickly without infrastructure. Keep
integration coverage focused on effects that cross persistence boundaries or
depend on transaction rollback.

The integration scenarios now prove that:

- an active reservation cannot be expired before its deadline
- repeating cancellation does not release another checkout's reservation
- repeating expiration does not release another checkout's reservation
- repeating completion does not consume stock or create a second order

No production change was required. The existing domain guards and database
transactions already satisfy the newly explicit contract.

## Verification

- 27 unit tests passing
- 19 integration tests passing
- no production behavior changed

## Next Investigation

Add one minimal end-to-end browser journey across Angular, API, domain, and
persistence. Keep detailed lifecycle combinations in lower test layers rather
than duplicating the entire matrix through the browser.
