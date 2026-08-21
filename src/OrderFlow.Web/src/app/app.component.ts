import { DecimalPipe, CurrencyPipe, DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';

import { OrderflowApiService } from './core/orderflow-api.service';
import {
  CheckoutResponse,
  OrderResponse,
  ProductResponse,
  ValidationProblemDetails
} from './core/orderflow.models';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [FormsModule, CurrencyPipe, DatePipe, DecimalPipe],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  private readonly api = inject(OrderflowApiService);

  protected readonly title = 'OrderFlow';
  protected readonly subtitle = 'A laboratory UI for products, stock reservation, checkout, and order completion.';

  protected readonly createProductName = signal('Mechanical Keyboard');
  protected readonly createProductPrice = signal(500);
  protected readonly stockQuantity = signal(10);
  protected readonly checkoutQuantity = signal(2);
  protected readonly productIdInput = signal('');
  protected readonly orderIdInput = signal('');

  protected readonly currentProduct = signal<ProductResponse | null>(null);
  protected readonly currentCheckout = signal<CheckoutResponse | null>(null);
  protected readonly currentOrder = signal<OrderResponse | null>(null);
  protected readonly banner = signal<string>('Use the guided flow below to exercise the backend you have been evolving.');
  protected readonly errorMessage = signal<string>('');
  protected readonly busyAction = signal<string>('');

  protected readonly hasProduct = computed(() => this.currentProduct() !== null);
  protected readonly hasCheckout = computed(() => this.currentCheckout() !== null);
  protected readonly hasOrder = computed(() => this.currentOrder() !== null);
  protected readonly productHealthTone = computed(() => {
    const product = this.currentProduct();

    if (!product) {
      return 'neutral';
    }

    if (product.availableStockQuantity === 0) {
      return 'danger';
    }

    if (product.reservedStockQuantity > 0) {
      return 'warning';
    }

    return 'success';
  });

  protected async createProduct(): Promise<void> {
    await this.runAction('Creating product', async () => {
      const product = await firstValueFrom(
        this.api.createProduct({
          name: this.createProductName().trim(),
          price: Number(this.createProductPrice())
        })
      );

      this.currentProduct.set(product);
      this.productIdInput.set(product.id);
      this.currentCheckout.set(null);
      this.currentOrder.set(null);
      this.orderIdInput.set('');
      this.banner.set('Product created. You can now add stock and observe the inventory numbers change.');
    });
  }

  protected async loadProduct(): Promise<void> {
    await this.runAction('Loading product', async () => {
      const productId = this.requireValue(this.productIdInput(), 'Product id is required.');
      const product = await firstValueFrom(this.api.getProduct(productId));

      this.currentProduct.set(product);
      this.banner.set('Product loaded. This is the current inventory view, including reserved and available quantities.');
    });
  }

  protected async addStock(): Promise<void> {
    await this.runAction('Adding stock', async () => {
      const productId = this.resolveProductId();
      const product = await firstValueFrom(this.api.addStock(productId, Number(this.stockQuantity())));

      this.currentProduct.set(product);
      this.banner.set('Stock added. Notice that total stock changes immediately, while reserved stock stays untouched.');
    });
  }

  protected async startCheckout(): Promise<void> {
    await this.runAction('Starting checkout', async () => {
      const productId = this.resolveProductId();
      const checkout = await firstValueFrom(
        this.api.startCheckout(productId, Number(this.checkoutQuantity()))
      );

      this.currentCheckout.set(checkout);
      await this.refreshCurrentProduct(productId);
      this.banner.set('Checkout started. The stock is now reserved, not yet sold.');
    });
  }

  protected async cancelCheckout(): Promise<void> {
    await this.runAction('Cancelling checkout', async () => {
      const checkout = this.currentCheckout();

      if (!checkout) {
        throw new Error('There is no active checkout to cancel.');
      }

      const updatedCheckout = await firstValueFrom(this.api.cancelCheckout(checkout.id));
      this.currentCheckout.set(updatedCheckout);
      await this.refreshCurrentProduct(this.resolveProductId());
      this.banner.set('Checkout cancelled. Reserved stock returned to the available pool.');
    });
  }

  protected async completeCheckout(): Promise<void> {
    await this.runAction('Completing checkout', async () => {
      const checkout = this.currentCheckout();

      if (!checkout) {
        throw new Error('There is no active checkout to complete.');
      }

      const completed = await firstValueFrom(this.api.completeCheckout(checkout.id));
      this.orderIdInput.set(completed.id);
      this.currentCheckout.set({
        ...checkout,
        status: 'Completed'
      });

      await Promise.all([
        this.refreshCurrentProduct(this.resolveProductId()),
        this.loadOrderById(completed.id)
      ]);

      this.banner.set('Checkout completed. The reservation became an order and the stock was confirmed.');
    });
  }

  protected async loadOrder(): Promise<void> {
    await this.runAction('Loading order', async () => {
      const orderId = this.requireValue(this.orderIdInput(), 'Order id is required.');
      await this.loadOrderById(orderId);
      this.banner.set('Order loaded. This snapshot preserves the commercial data used at completion time.');
    });
  }

  protected trackByProductId(index: number, item: { productId: string }): string {
    return `${index}-${item.productId}`;
  }

  protected clearMessage(): void {
    this.errorMessage.set('');
  }

  private async runAction(label: string, action: () => Promise<void>): Promise<void> {
    this.clearMessage();
    this.busyAction.set(label);

    try {
      await action();
    } catch (error) {
      this.errorMessage.set(this.toErrorMessage(error));
    } finally {
      this.busyAction.set('');
    }
  }

  private async refreshCurrentProduct(productId: string): Promise<void> {
    const product = await firstValueFrom(this.api.getProduct(productId));
    this.currentProduct.set(product);
  }

  private async loadOrderById(orderId: string): Promise<void> {
    const order = await firstValueFrom(this.api.getOrder(orderId));
    this.currentOrder.set(order);
  }

  private resolveProductId(): string {
    const currentProductId = this.currentProduct()?.id;
    return this.requireValue(this.productIdInput() || currentProductId || '', 'Product id is required.');
  }

  private requireValue(value: string, errorMessage: string): string {
    const normalized = value.trim();

    if (!normalized) {
      throw new Error(errorMessage);
    }

    return normalized;
  }

  private toErrorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      const payload = error.error as ValidationProblemDetails | string | null;

      if (typeof payload === 'string' && payload.trim().length > 0) {
        return payload;
      }

      const validationMessages = payload && typeof payload === 'object' && payload.errors
        ? Object.values(payload.errors).flat()
        : [];

      if (validationMessages.length > 0) {
        return validationMessages.join(' ');
      }

      if (payload && typeof payload === 'object') {
        return payload.detail || payload.title || `Request failed with status ${error.status}.`;
      }

      return `Request failed with status ${error.status}.`;
    }

    if (error instanceof Error) {
      return error.message;
    }

    return 'An unexpected error occurred.';
  }
}
