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
    this.orderSvc.confirm(this.order()!.id).subscribe(() => this.reload());
  }

  dispatch() {
    this.orderSvc.dispatch(this.order()!.id).subscribe(() => this.reload());
  }

  cancel() {
    if (!confirm('Cancel this order?')) return;
    this.orderSvc.cancel(this.order()!.id).subscribe(() => this.reload());
  }

  private reload() {
    const id = this.order()!.id;
    this.orderSvc.getById(id).subscribe(o => this.order.set(o));
  }

  statusClass(status: string) { return `badge badge-${status.toLowerCase()}`; }
}
