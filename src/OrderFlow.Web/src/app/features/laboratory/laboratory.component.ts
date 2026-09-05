import { CurrencyPipe, DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';

import { OrderflowApiService } from '../../core/orderflow-api.service';
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
export class LaboratoryComponent {
  private readonly api = inject(OrderflowApiService);

  protected readonly productName = signal('Mechanical Keyboard');
  protected readonly productPrice = signal(500);
  protected readonly stockQuantity = signal(10);
  protected readonly checkoutQuantity = signal(2);
  protected readonly productIdInput = signal('');
  protected readonly orderIdInput = signal('');

  protected readonly product = signal<ProductResponse | null>(null);
  protected readonly checkout = signal<CheckoutResponse | null>(null);
  protected readonly order = signal<OrderResponse | null>(null);
  protected readonly message = signal('Create a product to begin the purchase flow.');
  protected readonly error = signal('');
  protected readonly busy = signal('');

  protected readonly productTone = computed(() => {
    const product = this.product();

    if (!product || product.availableStockQuantity === 0) {
      return 'danger';
    }

    return product.reservedStockQuantity > 0 ? 'warning' : 'success';
  });

  protected async createProduct(): Promise<void> {
    await this.run('Creating product', async () => {
      const product = await firstValueFrom(this.api.createProduct({
        name: this.productName().trim(),
        price: Number(this.productPrice())
      }));

      this.product.set(product);
      this.productIdInput.set(product.id);
      this.checkout.set(null);
      this.order.set(null);
      this.orderIdInput.set('');
      this.message.set('Product created. Add stock before starting checkout.');
    });
  }

  protected async loadProduct(): Promise<void> {
    await this.run('Loading product', async () => {
      const product = await firstValueFrom(this.api.getProduct(this.requireProductId()));
      this.product.set(product);
      this.message.set('Product loaded with its current inventory state.');
    });
  }

  protected async addStock(): Promise<void> {
    await this.run('Adding stock', async () => {
      const product = await firstValueFrom(
        this.api.addStock(this.requireProductId(), Number(this.stockQuantity()))
      );
      this.product.set(product);
      this.message.set('Stock added. The product is ready for checkout.');
    });
  }

  protected async startCheckout(): Promise<void> {
    await this.run('Starting checkout', async () => {
      const productId = this.requireProductId();
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
      await this.refreshProduct(this.requireProductId());
      this.message.set('Checkout cancelled. Reserved stock is available again.');
    });
  }

  protected async completeCheckout(): Promise<void> {
    await this.run('Completing checkout', async () => {
      const checkout = this.requireCheckout();
      const result = await firstValueFrom(this.api.completeCheckout(checkout.id));

      this.checkout.set({ ...checkout, status: 'Completed', isExpired: false });
      this.orderIdInput.set(result.id);
      await Promise.all([
        this.refreshProduct(this.requireProductId()),
        this.loadOrderById(result.id)
      ]);
      this.message.set('Checkout completed. The order now preserves the commercial snapshot.');
    });
  }

  protected async loadOrder(): Promise<void> {
    await this.run('Loading order', async () => {
      const orderId = this.orderIdInput().trim();

      if (!orderId) {
        throw new Error('Order id is required.');
      }

      await this.loadOrderById(orderId);
      this.message.set('Order loaded.');
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

  private requireProductId(): string {
    const productId = (this.productIdInput() || this.product()?.id || '').trim();

    if (!productId) {
      throw new Error('Product id is required.');
    }

    return productId;
  }

  private requireCheckout(): CheckoutResponse {
    const checkout = this.checkout();

    if (!checkout) {
      throw new Error('There is no checkout to process.');
    }

    return checkout;
  }

  private async refreshProduct(productId: string): Promise<void> {
    this.product.set(await firstValueFrom(this.api.getProduct(productId)));
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
