import { CurrencyPipe, DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';

import { OrderflowApiService } from '../../core/orderflow-api.service';
import { AuthService } from '../../core/auth.service';
import {
  CheckoutResponse,
  OrderResponse,
  ProductResponse,
  ValidationProblemDetails
} from '../../core/orderflow.models';

@Component({
  selector: 'app-laboratory',
  standalone: true,
  imports: [FormsModule, CurrencyPipe, DatePipe],
  templateUrl: './laboratory.component.html',
  styleUrl: './laboratory.component.css'
})
export class LaboratoryComponent implements OnInit {
  private readonly api = inject(OrderflowApiService);
  protected readonly auth = inject(AuthService);

  protected readonly products = signal<ProductResponse[]>([]);
  protected readonly checkoutQuantity = signal(2);
  protected readonly product = signal<ProductResponse | null>(null);
  protected readonly checkout = signal<CheckoutResponse | null>(null);
  protected readonly order = signal<OrderResponse | null>(null);
  protected readonly message = signal('Loading the product catalog.');
  protected readonly error = signal('');
  protected readonly busy = signal('');

  protected readonly productTone = computed(() => {
    const product = this.product();

    if (!product || product.availableStockQuantity === 0) {
      return 'danger';
    }

    return product.reservedStockQuantity > 0 ? 'warning' : 'success';
  });

  ngOnInit(): void {
    void this.loadCatalog();
  }

  protected selectProduct(product: ProductResponse): void {
    this.product.set(product);
    this.checkout.set(null);
    this.order.set(null);
    this.message.set(`${product.name} selected. Choose a quantity to reserve.`);
  }

  protected async startCheckout(): Promise<void> {
    if (!this.auth.authenticated()) {
      await this.auth.login();
      return;
    }

    await this.run('Starting checkout', async () => {
      const productId = this.requireProduct().id;
      const checkout = await firstValueFrom(
        this.api.startCheckout(productId, Number(this.checkoutQuantity()))
      );

      this.checkout.set(checkout);
      await this.refreshProduct(productId);
      this.message.set('Checkout started. Stock is reserved for 15 minutes.');
    });
  }

  protected async cancelCheckout(): Promise<void> {
    await this.run('Cancelling checkout', async () => {
      const checkout = this.requireCheckout();
      this.checkout.set(await firstValueFrom(this.api.cancelCheckout(checkout.id)));
      await this.refreshProduct(this.requireProduct().id);
      this.message.set('Checkout cancelled. Reserved stock is available again.');
    });
  }

  protected async completeCheckout(): Promise<void> {
    await this.run('Completing checkout', async () => {
      const checkout = this.requireCheckout();
      const result = await firstValueFrom(this.api.completeCheckout(checkout.id));

      this.checkout.set({ ...checkout, status: 'Completed', isExpired: false });
      await this.refreshProduct(this.requireProduct().id);
      await this.loadOrderById(result.id);
      this.message.set('Checkout completed. The order now preserves the commercial snapshot.');
    });
  }

  private async loadCatalog(): Promise<void> {
    await this.run('Loading catalog', async () => {
      const products = await firstValueFrom(this.api.listProducts());
      this.products.set(products);

      if (products.length === 0) {
        this.message.set('The catalog is empty. An administrator can provision products.');
        return;
      }

      this.product.set(products[0]);
      this.message.set(`${products.length} product(s) available in the catalog.`);
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

  private requireProduct(): ProductResponse {
    const product = this.product();

    if (!product) {
      throw new Error('Select a product before starting checkout.');
    }

    return product;
  }

  private requireCheckout(): CheckoutResponse {
    const checkout = this.checkout();

    if (!checkout) {
      throw new Error('There is no checkout to process.');
    }

    return checkout;
  }

  private async refreshProduct(productId: string): Promise<void> {
    const product = await firstValueFrom(this.api.getProduct(productId));
    this.product.set(product);
    this.products.update(products => products.map(candidate =>
      candidate.id === product.id ? product : candidate
    ));
  }

  private async loadOrderById(orderId: string): Promise<void> {
    this.order.set(await firstValueFrom(this.api.getOrder(orderId)));
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
