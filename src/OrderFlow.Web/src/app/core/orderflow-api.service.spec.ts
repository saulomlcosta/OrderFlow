import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { OrderflowApiService } from './orderflow-api.service';
import { CheckoutResponse, ProductResponse } from './orderflow.models';

describe('OrderflowApiService', () => {
  let service: OrderflowApiService;
  let http: HttpTestingController;

  const expiredCheckout: CheckoutResponse = {
    id: 'checkout-1',
    createdAt: '2026-09-04T12:00:00Z',
    expiresAt: '2026-09-04T12:15:00Z',
    status: 'Active',
    isExpired: true,
    items: [{ productId: 'product-1', quantity: 2 }]
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });

    service = TestBed.inject(OrderflowApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('should list products for the public catalog', () => {
    const product: ProductResponse = {
      id: 'product-1',
      name: 'Mechanical Keyboard',
      price: 500,
      stockQuantity: 10,
      reservedStockQuantity: 0,
      availableStockQuantity: 10
    };

    service.listProducts().subscribe(products => expect(products).toEqual([product]));

    const request = http.expectOne('/products');
    expect(request.request.method).toBe('GET');
    request.flush([product]);
  });

  it('should list checkouts using the lifecycle filter', () => {
    service.listCheckouts('expired').subscribe(checkouts => {
      expect(checkouts).toEqual([expiredCheckout]);
    });

    const request = http.expectOne(request =>
      request.url === '/checkouts' && request.params.get('status') === 'expired'
    );

    expect(request.request.method).toBe('GET');
    request.flush([expiredCheckout]);
  });

  it('should expire a checkout through the administrative operation', () => {
    service.expireCheckout(expiredCheckout.id).subscribe(checkout => {
      expect(checkout.status).toBe('Expired');
    });

    const request = http.expectOne(`/checkouts/${expiredCheckout.id}/expire`);
    expect(request.request.method).toBe('POST');
    request.flush({ ...expiredCheckout, status: 'Expired' });
  });
});
