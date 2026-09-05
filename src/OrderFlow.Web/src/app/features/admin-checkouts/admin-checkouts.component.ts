import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { OrderflowApiService } from '../../core/orderflow-api.service';
import {
  CheckoutResponse,
  ProductResponse,
  ValidationProblemDetails
} from '../../core/orderflow.models';

type CheckoutFilter = 'active' | 'expired' | 'completed' | 'cancelled';

@Component({
  selector: 'app-admin-checkouts',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './admin-checkouts.component.html',
  styleUrl: './admin-checkouts.component.css'
})
export class AdminCheckoutsComponent implements OnInit {
  private readonly api = inject(OrderflowApiService);

  protected readonly filters: ReadonlyArray<{ value: CheckoutFilter; label: string }> = [
    { value: 'active', label: 'Active' },
    { value: 'expired', label: 'Needs release' },
    { value: 'completed', label: 'Completed' },
    { value: 'cancelled', label: 'Cancelled' }
  ];

  protected readonly selectedFilter = signal<CheckoutFilter>('expired');
  protected readonly checkouts = signal<CheckoutResponse[]>([]);
  protected readonly selectedCheckout = signal<CheckoutResponse | null>(null);
  protected readonly products = signal<ProductResponse[]>([]);
  protected readonly loading = signal(false);
  protected readonly processingId = signal<string | null>(null);
  protected readonly error = signal('');
  protected readonly notice = signal('');

  protected readonly canExpireSelected = computed(() => {
    const checkout = this.selectedCheckout();
    return checkout?.status === 'Active' && checkout.isExpired;
  });

  protected readonly reservedUnits = computed(() =>
    this.selectedCheckout()?.items.reduce((total, item) => total + item.quantity, 0) ?? 0
  );

  async ngOnInit(): Promise<void> {
    await this.loadCheckouts();
  }

  protected async selectFilter(filter: CheckoutFilter): Promise<void> {
    if (filter === this.selectedFilter() && this.checkouts().length > 0) {
      return;
    }

    this.selectedFilter.set(filter);
    this.selectedCheckout.set(null);
    this.products.set([]);
    await this.loadCheckouts();
  }

  protected async loadCheckouts(): Promise<void> {
    this.loading.set(true);
    this.error.set('');
    this.notice.set('');

    try {
      this.checkouts.set(await firstValueFrom(this.api.listCheckouts(this.selectedFilter())));
    } catch (error) {
      this.error.set(this.toErrorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }

  protected async selectCheckout(checkoutId: string): Promise<void> {
    this.processingId.set(checkoutId);
    this.error.set('');

    try {
      const checkout = await firstValueFrom(this.api.getCheckout(checkoutId));
      this.selectedCheckout.set(checkout);
      await this.loadProducts(checkout);
    } catch (error) {
      this.error.set(this.toErrorMessage(error));
    } finally {
      this.processingId.set(null);
    }
  }

  protected async expireSelected(): Promise<void> {
    const checkout = this.selectedCheckout();

    if (!checkout || !this.canExpireSelected()) {
      return;
    }

    this.processingId.set(checkout.id);
    this.error.set('');
    this.notice.set('');

    try {
      const expiredCheckout = await firstValueFrom(this.api.expireCheckout(checkout.id));
      this.selectedCheckout.set(expiredCheckout);
      await Promise.all([
        this.loadProducts(expiredCheckout),
        this.refreshListWithoutClearingNotice()
      ]);
      this.notice.set('Reservation released. The reserved units are available again.');
    } catch (error) {
      this.error.set(this.toErrorMessage(error));
    } finally {
      this.processingId.set(null);
    }
  }

  protected displayStatus(checkout: CheckoutResponse): string {
    if (checkout.status === 'Active' && checkout.isExpired) {
      return 'Needs release';
    }

    return checkout.status;
  }

  protected statusTone(checkout: CheckoutResponse): string {
    if (checkout.status === 'Active' && checkout.isExpired) {
      return 'danger';
    }

    return checkout.status.toLowerCase();
  }

  protected productFor(productId: string): ProductResponse | undefined {
    return this.products().find(product => product.id === productId);
  }

  private async refreshListWithoutClearingNotice(): Promise<void> {
    this.checkouts.set(await firstValueFrom(this.api.listCheckouts(this.selectedFilter())));
  }

  private async loadProducts(checkout: CheckoutResponse): Promise<void> {
    const productIds = [...new Set(checkout.items.map(item => item.productId))];
    const products = await Promise.all(
      productIds.map(productId => firstValueFrom(this.api.getProduct(productId)))
    );
    this.products.set(products);
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
