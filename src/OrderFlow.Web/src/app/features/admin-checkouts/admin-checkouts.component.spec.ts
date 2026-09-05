import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { OrderflowApiService } from '../../core/orderflow-api.service';
import { CheckoutResponse, ProductResponse } from '../../core/orderflow.models';
import { AdminCheckoutsComponent } from './admin-checkouts.component';

describe('AdminCheckoutsComponent', () => {
  let fixture: ComponentFixture<AdminCheckoutsComponent>;
  let api: jasmine.SpyObj<OrderflowApiService>;

  const overdueCheckout: CheckoutResponse = {
    id: 'checkout-1',
    createdAt: '2026-09-04T12:00:00Z',
    expiresAt: '2026-09-04T12:15:00Z',
    status: 'Active',
    isExpired: true,
    items: [{ productId: 'product-1', quantity: 2 }]
  };

  const product: ProductResponse = {
    id: 'product-1',
    name: 'Mechanical Keyboard',
    price: 500,
    stockQuantity: 10,
    reservedStockQuantity: 2,
    availableStockQuantity: 8
  };

  beforeEach(async () => {
    api = jasmine.createSpyObj<OrderflowApiService>('OrderflowApiService', [
      'listCheckouts',
      'getCheckout',
      'getProduct',
      'expireCheckout'
    ]);
    api.listCheckouts.and.returnValue(of([]));

    await TestBed.configureTestingModule({
      imports: [AdminCheckoutsComponent],
      providers: [{ provide: OrderflowApiService, useValue: api }]
    }).compileComponents();
  });

  it('should load the overdue checkout queue when the page opens', async () => {
    api.listCheckouts.and.returnValue(of([overdueCheckout]));

    await createComponent();

    expect(api.listCheckouts).toHaveBeenCalledOnceWith('expired');
    expect(pageText()).toContain('expired checkouts');
    expect(pageText()).toContain('Needs release');
    expect(pageText()).toContain(overdueCheckout.id);
  });

  it('should explain when the selected queue is empty', async () => {
    await createComponent();

    expect(pageText()).toContain('Nothing in this queue');
    expect(pageText()).toContain('There are no checkouts matching this lifecycle filter.');
  });

  it('should show reservation details and the expiration action after selection', async () => {
    api.listCheckouts.and.returnValue(of([overdueCheckout]));
    api.getCheckout.and.returnValue(of(overdueCheckout));
    api.getProduct.and.returnValue(of(product));
    await createComponent();

    checkoutRow().click();
    await renderAsyncChanges();

    expect(api.getCheckout).toHaveBeenCalledOnceWith(overdueCheckout.id);
    expect(api.getProduct).toHaveBeenCalledOnceWith(product.id);
    expect(pageText()).toContain('Mechanical Keyboard');
    expect(pageText()).toContain('8 currently available');
    expect(pageText()).toContain('Expire and release stock');
  });

  it('should refresh the queue and inventory after expiring a reservation', async () => {
    const expiredCheckout: CheckoutResponse = {
      ...overdueCheckout,
      status: 'Expired',
      isExpired: true
    };
    const releasedProduct: ProductResponse = {
      ...product,
      reservedStockQuantity: 0,
      availableStockQuantity: 10
    };

    api.listCheckouts.and.returnValues(of([overdueCheckout]), of([]));
    api.getCheckout.and.returnValue(of(overdueCheckout));
    api.getProduct.and.returnValues(of(product), of(releasedProduct));
    api.expireCheckout.and.returnValue(of(expiredCheckout));
    await createComponent();

    checkoutRow().click();
    await renderAsyncChanges();
    expirationButton().click();
    await renderAsyncChanges();

    expect(api.expireCheckout).toHaveBeenCalledOnceWith(overdueCheckout.id);
    expect(api.listCheckouts).toHaveBeenCalledTimes(2);
    expect(pageText()).toContain('Reservation released. The reserved units are available again.');
    expect(pageText()).toContain('Nothing in this queue');
    expect(pageText()).toContain('10 currently available');
    expect(pageText()).not.toContain('Expire and release stock');
  });

  async function createComponent(): Promise<void> {
    fixture = TestBed.createComponent(AdminCheckoutsComponent);
    fixture.detectChanges();
    await renderAsyncChanges();
  }

  async function renderAsyncChanges(): Promise<void> {
    await fixture.whenStable();
    fixture.detectChanges();
  }

  function pageText(): string {
    return fixture.nativeElement.textContent;
  }

  function checkoutRow(): HTMLButtonElement {
    return fixture.nativeElement.querySelector('.checkout-row');
  }

  function expirationButton(): HTMLButtonElement {
    return [...fixture.nativeElement.querySelectorAll('button')]
      .find((button: HTMLButtonElement) => button.textContent?.includes('Expire and release stock'));
  }
});
