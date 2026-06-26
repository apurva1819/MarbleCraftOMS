import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { OrderService } from '../../core/services/order.service';
import { OrderSummary } from '../../core/models/models';

@Component({
  selector: 'app-order-list',
  imports: [CommonModule, RouterLink],
  templateUrl: './order-list.html',
  styleUrl: './order-list.css'
})
export class OrderListComponent implements OnInit {
  orders      = signal<OrderSummary[]>([]);
  loading     = signal(true);
  actionError = signal('');

  // Backend may return numeric enum (0=Pending,1=Confirmed,2=Dispatched,3=Cancelled)
  private readonly LABELS = ['Pending', 'Confirmed', 'Dispatched', 'Cancelled'];

  constructor(public auth: AuthService, private orderSvc: OrderService) {}

  ngOnInit() { this.load(); }

  load() {
    this.loading.set(true);
    const customerId = this.auth.isDistributor() ? this.auth.distributorId() ?? undefined : undefined;
    this.orderSvc.getAll(customerId).subscribe(data => {
      this.orders.set(data);
      this.loading.set(false);
    });
  }

  confirm(order: OrderSummary) {
    this.orderSvc.confirm(order.id).subscribe({
      next:  () => this.load(),
      error: (err) => this.actionError.set(err?.error?.detail || err?.error?.message || 'Failed to confirm order.')
    });
  }

  dispatch(order: OrderSummary) {
    this.orderSvc.dispatch(order.id).subscribe({
      next:  () => this.load(),
      error: (err) => this.actionError.set(err?.error?.detail || err?.error?.message || 'Failed to dispatch order.')
    });
  }

  cancel(order: OrderSummary) {
    if (!confirm(`Cancel order ${order.orderNumber}?`)) return;
    this.orderSvc.cancel(order.id).subscribe({
      next:  () => this.load(),
      error: (err) => this.actionError.set(err?.error?.detail || err?.error?.message || 'Failed to cancel order.')
    });
  }

  toLabel(s: string | number): string {
    return typeof s === 'number' ? (this.LABELS[s] ?? 'Unknown') : String(s);
  }

  statusClass(s: string | number): string {
    return `badge badge-${this.toLabel(s).toLowerCase()}`;
  }

  isPending(s: string | number): boolean    { return s === 'Pending'    || s === 0; }
  isConfirmed(s: string | number): boolean  { return s === 'Confirmed'  || s === 1; }
  isNonTerminal(s: string | number): boolean {
    return s !== 'Dispatched' && s !== 2 && s !== 'Cancelled' && s !== 3;
  }
}
