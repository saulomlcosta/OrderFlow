import http from 'k6/http';
import { check } from 'k6';
import { Rate, Trend } from 'k6/metrics';

const baseUrl = __ENV.BASE_URL || 'http://host.docker.internal:5217';
const identityUrl = __ENV.IDENTITY_URL || 'http://host.docker.internal:8081';
const virtualUsers = Number.parseInt(__ENV.VUS || '10', 10);
const iterations = Number.parseInt(__ENV.ITERATIONS || '100', 10);
const maxDuration = __ENV.MAX_DURATION || '2m';

const journeySuccess = new Rate('journey_success');
const journeyDuration = new Trend('journey_duration', true);
const startCheckoutDuration = new Trend('start_checkout_duration', true);
const completeCheckoutDuration = new Trend('complete_checkout_duration', true);

export const options = {
  scenarios: {
    checkout_baseline: {
      executor: 'shared-iterations',
      vus: virtualUsers,
      iterations,
      maxDuration,
    },
  },
  thresholds: {
    checks: ['rate==1'],
    http_req_failed: ['rate==0'],
    journey_success: ['rate==1'],
  },
};

const jsonRequest = (name, token) => ({
  headers: {
    'Content-Type': 'application/json',
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
  },
  tags: { name },
});

const namedRequest = (name, token) => ({
  headers: token ? { Authorization: `Bearer ${token}` } : {},
  tags: { name },
});

const hasStatus = (response, expectedStatus, label) =>
  check(response, {
    [`${label} returns ${expectedStatus}`]: (result) => result.status === expectedStatus,
  });

const parseJson = (response) => {
  try {
    return response.json();
  } catch {
    return null;
  }
};

export function setup() {
  const response = http.get(`${baseUrl}/health/ready`, namedRequest('GET /health/ready'));

  if (!hasStatus(response, 200, 'readiness')) {
    throw new Error(`OrderFlow is not ready at ${baseUrl}.`);
  }

  const tokenResponse = http.post(
    `${identityUrl}/realms/orderflow/protocol/openid-connect/token`,
    {
      grant_type: 'client_credentials',
      client_id: __ENV.CLIENT_ID,
      client_secret: __ENV.CLIENT_SECRET,
    },
    namedRequest('POST Keycloak token'),
  );

  if (!hasStatus(tokenResponse, 200, 'automation authentication')) {
    throw new Error('Keycloak did not issue an automation access token.');
  }

  return { runId: `${Date.now()}`, accessToken: tokenResponse.json('access_token') };
}

export default function (data) {
  const startedAt = Date.now();
  const productName = `Load Product ${data.runId}-${__VU}-${__ITER}`;
  let successful = false;

  try {
    const createProductResponse = http.post(
      `${baseUrl}/products`,
      JSON.stringify({ name: productName, price: 100 }),
      jsonRequest('POST /products', data.accessToken),
    );

    if (!hasStatus(createProductResponse, 201, 'create product')) {
      return;
    }

    const product = parseJson(createProductResponse);
    if (!check(product, { 'created product has an id': (value) => Boolean(value?.id) })) {
      return;
    }

    const addStockResponse = http.post(
      `${baseUrl}/products/${product.id}/stock`,
      JSON.stringify({ quantity: 1 }),
      jsonRequest('POST /products/{id}/stock', data.accessToken),
    );

    if (!hasStatus(addStockResponse, 200, 'add stock')) {
      return;
    }

    const startCheckoutResponse = http.post(
      `${baseUrl}/checkouts`,
      JSON.stringify({ items: [{ productId: product.id, quantity: 1 }] }),
      jsonRequest('POST /checkouts', data.accessToken),
    );
    startCheckoutDuration.add(startCheckoutResponse.timings.duration);

    if (!hasStatus(startCheckoutResponse, 201, 'start checkout')) {
      return;
    }

    const checkout = parseJson(startCheckoutResponse);
    if (!check(checkout, { 'created checkout is active': (value) => value?.id && value.status === 'Active' })) {
      return;
    }

    const completeCheckoutResponse = http.post(
      `${baseUrl}/checkouts/${checkout.id}/complete`,
      null,
      namedRequest('POST /checkouts/{id}/complete', data.accessToken),
    );
    completeCheckoutDuration.add(completeCheckoutResponse.timings.duration);

    if (!hasStatus(completeCheckoutResponse, 201, 'complete checkout')) {
      return;
    }

    const createdOrder = parseJson(completeCheckoutResponse);
    if (!check(createdOrder, {
      'created order references checkout': (value) => value?.id && value.checkoutId === checkout.id,
    })) {
      return;
    }

    const productResponse = http.get(
      `${baseUrl}/products/${product.id}`,
      namedRequest('GET /products/{id}'),
    );
    const orderResponse = http.get(
      `${baseUrl}/orders/${createdOrder.id}`,
      namedRequest('GET /orders/{id}', data.accessToken),
    );

    const productAfterCompletion = parseJson(productResponse);
    const order = parseJson(orderResponse);

    const responsesAreValid = check(null, {
      'completed product returns 200': () => productResponse.status === 200,
      'created order returns 200': () => orderResponse.status === 200,
      'completion consumes physical and reserved stock': () =>
        productAfterCompletion?.stockQuantity === 0 &&
        productAfterCompletion?.reservedStockQuantity === 0 &&
        productAfterCompletion?.availableStockQuantity === 0,
      'order preserves checkout and commercial snapshot': () =>
        order?.checkoutId === checkout.id &&
        order?.total === 100 &&
        order?.items?.length === 1 &&
        order.items[0].productName === productName &&
        order.items[0].unitPrice === 100 &&
        order.items[0].quantity === 1,
    });

    successful = responsesAreValid;
  } finally {
    journeyDuration.add(Date.now() - startedAt);
    journeySuccess.add(successful);
  }
}
