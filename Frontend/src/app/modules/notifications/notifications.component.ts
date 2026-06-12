import { Component, OnInit, OnDestroy, ChangeDetectorRef, Input } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { finalize, timeout } from 'rxjs';
import { AuthService, EnrolledUser, RoleOption } from '../../services/auth.service';
import { NotificationService, NotificationItem, NotificationHistory, NOTIFICATION_CATEGORIES } from '../../services/notification.service';

@Component({
  selector: 'app-notifications',
  standalone: false,
  templateUrl: './notifications.component.html',
  styleUrl: './notifications.component.css'
})
export class NotificationsComponent implements OnInit, OnDestroy {
  @Input() role = '';
  @Input() currentUserId = '';

  notifications: NotificationItem[] = [];
  notifHistory: NotificationHistory | null = null;
  notifLoading = false;
  notifError = '';
  notifUnread = 0;
  notifStatusFilter = '';
  notifCategoryFilter = '';
  notifCategories = NOTIFICATION_CATEGORIES;

  activeTab: 'inbox' | 'sent' = 'inbox';
  sentNotifications: NotificationItem[] = [];
  sentLoading = false;

  showSendPanel = false;
  notifForm!: FormGroup;
  notifSending = false;
  notifSendError = '';
  notifSendSuccess = '';

  users: EnrolledUser[] = [];
  roles: RoleOption[] = [];

  get isAdmin(): boolean { return this.role === 'ADMIN' || this.role === 'SYSTEM_ADMIN'; }
  get canSend(): boolean {
    const r = (this.role || '').toUpperCase().replace(/[_\s]/g, '');
    return r === 'ADMIN' || r === 'SYSTEMADMIN';
  }
  get canBroadcast(): boolean { return this.isAdmin; }

  get notifRecipients(): EnrolledUser[] {
    return this.users.filter(u => u.userId !== this.currentUserId && u.isActive);
  }
  get recipientRoleOptions(): string[] {
    return Array.from(new Set(this.notifRecipients.map(u => u.role))).sort();
  }
  get recipientsForRole(): EnrolledUser[] {
    const role = this.notifForm?.get('recipientRole')?.value;
    if (!role) return [];
    return this.notifRecipients.filter(u => u.role === role);
  }

  constructor(private notify: NotificationService, private auth: AuthService, private fb: FormBuilder, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.notifForm = this.fb.group({
      mode:          ['user'],
      recipientRole: [''],
      userId:        [''],
      role:          [''],
      message:       ['', [Validators.required, Validators.minLength(3)]],
      category:      ['MILESTONE', Validators.required],
    });
    this.loadNotifications();
    this.loadNotifHistory();
    this.loadUnreadCount();
  }

  ngOnDestroy(): void {}

  loadNotifications(): void {
    this.notifError = '';
    this.notifLoading = this.notifications.length === 0;
    this.notify.getMine(this.notifStatusFilter || undefined, this.notifCategoryFilter || undefined)
      .pipe(timeout(8000))
      .subscribe({
        next: n => { this.notifications = n; this.notifLoading = false; this.cdr.detectChanges(); },
        error: err => {
          this.notifLoading = false;
          this.notifError = err.status === 0
            ? 'Cannot reach the Notification service. Make sure it is running on port 5103.'
            : (err.error?.message || 'Failed to load notifications.');
          this.cdr.detectChanges();
        }
      });
  }

  loadNotifHistory(): void {
    this.notify.getHistory().subscribe({
      next: h => { this.notifHistory = h; this.cdr.detectChanges(); },
      error: () => {}
    });
  }

  loadUnreadCount(): void {
    this.notify.getUnreadCount().subscribe({
      next: c => { this.notifUnread = c.count; this.cdr.detectChanges(); },
      error: () => {}
    });
  }

  switchTab(tab: 'inbox' | 'sent'): void {
    this.activeTab = tab;
    if (tab === 'sent' && this.sentNotifications.length === 0) this.loadSentNotifications();
  }

