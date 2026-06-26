import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormArray, FormGroup, FormControl, Validators } from '@angular/forms';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { ProductService } from '../../core/services/product.service';
import { InventoryService } from '../../core/services/inventory.service';
import { OrderService } from '../../core/services/order.service';
import { ProductBrowseItem, StockLotDetail } from '../../core/models/models';

@Component({
  selector: 'app-place-order',
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './place-order.html',
  styleUrl: './place-order.css'
})
export class PlaceOrderComponent implements OnInit {
  products   = signal<ProductBrowseItem[]>([]);
  lotsMap    = signal<Record<number, StockLotDetail[]>>({});
  submitting = signal(false);
  error      = signal('');
  success    = signal('');

  form: FormGroup;

  constructor(
    private fb: FormBuilder,
    public auth: AuthService,
    private productSvc: ProductService,
    private inventorySvc: InventoryService,
    private orderSvc: OrderService,
    private router: Router,
    private route: ActivatedRoute
  ) {
    this.form = this.fb.group({ notes: [''], lines: this.fb.array([]) });
  }

  get lines(): FormArray { return this.form.get('lines') as FormArray; }

  ngOnInit() {
    this.productSvc.browse(1, 100).subscribe(res => {
      this.products.set(res.items);
      const preId = this.route.snapshot.queryParamMap.get('productId');
      this.addLine(preId ? +preId : undefined);
    });
  }

  addLine(preProductId?: number) {
    const idx = this.lines.length;
    const group = new FormGroup({
      productId:  new FormControl<number | ''>(preProductId ?? '', Validators.required),
      stockLotId: new FormControl<number | ''>('' , Validators.required),
      quantity:   new FormControl<number>(1, [Validators.required, Validators.min(1)]),
      unitPrice:  new FormControl<number>({ value: 0, disabled: true })
    });
    this.lines.push(group);
    if (preProductId) this.onProductChange(idx, preProductId);
  }

  removeLine(i: number) { this.lines.removeAt(i); }

  onProductChange(lineIdx: number, productId: number | string) {
    const id = +productId;
    if (!id) return;
    const product = this.products().find(p => p.id === id);
    if (product) {
      this.lines.at(lineIdx).get('unitPrice')!.setValue(product.pricePerUnit);
    }
    this.inventorySvc.getByProduct(id).subscribe(res => {
      this.lotsMap.update(m => ({ ...m, [lineIdx]: res.lots }));
      this.lines.at(lineIdx).get('stockLotId')!.setValue('');
    });
  }

  lotsForLine(i: number): StockLotDetail[] { return this.lotsMap()[i] ?? []; }

  lineTotal(i: number): number {
    const line = this.lines.at(i);
    return (line.get('unitPrice')!.value ?? 0) * (line.get('quantity')!.value ?? 0);
  }

  grandTotal(): number {
    return this.lines.controls.reduce((s, _, i) => s + this.lineTotal(i), 0);
  }

  submit() {
    if (this.form.invalid || this.lines.length === 0) return;
    this.submitting.set(true);
    this.error.set('');

    const cmd = {
      customerId: +(this.auth.distributorId() ?? 0),
      notes: this.form.value.notes ?? '',
      lines: this.lines.controls.map(l => ({
        productId:  +l.get('productId')!.value,
        stockLotId: +l.get('stockLotId')!.value,
        quantity:   +l.get('quantity')!.value,
        unitPrice:  +(l.get('unitPrice')!.value ?? 0)
      }))
    };

    this.orderSvc.place(cmd).subscribe({
      next: (res) => {
        this.success.set(`Order ${res.orderNumber} placed!`);
        setTimeout(() => this.router.navigate(['/orders']), 1500);
      },
      error: (err) => {
        this.error.set(err?.error?.detail || err?.error?.message || 'Failed to place order. Check stock availability.');
        this.submitting.set(false);
      }
    });
  }
}
