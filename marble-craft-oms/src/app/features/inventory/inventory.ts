import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { InventoryService } from '../../core/services/inventory.service';
import { AuthService } from '../../core/auth/auth.service';
import { StockLotDetail, StockSummaryItem } from '../../core/models/models';

@Component({
  selector: 'app-inventory',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './inventory.html',
  styleUrl: './inventory.css'
})
export class InventoryComponent implements OnInit {
  items    = signal<StockSummaryItem[]>([]);
  loading  = signal(true);

  totalOnHand = computed(() => this.items().reduce((s, i) => s + i.totalOnHand, 0));
  totalAvail  = computed(() => this.items().reduce((s, i) => s + i.totalAvailable, 0));
  lowCount    = computed(() => this.items().filter(i => i.totalAvailable <= 50).length);

  // Adjust modal
  showModal      = signal(false);
  modalItem      = signal<StockSummaryItem | null>(null);
  modalLots      = signal<StockLotDetail[]>([]);
  modalLoading   = signal(false);
  modalError     = signal('');
  modalSuccess   = signal('');
  adjustForm: FormGroup;

  constructor(
    private inventorySvc: InventoryService,
    public  auth: AuthService,
    fb: FormBuilder
  ) {
    this.adjustForm = fb.group({
      stockLotId: ['', Validators.required],
      type:       ['Receive', Validators.required],
      quantity:   [1, [Validators.required, Validators.min(1)]],
      reason:     ['', Validators.required]
    });
  }

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    this.inventorySvc.getSummary().subscribe(data => {
      this.items.set(data);
      this.loading.set(false);
    });
  }

  utilizationPct(item: StockSummaryItem): number {
    if (item.totalOnHand === 0) return 0;
    return Math.round((item.totalCommitted / item.totalOnHand) * 100);
  }

  isLow(item: StockSummaryItem): boolean { return item.totalAvailable <= 50; }

  openAdjust(item: StockSummaryItem) {
    this.modalItem.set(item);
    this.modalError.set('');
    this.modalSuccess.set('');
    this.adjustForm.reset({ type: 'Receive', quantity: 1, stockLotId: '', reason: '' });
    this.inventorySvc.getByProduct(item.productId).subscribe(res => {
      this.modalLots.set(res.lots);
      this.showModal.set(true);
    });
  }

  closeModal() {
    this.showModal.set(false);
    this.modalItem.set(null);
    this.modalLots.set([]);
    this.modalSuccess.set('');
    this.modalError.set('');
  }

  submitAdjust() {
    if (this.adjustForm.invalid) return;
    this.modalLoading.set(true);
    this.modalError.set('');
    this.modalSuccess.set('');

    const v = this.adjustForm.value;
    this.inventorySvc.adjust({
      stockLotId: +v.stockLotId,
      type:       v.type,
      quantity:   +v.quantity,
      reason:     v.reason
    }).subscribe({
      next: res => {
        this.modalSuccess.set(
          `Lot ${res.lotNumber} updated — On Hand: ${res.quantityOnHand} · Committed: ${res.quantityCommitted} · Available: ${res.quantityAvailable}`
        );
        this.modalLoading.set(false);
        this.load();
      },
      error: err => {
        this.modalError.set(err.error?.detail || err.error?.message || 'Adjustment failed. Please try again.');
        this.modalLoading.set(false);
      }
    });
  }
}
