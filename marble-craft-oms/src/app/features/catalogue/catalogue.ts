import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ProductService } from '../../core/services/product.service';
import { ProductBrowseItem } from '../../core/models/models';

@Component({
  selector: 'app-catalogue',
  imports: [CommonModule, RouterLink],
  templateUrl: './catalogue.html',
  styleUrl: './catalogue.css'
})
export class CatalogueComponent implements OnInit {
  products = signal<ProductBrowseItem[]>([]);
  page = signal(1);
  totalPages = signal(1);
  totalCount = signal(0);
  loading = signal(true);
  readonly pageSize = 20;

  constructor(private productSvc: ProductService) {}

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.productSvc.browse(this.page(), this.pageSize).subscribe(res => {
      this.products.set(res.items);
      this.totalPages.set(res.totalPages);
      this.totalCount.set(res.totalCount);
      this.loading.set(false);
    });
  }

  prevPage() { if (this.page() > 1) { this.page.update(p => p - 1); this.load(); } }
  nextPage() { if (this.page() < this.totalPages()) { this.page.update(p => p + 1); this.load(); } }
}
