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
  orders = signal<OrderSummary[]>([]);
  loading = signal(true);
  actionError = signal('');

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
      next: () => this.load(),
      error: () => this.actionError.set('Failed to confirm order.')
    });
  }

  dispatch(order: OrderSummary) {
    this.orderSvc.dispatch(order.id).subscribe({
      next: () => this.load(),
      error: () => this.actionError.set('Failed to dispatch order.')
    });
  }

  cancel(order: OrderSummary) {
    if (!confirm(`Cancel order ${order.orderNumber}?`)) return;
    this.orderSvc.cancel(order.id).subscribe({
      next: () => this.load(),
      error: () => this.actionError.set('Failed to cancel order.')
    });
  }

  statusClass(status: string): string {
    return `badge badge-${status.toLowerCase()}`;
  }
}
