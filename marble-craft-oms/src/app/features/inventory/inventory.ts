import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { InventoryService } from '../../core/services/inventory.service';
import { StockSummaryItem } from '../../core/models/models';

@Component({
  selector: 'app-inventory',
  imports: [CommonModule],
  templateUrl: './inventory.html',
  styleUrl: './inventory.css'
})
export class InventoryComponent implements OnInit {
  items = signal<StockSummaryItem[]>([]);
  loading = signal(true);

  totalOnHand  = computed(() => this.items().reduce((s, i) => s + i.totalOnHand, 0));
  totalAvail   = computed(() => this.items().reduce((s, i) => s + i.totalAvailable, 0));
  lowCount     = computed(() => this.items().filter(i => i.totalAvailable <= 50).length);

  constructor(private inventorySvc: InventoryService) {}

  ngOnInit() {
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
}
