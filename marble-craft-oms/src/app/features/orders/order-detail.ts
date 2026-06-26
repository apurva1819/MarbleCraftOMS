import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { OrderService } from '../../core/services/order.service';
import { OrderDetail } from '../../core/models/models';

@Component({
  selector: 'app-order-detail',
  imports: [CommonModule, RouterLink],
  templateUrl: './order-detail.html',
  styleUrl: './order-detail.css'
})
export class OrderDetailComponent implements OnInit {
  order   = signal<OrderDetail | null>(null);
  loading = signal(true);
  error   = signal('');
  actionError = signal('');

  constructor(
    public auth: AuthService,
    private orderSvc: OrderService,
    private route: ActivatedRoute
  ) {}

  ngOnInit() {
    const id = +this.route.snapshot.paramMap.get('id')!;
    this.orderSvc.getById(id).subscribe({
      next: o => { this.order.set(o); this.loading.set(false); },
      error: () => { this.error.set('Order not found.'); this.loading.set(false); }
    });
  }

  confirm() {
    this.actionError.set('');
    this.orderSvc.confirm(this.order()!.id).subscribe({
      next: () => this.reload(),
      error: (err) => this.actionError.set(err?.error?.detail || err?.error?.message || 'Failed to confirm order.')
    });
  }

  dispatch() {
    this.actionError.set('');
    this.orderSvc.dispatch(this.order()!.id).subscribe({
      next: () => this.reload(),
      error: (err) => this.actionError.set(err?.error?.detail || err?.error?.message || 'Failed to dispatch order.')
    });
  }

  cancel() {
    if (!confirm('Cancel this order?')) return;
    this.actionError.set('');
    this.orderSvc.cancel(this.order()!.id).subscribe({
      next: () => this.reload(),
      error: (err) => this.actionError.set(err?.error?.detail || err?.error?.message || 'Failed to cancel order.')
    });
  }

  private reload() {
    const id = this.order()!.id;
    this.orderSvc.getById(id).subscribe(o => this.order.set(o));
  }

  private readonly LABELS = ['Pending', 'Confirmed', 'Dispatched', 'Cancelled'];

  toLabel(s: string | number): string {
    return typeof s === 'number' ? (this.LABELS[s] ?? 'Unknown') : String(s);
  }

  statusClass(s: string | number): string {
    return `badge badge-${this.toLabel(s).toLowerCase()}`;
  }

  isPending(s: string | number): boolean     { return s === 'Pending'   || s === 0; }
  isConfirmed(s: string | number): boolean   { return s === 'Confirmed' || s === 1; }
  isNonTerminal(s: string | number): boolean {
    return s !== 'Dispatched' && s !== 2 && s !== 'Cancelled' && s !== 3;
  }
}
