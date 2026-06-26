import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { AdjustStockCommand, AdjustStockResult, StockByProductResult, StockSummaryItem } from '../models/models';

@Injectable({ providedIn: 'root' })
export class InventoryService {
  constructor(private http: HttpClient) {}

  getSummary() {
    return this.http.get<StockSummaryItem[]>('/api/v1/inventory/summary');
  }

  getByProduct(productId: number) {
    return this.http.get<StockByProductResult>(`/api/v1/inventory/${productId}`);
  }

  adjust(cmd: AdjustStockCommand) {
    return this.http.post<AdjustStockResult>('/api/v1/inventory/adjust', cmd);
  }
}
