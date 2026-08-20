## Product vs Inventory

Question:
Are Product and Inventory the same concept?

My current understanding:

Product represents what we sell.
Inventory represents how much of it is available.

They are conceptually different.

However, conceptual separation does not necessarily
mean physical/application/service separation.

Important:
Different responsibility != different microservice.

## Current state vs historical fact

Product represents the current state of something being sold.

Order represents a historical business transaction.

Therefore, OrderItem should preserve the information
necessary to represent that transaction even if Product changes.

Product.Price != OrderItem.UnitPrice

## Inventory - First hypothesis

I initially think stock should only be decreased
when the customer finishes the purchase.

Potential problem:

If two customers are buying the last unit at the
same time, both may complete the purchase before
the stock is decreased.

Possible future concept:
Stock reservation.

Do not implement yet.