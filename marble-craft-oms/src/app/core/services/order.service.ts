import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { OrderDetail, OrderSummary, PlaceOrderCommand, PlaceOrderResponse } from '../models/models';

@Injectable({ providedIn: 'root' })
export class OrderService {
  constructor(private http: HttpClient) {}

  getAll(customerId?: number) {
    let params = new HttpParams();
    if (customerId) params = params.set('customerId', customerId);
    return this.http.get<OrderSummary[]>('/api/v1/orders', { params });
  }

  getById(id: number) {
    return this.http.get<OrderDetail>(`/api/v1/orders/${id}`);
  }

  place(cmd: PlaceOrderCommand) {
    return this.http.post<PlaceOrderResponse>('/api/v1/orders', cmd);
  }

  confirm(id: number) {
    return this.http.patch<void>(`/api/v1/orders/${id}/confirm`, {});
  }

  dispatch(id: number) {
    return this.http.patch<void>(`/api/v1/orders/${id}/dispatch`, {});
  }

  cancel(id: number) {
    return this.http.delete<void>(`/api/v1/orders/${id}`);
  }
}
