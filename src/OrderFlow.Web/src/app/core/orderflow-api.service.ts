import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

import {
  CheckoutResponse,
  CompleteCheckoutResponse,
  OrderResponse,
  ProductResponse
} from './orderflow.models';

@Injectable({
  providedIn: 'root'
})
export class OrderflowApiService {
  private readonly http = inject(HttpClient);

  listProducts() {
    return this.http.get<ProductResponse[]>('/products');
  }

  createProduct(payload: { name: string; price: number }) {
    return this.http.post<ProductResponse>('/products', payload);
  }

  getProduct(id: string) {
    return this.http.get<ProductResponse>(`/products/${id}`);
  }

  addStock(productId: string, quantity: number) {
    return this.http.post<ProductResponse>(`/products/${productId}/stock`, { quantity });
  }

  startCheckout(productId: string, quantity: number) {
    return this.http.post<CheckoutResponse>('/checkouts', {
      items: [{ productId, quantity }]
    });
  }

  listCheckouts(status: 'active' | 'expired' | 'completed' | 'cancelled') {
    return this.http.get<CheckoutResponse[]>('/checkouts', {
      params: { status }
    });
  }

  listMyCheckouts() {
    return this.http.get<CheckoutResponse[]>('/me/checkouts');
  }

  getCheckout(id: string) {
    return this.http.get<CheckoutResponse>(`/checkouts/${id}`);
  }

  cancelCheckout(id: string) {
    return this.http.post<CheckoutResponse>(`/checkouts/${id}/cancel`, {});
  }

  expireCheckout(id: string) {
    return this.http.post<CheckoutResponse>(`/checkouts/${id}/expire`, {});
  }

  completeCheckout(id: string) {
    return this.http.post<CompleteCheckoutResponse>(`/checkouts/${id}/complete`, {});
  }

  getOrder(id: string) {
    return this.http.get<OrderResponse>(`/orders/${id}`);
  }

  listMyOrders() {
    return this.http.get<OrderResponse[]>('/me/orders');
  }
}
