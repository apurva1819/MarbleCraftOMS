import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { NotificationItem } from '../models/models';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  constructor(private http: HttpClient) {}

  getAll() {
    return this.http.get<NotificationItem[]>('/api/v1/notifications');
  }

  markRead(id: number) {
    return this.http.patch<void>(`/api/v1/notifications/${id}/read`, {});
  }

  markAllRead() {
    return this.http.patch<void>('/api/v1/notifications/read-all', {});
  }
}
