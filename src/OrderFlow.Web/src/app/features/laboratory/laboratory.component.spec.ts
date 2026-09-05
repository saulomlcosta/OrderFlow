import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { OrderflowApiService } from '../../core/orderflow-api.service';
import { CheckoutResponse, OrderResponse, ProductResponse } from '../../core/orderflow.models';
import { LaboratoryComponent } from './laboratory.component';

describe('LaboratoryComponent', () => {
  let fixture: ComponentFixture<LaboratoryComponent>;
  let api: jasmine.SpyObj<OrderflowApiService>;

  const createdProduct: ProductResponse = {
    id: 'product-1',
    name: 'Mechanical Keyboard',
    price: 500,
    stockQuantity: 0,
    reservedStockQuantity: 0,
    availableStockQuantity: 0
  };

  const reservedProduct: ProductResponse = {
    ...createdProduct,
    stockQuantity: 10,
    reservedStockQuantity: 2,
    availableStockQuantity: 8
  };

  const activeCheckout: CheckoutResponse = {
    id: 'checkout-1',
    createdAt: '2026-09-04T12:00:00Z',
    expiresAt: '2026-09-04T12:15:00Z',
    status: 'Active',
    isExpired: false,
    items: [{ productId: createdProduct.id, quantity: 2 }]
  };

  const order: OrderResponse = {
    id: 'order-1',
    createdAt: '2026-09-04T12:05:00Z',
    total: 1000,
    items: [{
      productId: createdProduct.id,
      productName: createdProduct.name,
      unitPrice: createdProduct.price,
      quantity: 2
    }]
  };

  beforeEach(async () => {
    api = jasmine.createSpyObj<OrderflowApiService>('OrderflowApiService', [
      'createProduct',
      'getProduct',
      'addStock',
      'startCheckout',
      'cancelCheckout',
      'completeCheckout',
      'getOrder'
    ]);

    await TestBed.configureTestingModule({
      imports: [LaboratoryComponent],
      providers: [{ provide: OrderflowApiService, useValue: api }]
    }).compileComponents();

    fixture = TestBed.createComponent(LaboratoryComponent);
    fixture.detectChanges();
  });

  it('should create a product and present its initial inventory state', async () => {
    api.createProduct.and.returnValue(of(createdProduct));

    await clickButton('Create product');

    expect(api.createProduct).toHaveBeenCalledOnceWith({
      name: 'Mechanical Keyboard',
      price: 500
    });
    expect(pageText()).toContain(createdProduct.name);
    expect(pageText()).toContain(createdProduct.id);
    expect(pageText()).toContain('Product created. Add stock before starting checkout.');
  });

  it('should start checkout and show the resulting reservation', async () => {
    api.createProduct.and.returnValue(of(createdProduct));
    api.startCheckout.and.returnValue(of(activeCheckout));
    api.getProduct.and.returnValue(of(reservedProduct));
    await clickButton('Create product');

    await clickButton('Start checkout');

    expect(api.startCheckout).toHaveBeenCalledOnceWith(createdProduct.id, 2);
    expect(api.getProduct).toHaveBeenCalledOnceWith(createdProduct.id);
    expect(pageText()).toContain('2 unit(s) reserved');
    expect(pageText()).toContain('8 available');
    expect(pageText()).toContain('Checkout started. Stock is reserved for 15 minutes.');
  });

  it('should complete checkout and render the historical order', async () => {
    const completedProduct: ProductResponse = {
      ...reservedProduct,
      stockQuantity: 8,
      reservedStockQuantity: 0,
      availableStockQuantity: 8
    };
    api.createProduct.and.returnValue(of(createdProduct));
    api.startCheckout.and.returnValue(of(activeCheckout));
    api.getProduct.and.returnValues(of(reservedProduct), of(completedProduct));
    api.completeCheckout.and.returnValue(of({ id: order.id, checkoutId: activeCheckout.id }));
    api.getOrder.and.returnValue(of(order));
    await clickButton('Create product');
    await clickButton('Start checkout');

    await clickButton('Complete');

    expect(api.completeCheckout).toHaveBeenCalledOnceWith(activeCheckout.id);
    expect(api.getOrder).toHaveBeenCalledOnceWith(order.id);
    expect(pageText()).toContain('Checkout completed. The order now preserves the commercial snapshot.');
    const orderIdInput = fixture.nativeElement.querySelector(
      'input[placeholder="Paste an order id"]'
    ) as HTMLInputElement;
    expect(orderIdInput.value).toBe(order.id);
    expect(pageText()).toContain(order.items[0].productName);
    expect(pageText()).toContain('Completed');
  });

  it('should present backend validation errors to the user', async () => {
    api.createProduct.and.returnValue(throwError(() => new HttpErrorResponse({
      status: 400,
      error: { errors: { Name: ['Product name is required.'] } }
    })));

    await clickButton('Create product');

    expect(pageText()).toContain('Request failed.');
    expect(pageText()).toContain('Product name is required.');
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
