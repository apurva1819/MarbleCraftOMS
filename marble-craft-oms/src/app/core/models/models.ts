export interface LoginResponse {
  token: string;
  expiresAt: string;
  role: string;
}

export interface JwtPayload {
  sub: string;
  role: string;
  distributorId?: number;
  exp: number;
}

export interface ProductBrowseItem {
  id: number;
  name: string;
  material: string;
  format: string;
  surface: string;
  color: string;
  size: string;
  countryOfOrigin: string;
  pricePerUnit: number;
  supplierId: number;
  supplierName: string;
  quantityAvailable: number;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface StockSummaryItem {
  productId: number;
  productName: string;
  material: string;
  totalOnHand: number;
  totalCommitted: number;
  totalAvailable: number;
  lotCount: number;
}

export interface StockLotDetail {
  lotId: number;
  lotNumber: string;
  onHand: number;
  committed: number;
  available: number;
  unitCostPerSqm: number;
  receivedDate: string;
}

export interface StockByProductResult {
  productId: number;
  productName: string;
  totalOnHand: number;
  totalCommitted: number;
  totalAvailable: number;
  lots: StockLotDetail[];
}

export type OrderStatus = 'Pending' | 'Confirmed' | 'Dispatched' | 'Cancelled';

export interface OrderLineDetail {
  id: number;
  productId: number;
  productName: string;
  stockLotId: number;
  lotNumber: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface OrderSummary {
  id: number;
  orderNumber: string;
  customerId: number;
  customerName: string;
  status: OrderStatus;
  orderDate: string;
  lineCount: number;
  totalAmount: number;
}

export interface OrderDetail {
  id: number;
  orderNumber: string;
  customerId: number;
  customerName: string;
  status: OrderStatus;
  notes: string;
  orderDate: string;
  createdAt: string;
  lines: OrderLineDetail[];
  totalAmount: number;
}

export interface PlaceOrderResponse {
  orderId: number;
  orderNumber: string;
  status: OrderStatus;
  createdAt: string;
}

export interface OrderLineRequest {
  productId: number;
  stockLotId: number;
  quantity: number;
  unitPrice: number;
}

export interface PlaceOrderCommand {
  customerId: number;
  notes: string;
  lines: OrderLineRequest[];
}

export interface NotificationItem {
  id: number;
  type: string;
  title: string;
  body: string;
  isRead: boolean;
  createdAt: string;
}
