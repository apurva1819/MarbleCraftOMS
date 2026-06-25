import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NotificationService } from '../../core/services/notification.service';
import { NotificationItem } from '../../core/models/models';

@Component({
  selector: 'app-notifications',
  imports: [CommonModule],
  templateUrl: './notifications.html',
  styleUrl: './notifications.css'
})
export class NotificationsComponent implements OnInit {
  items    = signal<NotificationItem[]>([]);
  loading  = signal(true);
  unread   = computed(() => this.items().filter(n => !n.isRead).length);

  constructor(private notifSvc: NotificationService) {}

  ngOnInit() { this.load(); }

  load() {
    this.notifSvc.getAll().subscribe(data => {
      this.items.set(data);
      this.loading.set(false);
    });
  }

  markRead(n: NotificationItem) {
    if (n.isRead) return;
    this.notifSvc.markRead(n.id).subscribe(() => {
      this.items.update(list => list.map(x => x.id === n.id ? { ...x, isRead: true } : x));
    });
  }

  markAllRead() {
    this.notifSvc.markAllRead().subscribe(() => {
      this.items.update(list => list.map(x => ({ ...x, isRead: true })));
    });
  }

  typeIcon(type: string): string {
    if (type.toLowerCase().includes('low')) return '⚠';
    if (type.toLowerCase().includes('order')) return '📦';
    return '🔔';
  }
}
