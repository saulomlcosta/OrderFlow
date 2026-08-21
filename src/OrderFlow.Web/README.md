# OrderFlow Web

This frontend was generated with Angular 18 and adapted to exercise the current OrderFlow backend flow.

## Development server

1. Start the backend with `dotnet run` from `src/OrderFlow.Api`.
2. Start the frontend with `npm start` from `src/OrderFlow.Web`.
3. Open `http://localhost:4200`.

The dev server proxies `/products`, `/checkouts`, and `/orders` to `http://localhost:5216`, so the UI talks directly to the local API without extra setup.

## Current flow

- Create a product
- Add stock
- Start a checkout to reserve stock
- Cancel or complete that checkout
- Load the final order snapshot

## Build

Run `npm run build` to create a production build.

## Tests

Run `npm test` to execute the Angular unit tests.
