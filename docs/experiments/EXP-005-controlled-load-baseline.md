# EXP-005 - Controlled Load-Test Baseline

## Why this experiment exists

The PostgreSQL concurrency tests prove correctness when a small number of
requests collide on the same inventory or checkout. They do not measure latency,
throughput, error rate, or behavior under a sustained stream of complete
business journeys.

The project needs observations before it can discuss performance targets or
justify optimization, caching, connection-pool changes, or architectural
separation.

## Question

How does the current modular monolith behave when multiple virtual users execute
the complete purchase journey concurrently against PostgreSQL?

## Tool Decision

Use k6 `v2.2.0` through its pinned Docker image.

k6 was chosen because it provides virtual-user scheduling, HTTP metrics,
percentiles, checks, custom metrics, and process exit codes without adding a
load-generation project to the application solution. A custom HttpClient loop
was rejected because it would recreate measurement and reporting behavior.
NBomber remains a valid .NET alternative, but no current requirement needs its
in-process extensibility.

## Isolation

The harness uses infrastructure that is separate from Development:

- PostgreSQL database `orderflow_load` on host port `5433`
- temporary API process on `http://localhost:5217`
- API compiled and executed in Release configuration
- tmpfs-backed database storage removed after the run
- k6 container that reaches the host API through `host.docker.internal`

The persistent `orderflow` Development database and its Docker volume are not
read, truncated, or removed.

## Initial Workload

- executor: shared iterations
- virtual users: 10
- total iterations: 100
- maximum duration: 2 minutes
- think time: none
- one unique product and inventory row per journey
- six business HTTP requests per completed journey, plus one readiness probe

Each iteration creates a product, adds one unit of stock, starts a checkout,
completes it, and then reads the product and order.

This is a local saturation-oriented baseline, not a model of real customer
traffic. Unique products avoid a hot-row contention test; focused contention is
already covered separately by PostgreSQL integration tests.

## Correctness Criteria

The run fails if:

- any HTTP request is unsuccessful
- any k6 check fails
- any complete journey fails
- stock is not zero after successful completion
- reserved stock is not zero after successful completion
- the Order does not reference the Checkout
- the Order does not preserve product name, unit price, quantity, and total

No latency threshold exists yet. Percentiles and throughput from the first run
are measurements to record, not promises or production SLOs.

## Metrics to Observe

- `http_req_duration` for aggregate HTTP latency
- `http_reqs` and iteration rate for throughput
- `journey_duration` for end-to-end business latency
- `start_checkout_duration` for reservation latency
- `complete_checkout_duration` for transactional completion latency
- `http_req_failed`, `checks`, and `journey_success` for correctness

## Result

The first Release baseline ran on 2026-09-10 with:

- Windows 11 Pro `10.0.26200`
- AMD Ryzen 5 5600G, 6 physical cores and 12 logical processors
- 31.9 GiB host memory
- .NET SDK `10.0.302`
- Docker Engine `28.5.1`, allocated 12 CPUs and 15.6 GiB memory
- PostgreSQL `18.6-alpine`
- k6 `v2.2.0`

| Metric | Result |
| --- | ---: |
| Completed journeys | 100 of 100 |
| Successful checks | 1,101 of 1,101 |
| HTTP requests | 601 |
| HTTP request failures | 0 |
| Journey throughput | 65.45 iterations/s |
| HTTP throughput | 393.36 requests/s |
| Aggregate HTTP duration, median | 8.55 ms |
| Aggregate HTTP duration, p95 | 58.74 ms |
| Aggregate HTTP duration, maximum | 445.98 ms |
| Complete journey duration, median | 57 ms |
| Complete journey duration, p90 | 157.6 ms |
| Complete journey duration, p95 | 937 ms |
| Complete journey duration, maximum | 938 ms |
| Start checkout duration, median | 11.33 ms |
| Start checkout duration, p95 | 237.69 ms |
| Complete checkout duration, median | 15.67 ms |
| Complete checkout duration, p95 | 157.2 ms |

All correctness thresholds passed. The environment was removed automatically,
and the persistent Development database was untouched.

The large distance between median and p95 journey latency indicates a short
tail concentrated in a few iterations. The complete run lasted about 1.5
seconds, so startup warm-up and the first database operations can dominate high
percentiles. One local run is insufficient to attribute the tail or establish a
service-level objective.

## Next Decision

Repeat the same profile enough times to understand variance and separate warm-up
from steady state before increasing concurrency or introducing arrival-rate
traffic. Do not optimize based on this first observation alone.

## References

- [Grafana k6 scenarios](https://grafana.com/docs/k6/latest/using-k6/scenarios/)
- [Grafana k6 thresholds](https://grafana.com/docs/k6/latest/using-k6/thresholds/)
- [k6 v2.2.0 release](https://github.com/grafana/k6/releases/tag/v2.2.0)