  loadSentNotifications(): void {
    this.sentLoading = true;
    this.notify.getSent().pipe(timeout(8000)).subscribe({
      next: n => { this.sentNotifications = n; this.sentLoading = false; this.cdr.detectChanges(); },
      error: () => { this.sentLoading = false; this.cdr.detectChanges(); }
    });
  }

  onFilterChange(): void { this.loadNotifications(); }

  markRead(n: NotificationItem): void {
    if (n.status === 'READ') return;
    this.notify.markRead(n.notificationId).subscribe({
      next: () => { n.status = 'READ'; n.readAt = new Date().toISOString(); this.loadUnreadCount(); this.loadNotifHistory(); this.cdr.detectChanges(); },
      error: () => {}
    });
  }

  markAllRead(): void {
    this.notify.markAllRead().subscribe({
      next: () => { this.notifications.forEach(n => { if (n.status === 'UNREAD') n.status = 'READ'; }); this.loadUnreadCount(); this.loadNotifHistory(); this.cdr.detectChanges(); },
      error: () => {}
    });
  }

  deleteNotif(n: NotificationItem): void {
    this.notify.delete(n.notificationId).subscribe({
      next: () => { this.notifications = this.notifications.filter(x => x.notificationId !== n.notificationId); this.loadUnreadCount(); this.loadNotifHistory(); this.cdr.detectChanges(); },
      error: () => {}
    });
  }

  openSendPanel(): void {
    this.notifForm.reset({ mode: 'user', recipientRole: '', userId: '', role: '', message: '', category: 'MILESTONE' });
    this.notifSendError = '';
    this.notifSendSuccess = '';
    if (this.users.length === 0) this.auth.getUsers().subscribe({ next: u => { this.users = u; this.cdr.detectChanges(); } });
    if (this.roles.length === 0) this.auth.getRoles().subscribe({ next: r => { this.roles = r; this.cdr.detectChanges(); } });
    this.showSendPanel = true;
  }
  closeSendPanel(): void { this.showSendPanel = false; }

  onRecipientRoleChange(): void { this.notifForm.patchValue({ userId: '' }); }

  submitNotif(): void {
    if (this.notifForm.invalid) return;
    const v = this.notifForm.value;
    const channels = ['IN_APP'];
    if (v.mode === 'user' && !v.userId) { this.notifSendError = 'Please choose a recipient.'; return; }
    this.notifSending = true;
    this.notifSendError = '';
    this.notifSendSuccess = '';
    const done = (label: string) => {
      this.notifSendSuccess = label;
      this.loadUnreadCount();
      this.loadNotifications();
      this.loadNotifHistory();
      setTimeout(() => { this.showSendPanel = false; this.notifSendSuccess = ''; this.cdr.detectChanges(); }, 1500);
    };
    const fail = (err: any) => {
      this.notifSendError = err.status === 403 ? 'Your role is not permitted to send this.' : (err.error?.message || 'Could not send notification.');
      this.cdr.detectChanges();
    };
    if (v.mode === 'broadcast') {
      this.notify.broadcast({ message: v.message, category: v.category, channels, role: v.role || null })
        .pipe(finalize(() => { this.notifSending = false; this.cdr.detectChanges(); }))
        .subscribe({ next: list => done(`Broadcast sent to ${list.length} recipient(s).`), error: fail });
    } else {
      this.notify.create({ userId: v.userId, message: v.message, category: v.category, channels })
        .pipe(finalize(() => { this.notifSending = false; this.cdr.detectChanges(); }))
        .subscribe({ next: () => done('Notification sent.'), error: fail });
    }
  }

  categoryColor(cat: string): string {
    switch (cat) {
      case 'MILESTONE':           return '#e3f2fd';
      case 'LAB_RESULT':          return '#e8f5e9';
      case 'COMPLIANCE_DEADLINE': return '#fff3e0';
      default:                    return '#f3e5f5';
    }
  }
  categoryText(cat: string): string {
    switch (cat) {
      case 'MILESTONE':           return '#1565c0';
      case 'LAB_RESULT':          return '#2e7d32';
      case 'COMPLIANCE_DEADLINE': return '#e65100';
      default:                    return '#6a1b9a';
    }
  }
}
