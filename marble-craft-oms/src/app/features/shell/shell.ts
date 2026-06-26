import { Component, OnInit, signal, computed } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../core/auth/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import { NotificationItem } from '../../core/models/models';

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, CommonModule],
  templateUrl: './shell.html',
  styleUrl: './shell.css'
})
export class ShellComponent implements OnInit {
  notifications = signal<NotificationItem[]>([]);
  showNotifPanel = signal(false);

  unreadCount = computed(() => this.notifications().filter(n => !n.isRead).length);

  constructor(
    public auth: AuthService,
    private notifSvc: NotificationService
  ) {}

  ngOnInit() { this.loadNotifications(); }

  loadNotifications() {
    this.notifSvc.getAll().subscribe(n => this.notifications.set(n));
  }

  toggleNotifPanel() {
    const opening = !this.showNotifPanel();
    this.showNotifPanel.set(opening);
    if (opening) this.loadNotifications();
  }

  markRead(n: NotificationItem) {
    if (n.isRead) return;
    this.notifSvc.markRead(n.id).subscribe(() => {
      this.notifications.update(list =>
        list.map(x => x.id === n.id ? { ...x, isRead: true } : x)
      );
    });
  }

  markAllRead() {
    this.notifSvc.markAllRead().subscribe(() => {
      this.notifications.update(list => list.map(x => ({ ...x, isRead: true })));
    });
  }
}
