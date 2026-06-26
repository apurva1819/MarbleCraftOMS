import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { AddProductCommand, PagedResult, ProductBrowseItem, SupplierItem } from '../models/models';

@Injectable({ providedIn: 'root' })
export class ProductService {
  constructor(private http: HttpClient) {}

  browse(page = 1, pageSize = 20) {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PagedResult<ProductBrowseItem>>('/api/v1/products', { params });
  }

  create(cmd: AddProductCommand) {
    return this.http.post<ProductBrowseItem>('/api/v1/products', cmd);
  }

  getSuppliers() {
    return this.http.get<SupplierItem[]>('/api/v1/suppliers');
  }
}
