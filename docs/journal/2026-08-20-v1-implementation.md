# V1 Implementation

## What was implemented

Implemented the first functional OrderFlow version as a single ASP.NET Core
Minimal API application with logical boundaries for Products, Inventory, and
Orders.

The API now supports:

- creating products
- adding stock
- getting a product
- creating orders
- getting orders

Orders now capture product name and unit price at creation time so they
represent historical commercial facts rather than current catalog state.

## Important decisions made

Used EF Core with SQLite as the simplest relational persistence option for V1.

Kept the main application in one .NET project and organized behavior by
feature folders and namespaces instead of splitting into multiple projects.

Allowed endpoints to use the DbContext directly so we can learn where
abstractions become useful later instead of adding them preemptively now.

Kept one shared DbContext for now, because the current order flow still crosses
Products, Inventory, and Orders in a single simple transaction.

Moved EF Core mapping details closer to each module instead of keeping all
mapping logic centralized in the DbContext. This keeps persistence simple while
making module boundaries easier to see.

This feels like a good middle point for V1:

- stronger logical boundaries than a fully centralized persistence setup
- less complexity than multiple DbContexts or multiple .NET projects
- better alignment with the kind of architectural evolution we want to observe

## Known limitations

- No update product endpoint
- No stock reservation
- No concurrency protection for simultaneous purchases
- No payment flow
- No authentication or authorization
- Database initialization uses `EnsureCreated`, not migrations

## Questions discovered

- What breaks first when multiple clients try to buy the last available units?
- When will direct endpoint-to-DbContext code become hard to change or test?
- When will product reads and order reads need different shapes or optimization?
- When does database initialization need to move from `EnsureCreated` to migrations?

## Investigation note

To make the first concurrency failure reproducible, we temporarily introduced a
diagnostic hook in the order creation flow used only by integration tests.

The goal was not to create a production solution.

The goal was to force two requests to cross the stock validation point with the
same previously observed inventory state so we could prove the overselling
problem deterministically.

This kind of temporary instrumentation is useful in the laboratory because it
helps separate:

- reproducing the problem
- understanding the problem
- fixing the problem

After reproducing the issue, the first correction chosen was:

- atomic stock update
- transaction covering stock decrement and order creation

This was intentionally smaller than introducing a broad concurrency abstraction.

After the fix was validated, the temporary diagnostic hook was removed so the
main application flow would keep only the behavior required by the business.
