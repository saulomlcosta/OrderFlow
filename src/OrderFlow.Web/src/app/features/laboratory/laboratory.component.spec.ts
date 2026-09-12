import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { OrderflowApiService } from '../../core/orderflow-api.service';
import { AuthService } from '../../core/auth.service';
import { CheckoutResponse, OrderResponse, ProductResponse } from '../../core/orderflow.models';
import { LaboratoryComponent } from './laboratory.component';

describe('LaboratoryComponent', () => {
  let fixture: ComponentFixture<LaboratoryComponent>;
  let api: jasmine.SpyObj<OrderflowApiService>;
  const auth = {
    authenticated: signal(true),
    login: jasmine.createSpy('login').and.resolveTo()
  };

  const product: ProductResponse = {
    id: 'product-1',
    name: 'Mechanical Keyboard',
    price: 500,
    stockQuantity: 10,
    reservedStockQuantity: 0,
    availableStockQuantity: 10
  };

  const reservedProduct: ProductResponse = {
    ...product,
    reservedStockQuantity: 2,
    availableStockQuantity: 8
  };

  const activeCheckout: CheckoutResponse = {
    id: 'checkout-1',
    createdAt: '2026-09-04T12:00:00Z',
    expiresAt: '2026-09-04T12:15:00Z',
    status: 'Active',
    isExpired: false,
    items: [{ productId: product.id, quantity: 2 }]
  };

  const order: OrderResponse = {
    id: 'order-1',
    checkoutId: activeCheckout.id,
    createdAt: '2026-09-04T12:05:00Z',
    total: 1000,
    items: [{ productId: product.id, productName: product.name, unitPrice: product.price, quantity: 2 }]
  };

  beforeEach(async () => {
    api = jasmine.createSpyObj<OrderflowApiService>('OrderflowApiService', [
      'listProducts',
      'getProduct',
      'startCheckout',
      'cancelCheckout',
      'completeCheckout',
      'getOrder'
    ]);
    api.listProducts.and.returnValue(of([product]));
    auth.authenticated.set(true);
    auth.login.calls.reset();

    await TestBed.configureTestingModule({
      imports: [LaboratoryComponent],
      providers: [
        { provide: OrderflowApiService, useValue: api },
        { provide: AuthService, useValue: auth }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(LaboratoryComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  });

  it('should render the public catalog without administrative commands', () => {
    expect(api.listProducts).toHaveBeenCalledTimes(1);
    expect(pageText()).toContain(product.name);
    expect(pageText()).toContain('10 available');
    expect(pageText()).not.toContain('Create product');
    expect(pageText()).not.toContain('Add stock');
  });

  it('should start checkout for the selected catalog product', async () => {
    api.startCheckout.and.returnValue(of(activeCheckout));
    api.getProduct.and.returnValue(of(reservedProduct));

    await clickButton('Start checkout');

    expect(api.startCheckout).toHaveBeenCalledOnceWith(product.id, 2);
    expect(pageText()).toContain('2 unit(s) reserved');
    expect(pageText()).toContain('8 available');
  });

  it('should redirect an anonymous customer to login before checkout', async () => {
    auth.authenticated.set(false);
    fixture.detectChanges();

    await clickButton('Sign in to checkout');

    expect(auth.login).toHaveBeenCalledTimes(1);
    expect(api.startCheckout).not.toHaveBeenCalled();
  });

  it('should complete checkout and render the historical order', async () => {
    const completedProduct = { ...product, stockQuantity: 8, availableStockQuantity: 8 };
    api.startCheckout.and.returnValue(of(activeCheckout));
    api.getProduct.and.returnValues(of(reservedProduct), of(completedProduct));
    api.completeCheckout.and.returnValue(of({ id: order.id, checkoutId: activeCheckout.id }));
    api.getOrder.and.returnValue(of(order));
    await clickButton('Start checkout');

    await clickButton('Complete');

    expect(api.completeCheckout).toHaveBeenCalledOnceWith(activeCheckout.id);
    expect(api.getOrder).toHaveBeenCalledOnceWith(order.id);
    expect(pageText()).toContain('Checkout completed. The order now preserves the commercial snapshot.');
    expect(pageText()).toContain(order.items[0].productName);
    expect(pageText()).toContain('Completed');
  });

  it('should present catalog errors to the user', async () => {
    api.listProducts.and.returnValue(throwError(() => new HttpErrorResponse({
      status: 500,
      error: { title: 'Catalog unavailable.' }
    })));

    const errorFixture = TestBed.createComponent(LaboratoryComponent);
    errorFixture.detectChanges();
    await errorFixture.whenStable();
    errorFixture.detectChanges();

    expect(errorFixture.nativeElement.textContent).toContain('Request failed.');
    expect(errorFixture.nativeElement.textContent).toContain('Catalog unavailable.');
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
