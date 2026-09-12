import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { OrderflowApiService } from '../../core/orderflow-api.service';
import { CheckoutResponse, OrderResponse, ProductResponse } from '../../core/orderflow.models';
import { AccountComponent } from './account.component';

describe('AccountComponent', () => {
  let fixture: ComponentFixture<AccountComponent>;
  let api: jasmine.SpyObj<OrderflowApiService>;

  const product: ProductResponse = {
    id: 'product-1',
    name: 'Mechanical Keyboard',
    price: 500,
    stockQuantity: 10,
    reservedStockQuantity: 2,
    availableStockQuantity: 8
  };

  const activeCheckout: CheckoutResponse = {
    id: 'checkout-1',
    createdAt: '2026-09-12T12:00:00Z',
    expiresAt: '2026-09-12T12:15:00Z',
    status: 'Active',
    isExpired: false,
    items: [{ productId: product.id, quantity: 2 }]
  };

  const order: OrderResponse = {
    id: 'order-1',
    checkoutId: activeCheckout.id,
    createdAt: '2026-09-12T12:05:00Z',
    total: 1000,
    items: [{ productId: product.id, productName: product.name, unitPrice: product.price, quantity: 2 }]
  };

  beforeEach(async () => {
    api = jasmine.createSpyObj<OrderflowApiService>('OrderflowApiService', [
      'listMyCheckouts',
      'listMyOrders',
      'listProducts',
      'completeCheckout',
      'cancelCheckout'
    ]);
    api.listMyCheckouts.and.returnValue(of([activeCheckout]));
    api.listMyOrders.and.returnValue(of([order]));
    api.listProducts.and.returnValue(of([product]));

    await TestBed.configureTestingModule({
      imports: [AccountComponent],
      providers: [{ provide: OrderflowApiService, useValue: api }]
    }).compileComponents();

    fixture = TestBed.createComponent(AccountComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  });

  it('should render the signed-in customers checkout and order history', () => {
    expect(api.listMyCheckouts).toHaveBeenCalledTimes(1);
    expect(api.listMyOrders).toHaveBeenCalledTimes(1);
    expect(api.listProducts).toHaveBeenCalledTimes(1);
    expect(pageText()).toContain('My checkouts');
    expect(pageText()).toContain(product.name);
    expect(pageText()).toContain('Complete purchase');
    expect(pageText()).toContain('$1,000.00');
  });

  it('should complete an active checkout and refresh account history', async () => {
    api.completeCheckout.and.returnValue(of({ id: order.id, checkoutId: activeCheckout.id }));

    await clickButton('Complete purchase');

    expect(api.completeCheckout).toHaveBeenCalledOnceWith(activeCheckout.id);
    expect(api.listMyCheckouts).toHaveBeenCalledTimes(2);
    expect(api.listMyOrders).toHaveBeenCalledTimes(2);
    expect(pageText()).toContain('Checkout completed. Your order is now available below.');
  });

  it('should show an expired reservation without customer actions', async () => {
    api.listMyCheckouts.and.returnValue(of([{ ...activeCheckout, isExpired: true }]));

    const expiredFixture = TestBed.createComponent(AccountComponent);
    expiredFixture.detectChanges();
    await expiredFixture.whenStable();
    expiredFixture.detectChanges();

    const text = expiredFixture.nativeElement.textContent;
    expect(text).toContain('Awaiting release');
    expect(text).toContain('This reservation expired and is awaiting administrative stock release.');
    expect(text).not.toContain('Complete purchase');
    expect(text).not.toContain('Cancel reservation');
  });

  async function clickButton(label: string): Promise<void> {
    const button = [...fixture.nativeElement.querySelectorAll('button')]
      .find((candidate: HTMLButtonElement) => candidate.textContent?.trim() === label);

    if (!button) {
      throw new Error(`Button "${label}" was not found.`);
    }

    button.click();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  function pageText(): string {
    return fixture.nativeElement.textContent;
  }
});
