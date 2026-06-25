import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { StockByProductResult, StockSummaryItem } from '../models/models';

@Injectable({ providedIn: 'root' })
export class InventoryService {
  constructor(private http: HttpClient) {}

  getSummary() {
    return this.http.get<StockSummaryItem[]>('/api/v1/inventory/summary');
  }

  getByProduct(productId: number) {
    return this.http.get<StockByProductResult>(`/api/v1/inventory/${productId}`);
  }
}
