import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

// ── Models (mirror Shared.DTOs / NotificationDto.cs) ───────────────────────────

export type NotificationChannel = 'IN_APP' | 'EMAIL' | 'SMS';
export type NotificationStatus  = 'UNREAD' | 'READ' | 'ESCALATED' | 'ARCHIVED';

export interface NotificationItem {
  notificationId: string;
  userId: string;
  message: string;
  category: string;
  channel: string;
  status: string;
  createdAt: string;
  readAt?: string | null;
  sentByUserId?: string | null;
  recipientName?: string | null;
}

export interface CreateNotificationRequest {
  userId: string;
  message: string;
  category: string;
  channels: string[];   // IN_APP | EMAIL | SMS
}

export interface BroadcastRequest {
  message: string;
  category: string;
  channels: string[];
  role?: string | null;  // null/empty = all active users
}

export interface CategorySummary { category: string; count: number; }

export interface NotificationHistory {
  userId: string;
  from: string;
  to: string;
  totalCount: number;
  readCount: number;
  unreadCount: number;
  escalatedCount: number;
  byCategory: CategorySummary[];
}

export interface UnreadCount { count: number; }

// Categories the UI offers when composing a notification.
export const NOTIFICATION_CATEGORIES = [
  'MILESTONE', 'LAB_RESULT', 'COMPLIANCE_DEADLINE', 'SYSTEM',
] as const;

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private apiUrl = `${environment.notificationUrl}/api/notifications`;

  constructor(private http: HttpClient) {}

  getMine(status?: string, category?: string): Observable<NotificationItem[]> {
    let params = new HttpParams();
    if (status)   params = params.set('status', status);
    if (category) params = params.set('category', category);
    return this.http.get<NotificationItem[]>(this.apiUrl, { params });
  }

  getUnreadCount(): Observable<UnreadCount> {
    return this.http.get<UnreadCount>(`${this.apiUrl}/unread-count`);
  }

  getHistory(from?: string, to?: string): Observable<NotificationHistory> {
    let params = new HttpParams();
    if (from) params = params.set('from', from);
    if (to)   params = params.set('to', to);
    return this.http.get<NotificationHistory>(`${this.apiUrl}/history`, { params });
  }

  create(dto: CreateNotificationRequest): Observable<NotificationItem[]> {
    return this.http.post<NotificationItem[]>(this.apiUrl, dto);
  }

  broadcast(dto: BroadcastRequest): Observable<NotificationItem[]> {
    return this.http.post<NotificationItem[]>(`${this.apiUrl}/broadcast`, dto);
  }

  markRead(id: string): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}/read`, {});
  }

  markAllRead(): Observable<{ updated: number }> {
    return this.http.put<{ updated: number }>(`${this.apiUrl}/read-all`, {});
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  getSent(): Observable<NotificationItem[]> {
    return this.http.get<NotificationItem[]>(`${this.apiUrl}/sent`);
  }
}
