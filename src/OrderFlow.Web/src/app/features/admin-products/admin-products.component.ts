import { CurrencyPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';

import { OrderflowApiService } from '../../core/orderflow-api.service';
import { ProductResponse, ValidationProblemDetails } from '../../core/orderflow.models';

@Component({
  selector: 'app-admin-products',
  standalone: true,
  imports: [FormsModule, CurrencyPipe],
  templateUrl: './admin-products.component.html',
  styleUrl: './admin-products.component.css'
})
export class AdminProductsComponent implements OnInit {
  private readonly api = inject(OrderflowApiService);

  protected readonly products = signal<ProductResponse[]>([]);
  protected readonly selectedProduct = signal<ProductResponse | null>(null);
  protected readonly productName = signal('Mechanical Keyboard');
  protected readonly productPrice = signal(500);
  protected readonly stockQuantity = signal(10);
  protected readonly message = signal('Loading the operational catalog.');
  protected readonly error = signal('');
  protected readonly busy = signal('');

  ngOnInit(): void {
    void this.loadProducts();
  }

  protected selectProduct(product: ProductResponse): void {
    this.selectedProduct.set(product);
    this.message.set(`${product.name} selected for inventory operations.`);
  }

  protected async createProduct(): Promise<void> {
    await this.run('Creating product', async () => {
      const product = await firstValueFrom(this.api.createProduct({
        name: this.productName().trim(),
        price: Number(this.productPrice())
      }));

      this.products.update(products => [...products, product]
        .sort((left, right) => left.name.localeCompare(right.name)));
      this.selectedProduct.set(product);
      this.message.set('Product created. Add physical stock before customers can reserve it.');
    });
  }

  protected async addStock(): Promise<void> {
    await this.run('Adding stock', async () => {
      const selected = this.selectedProduct();

      if (!selected) {
        throw new Error('Select a product before adding stock.');
      }

      const product = await firstValueFrom(
        this.api.addStock(selected.id, Number(this.stockQuantity()))
      );

      this.selectedProduct.set(product);
      this.products.update(products => products.map(candidate =>
        candidate.id === product.id ? product : candidate
      ));
      this.message.set('Physical stock added. Availability is visible in the storefront.');
    });
  }

  private async loadProducts(): Promise<void> {
    await this.run('Loading products', async () => {
      const products = await firstValueFrom(this.api.listProducts());
      this.products.set(products);
      this.selectedProduct.set(products[0] ?? null);
      this.message.set(products.length === 0
        ? 'No products exist yet. Create the first catalog entry.'
        : `${products.length} product(s) ready for administration.`);
    });
  }

  private async run(label: string, action: () => Promise<void>): Promise<void> {
    this.error.set('');
    this.busy.set(label);

    try {
      await action();
    } catch (error) {
      this.error.set(this.toErrorMessage(error));
    } finally {
      this.busy.set('');
    }
  }

  private toErrorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      const payload = error.error as ValidationProblemDetails | string | null;
      const validationMessages = payload && typeof payload === 'object' && payload.errors
        ? Object.values(payload.errors).flat()
        : [];

      if (validationMessages.length > 0) {
        return validationMessages.join(' ');
      }

      if (payload && typeof payload === 'object') {
        return payload.detail || payload.title || `Request failed with status ${error.status}.`;
      }
    }

    return error instanceof Error ? error.message : 'An unexpected error occurred.';
  }
}
