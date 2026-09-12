import { CurrencyPipe, DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { OrderflowApiService } from '../../core/orderflow-api.service';
import {
  CheckoutResponse,
  OrderResponse,
  ProductResponse,
  ValidationProblemDetails
} from '../../core/orderflow.models';

@Component({
  selector: 'app-account',
  standalone: true,
  imports: [CurrencyPipe, DatePipe],
  templateUrl: './account.component.html',
  styleUrl: './account.component.css'
})
export class AccountComponent implements OnInit {
  private readonly api = inject(OrderflowApiService);

  protected readonly checkouts = signal<CheckoutResponse[]>([]);
  protected readonly orders = signal<OrderResponse[]>([]);
  protected readonly products = signal<ProductResponse[]>([]);
  protected readonly loading = signal(true);
  protected readonly processingId = signal<string | null>(null);
  protected readonly error = signal('');
  protected readonly notice = signal('');

  async ngOnInit(): Promise<void> {
    await this.loadHistory();
  }

  protected async refresh(): Promise<void> {
    await this.loadHistory();
  }

  protected canComplete(checkout: CheckoutResponse): boolean {
    return checkout.status === 'Active' && !checkout.isExpired;
  }

  protected canCancel(checkout: CheckoutResponse): boolean {
    return checkout.status === 'Active' && !checkout.isExpired;
  }

  protected displayStatus(checkout: CheckoutResponse): string {
    return checkout.status === 'Active' && checkout.isExpired
      ? 'Awaiting release'
      : checkout.status;
  }

  protected productName(productId: string): string {
    return this.products().find(product => product.id === productId)?.name ?? productId;
  }

  protected async complete(checkout: CheckoutResponse): Promise<void> {
    await this.process(checkout.id, async () => {
      await firstValueFrom(this.api.completeCheckout(checkout.id));
      await this.loadHistory(false);
      this.notice.set('Checkout completed. Your order is now available below.');
    });
  }

  protected async cancel(checkout: CheckoutResponse): Promise<void> {
    await this.process(checkout.id, async () => {
      await firstValueFrom(this.api.cancelCheckout(checkout.id));
      await this.loadHistory(false);
      this.notice.set('Checkout cancelled. Its reserved stock is available again.');
    });
  }

  private async loadHistory(showLoading = true): Promise<void> {
    if (showLoading) {
      this.loading.set(true);
    }

    this.error.set('');

    try {
      // Keep isolated SQLite browser tests free from parallel connection locks.
      this.checkouts.set(await firstValueFrom(this.api.listMyCheckouts()));
      this.orders.set(await firstValueFrom(this.api.listMyOrders()));
      this.products.set(await firstValueFrom(this.api.listProducts()));
    } catch (error) {
      this.error.set(this.toErrorMessage(error));
    } finally {
      if (showLoading) {
        this.loading.set(false);
      }
    }
  }

  private async process(checkoutId: string, action: () => Promise<void>): Promise<void> {
    this.processingId.set(checkoutId);
    this.error.set('');
    this.notice.set('');

    try {
      await action();
    } catch (error) {
      this.error.set(this.toErrorMessage(error));
    } finally {
      this.processingId.set(null);
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
