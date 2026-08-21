export interface ProductResponse {
  id: string;
  name: string;
  price: number;
  stockQuantity: number;
  reservedStockQuantity: number;
  availableStockQuantity: number;
}

export interface CheckoutItemResponse {
  productId: string;
  quantity: number;
}

export interface CheckoutResponse {
  id: string;
  createdAt: string;
  expiresAt: string;
  status: string;
  items: CheckoutItemResponse[];
}

export interface CompleteCheckoutResponse {
  id: string;
  checkoutId: string;
}

export interface OrderItemResponse {
  productId: string;
  productName: string;
  unitPrice: number;
  quantity: number;
}

export interface OrderResponse {
  id: string;
  createdAt: string;
  total: number;
  items: OrderItemResponse[];
}

export interface ValidationProblemDetails {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}
