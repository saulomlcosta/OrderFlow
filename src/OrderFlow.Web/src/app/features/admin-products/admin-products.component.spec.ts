import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { OrderflowApiService } from '../../core/orderflow-api.service';
import { ProductResponse } from '../../core/orderflow.models';
import { AdminProductsComponent } from './admin-products.component';

describe('AdminProductsComponent', () => {
  let fixture: ComponentFixture<AdminProductsComponent>;
  let api: jasmine.SpyObj<OrderflowApiService>;

  const product: ProductResponse = {
    id: 'product-1',
    name: 'Mechanical Keyboard',
    price: 500,
    stockQuantity: 0,
    reservedStockQuantity: 0,
    availableStockQuantity: 0
  };

  beforeEach(async () => {
    api = jasmine.createSpyObj<OrderflowApiService>('OrderflowApiService', [
      'listProducts',
      'createProduct',
      'addStock'
    ]);
    api.listProducts.and.returnValue(of([]));

    await TestBed.configureTestingModule({
      imports: [AdminProductsComponent],
      providers: [{ provide: OrderflowApiService, useValue: api }]
    }).compileComponents();

    fixture = TestBed.createComponent(AdminProductsComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  });

  it('should load the operational catalog on initialization', () => {
    expect(api.listProducts).toHaveBeenCalledTimes(1);
    expect(pageText()).toContain('No products exist yet. Create the first catalog entry.');
  });

  it('should create and select a product', async () => {
    api.createProduct.and.returnValue(of(product));

    await clickButton('Create product');

    expect(api.createProduct).toHaveBeenCalledOnceWith({ name: product.name, price: product.price });
    expect(pageText()).toContain(product.name);
    expect(pageText()).toContain('Product created. Add physical stock before customers can reserve it.');
  });

  it('should add stock to the selected product', async () => {
    const stockedProduct = { ...product, stockQuantity: 10, availableStockQuantity: 10 };
    api.createProduct.and.returnValue(of(product));
    api.addStock.and.returnValue(of(stockedProduct));
    await clickButton('Create product');

    await clickButton('Add stock');

    expect(api.addStock).toHaveBeenCalledOnceWith(product.id, 10);
    expect(pageText()).toContain('10 available');
    expect(pageText()).toContain('Physical stock added. Availability is visible in the storefront.');
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
