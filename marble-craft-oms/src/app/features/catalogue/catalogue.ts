import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { ProductService } from '../../core/services/product.service';
import { AuthService } from '../../core/auth/auth.service';
import { ProductBrowseItem, SupplierItem } from '../../core/models/models';

@Component({
  selector: 'app-catalogue',
  imports: [CommonModule, RouterLink, ReactiveFormsModule],
  templateUrl: './catalogue.html',
  styleUrl: './catalogue.css'
})
export class CatalogueComponent implements OnInit {
  products   = signal<ProductBrowseItem[]>([]);
  page       = signal(1);
  totalPages = signal(1);
  totalCount = signal(0);
  loading    = signal(true);
  readonly pageSize = 20;

  // Add product modal
  showModal    = signal(false);
  modalLoading = signal(false);
  modalError   = signal('');
  suppliers    = signal<SupplierItem[]>([]);
  addForm: FormGroup;

  constructor(
    private productSvc: ProductService,
    public  auth: AuthService,
    fb: FormBuilder
  ) {
    this.addForm = fb.group({
      name:            ['', Validators.required],
      material:        ['', Validators.required],
      format:          ['', Validators.required],
      surface:         ['', Validators.required],
      color:           ['', Validators.required],
      size:            ['', Validators.required],
      countryOfOrigin: ['', Validators.required],
      pricePerUnit:    [null, [Validators.required, Validators.min(0.01)]],
      supplierId:      ['', Validators.required]
    });
  }

  ngOnInit() {
    this.load();
    if (this.auth.isSalesOrAdmin()) {
      this.productSvc.getSuppliers().subscribe(s => this.suppliers.set(s));
    }
  }

  load() {
    this.loading.set(true);
    this.productSvc.browse(this.page(), this.pageSize).subscribe(res => {
      this.products.set(res.items);
      this.totalPages.set(res.totalPages);
      this.totalCount.set(res.totalCount);
      this.loading.set(false);
    });
  }

  prevPage() { if (this.page() > 1)                  { this.page.update(p => p - 1); this.load(); } }
  nextPage() { if (this.page() < this.totalPages())   { this.page.update(p => p + 1); this.load(); } }

  openAddModal() {
    this.addForm.reset();
    this.modalError.set('');
    this.showModal.set(true);
  }

  closeModal() { this.showModal.set(false); }

  submitAdd() {
    if (this.addForm.invalid) return;
    this.modalLoading.set(true);
    this.modalError.set('');

    const v = this.addForm.value;
    this.productSvc.create({
      name:            v.name,
      material:        v.material,
      format:          v.format,
      surface:         v.surface,
      color:           v.color,
      size:            v.size,
      countryOfOrigin: v.countryOfOrigin,
      pricePerUnit:    +v.pricePerUnit,
      supplierId:      +v.supplierId
    }).subscribe({
      next: () => {
        this.modalLoading.set(false);
        this.showModal.set(false);
        this.page.set(1);
        this.load();
      },
      error: err => {
        this.modalError.set(err.error?.message || 'Failed to create product.');
        this.modalLoading.set(false);
      }
    });
  }
}
