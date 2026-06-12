import { Component, OnInit, ChangeDetectorRef, Input } from '@angular/core';
import { retry, timeout } from 'rxjs';
import { AuthService, AuditLogEntry } from '../../services/auth.service';

@Component({
  selector: 'app-audit',
  standalone: false,
  templateUrl: './audit.component.html',
  styleUrl: './audit.component.css'
})
export class AuditComponent implements OnInit {
  @Input() role = '';

  auditLogs: AuditLogEntry[] = [];
  filteredAuditLogs: AuditLogEntry[] = [];
  auditLoading = false;
  auditFailed = false;
  auditFilter = '';
  serviceFilter = '';

  get isAdmin(): boolean { return this.role === 'ADMIN' || this.role === 'SYSTEM_ADMIN'; }

  get services(): string[] {
    return [...new Set(this.auditLogs.map(l => l.serviceName))].sort();
  }

  private applyFilter(): void {
    let logs = this.auditLogs;
    if (this.serviceFilter) logs = logs.filter(l => l.serviceName === this.serviceFilter);
    if (this.auditFilter.trim()) {
      const q = this.auditFilter.toLowerCase();
      logs = logs.filter(l =>
        (l.actorName ?? '').toLowerCase().includes(q) ||
        (l.actorEmail ?? '').toLowerCase().includes(q) ||
        l.action.toLowerCase().includes(q) ||
        (l.description ?? '').toLowerCase().includes(q) ||
        (l.entityName ?? '').toLowerCase().includes(q) ||
        l.serviceName.toLowerCase().includes(q)
      );
    }
    this.filteredAuditLogs = logs;
    this.cdr.detectChanges();
  }

  constructor(private auth: AuthService, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void { this.loadAuditLogs(); }

  loadAuditLogs(): void {
    this.auditLoading = true;
    this.auditFailed = false;
    this.auth.getAuditLogs()
      .pipe(
        timeout(10000),
        retry({ count: 3, delay: 4000 })
      )
      .subscribe({
        next: l  => { this.auditLogs = l; this.auditLoading = false; this.applyFilter(); },
        error: () => { this.auditLoading = false; this.auditFailed = true; this.cdr.detectChanges(); }
      });
  }

  onFilterChange(): void { this.applyFilter(); }

  onTextInput(event: Event): void {
    this.auditFilter = (event.target as HTMLInputElement).value;
    this.applyFilter();
  }

  onServiceChange(event: Event): void {
    this.serviceFilter = (event.target as HTMLSelectElement).value;
    this.applyFilter();
  }

  avatarFor(name: string): string { return name ? name.substring(0, 2).toUpperCase() : '??'; }

  serviceColor(service: string): string {
    const map: Record<string, string> = {
      AuthService:     '#e3f2fd',
      PatientService:  '#e8f5e9',
      ProtocolService: '#fff3e0',
      SampleService:   '#f3e5f5'
    };
    return map[service] ?? '#f5f5f5';
  }

  serviceTextColor(service: string): string {
    const map: Record<string, string> = {
      AuthService:     '#1565c0',
      PatientService:  '#2e7d32',
      ProtocolService: '#e65100',
      SampleService:   '#6a1b9a'
    };
    return map[service] ?? '#555';
  }

  actionColor(action: string): string {
    if (['LOGIN', 'PATIENT_ENROLLED', 'PROTOCOL_CREATED', 'SITE_CREATED', 'SAMPLE_CREATED', 'LAB_RESULT_CREATED', 'USER_ENROLLED', 'SITE_ASSIGNED_TO_PROTOCOL'].includes(action)) return '#e8f5e9';
    if (['LOGOUT', 'PATIENT_WITHDRAWN', 'VISIT_CANCELLED', 'PATIENT_DEACTIVATED', 'SITE_DELETED', 'PROTOCOL_DELETED', 'SAMPLE_DELETED', 'LAB_RESULT_DELETED', 'PROTOCOL_SITE_CLOSED'].includes(action)) return '#fff3e0';
    if (['LOGIN_FAILED'].includes(action)) return '#fee2e2';
    return '#f5f5f5';
  }

  actionTextColor(action: string): string {
    if (['LOGIN', 'PATIENT_ENROLLED', 'PROTOCOL_CREATED', 'SITE_CREATED', 'SAMPLE_CREATED', 'LAB_RESULT_CREATED', 'USER_ENROLLED', 'SITE_ASSIGNED_TO_PROTOCOL'].includes(action)) return '#2e7d32';
    if (['LOGOUT', 'PATIENT_WITHDRAWN', 'VISIT_CANCELLED', 'PATIENT_DEACTIVATED', 'SITE_DELETED', 'PROTOCOL_DELETED', 'SAMPLE_DELETED', 'LAB_RESULT_DELETED', 'PROTOCOL_SITE_CLOSED'].includes(action)) return '#e65100';
    if (['LOGIN_FAILED'].includes(action)) return '#c62828';
    return '#555';
  }
}
