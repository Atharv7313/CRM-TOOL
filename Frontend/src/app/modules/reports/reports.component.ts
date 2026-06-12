import { Component, OnInit, ChangeDetectorRef, Input } from '@angular/core';
import { finalize, timeout } from 'rxjs';
import {
  ReportingService, DashboardSummary, KpiReport, KpiAlert, KpiMetrics,
  KPI_SCOPES, KpiScopeName
} from '../../services/reporting.service';

@Component({
  selector: 'app-reports',
  standalone: false,
  templateUrl: './reports.component.html',
  styleUrl: './reports.component.css'
})
export class ReportsComponent implements OnInit {
  @Input() role = '';

  reportSummary: DashboardSummary | null = null;
  reportSummaryLoading = false;
  reports: KpiReport[] = [];
  reportsLoading = false;
  reportsError = '';
  reportScopeFilter: 'ALL' | KpiScopeName = 'ALL';
  scopeOptions = KPI_SCOPES;
  pdfDownloading = false;

  // Generate report modal
  showGenerateModal = false;
  generateScope: KpiScopeName = 'Enrollment';
  generateProtocolId = '';
  generateLoading = false;
  generateError = '';

  kpiCards = [
    { key: 'enrollmentRate',       label: 'Enrollment Rate',   trendKey: 'enrollmentRateChange',       color: '#e3f2fd', accent: '#1565c0' },
    { key: 'sampleProcessingRate', label: 'Sample Processing', trendKey: 'sampleProcessingRateChange', color: '#e8f5e9', accent: '#2e7d32' },
    { key: 'complianceScore',      label: 'Compliance Score',  trendKey: 'complianceScoreChange',      color: '#f3e5f5', accent: '#6a1b9a' },
    { key: 'sitePerformanceScore', label: 'Site Performance',  trendKey: 'sitePerformanceScoreChange', color: '#fff3e0', accent: '#e65100' },
  ];

  get canManageReports(): boolean {
    const r = (this.role || '').toUpperCase().replace(/[_\s]/g, '');
    return r === 'ADMIN' || r === 'SYSTEMADMIN' || r === 'DATAMANAGER';
  }

  get visibleAlerts(): KpiAlert[] {
    return (this.reportSummary?.alerts ?? []).filter(a => this.alertLevelLabel(a.level) !== 'Normal');
  }

  constructor(private reporting: ReportingService, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void { this.loadReports(); }

  loadReports(): void {
    this.reportsError = '';
    this.reportSummaryLoading = !this.reportSummary;
    this.reporting.getDashboard().pipe(timeout(8000)).subscribe({
      next: s => { this.reportSummary = s; this.reportSummaryLoading = false; this.cdr.detectChanges(); },
      error: () => { this.reportSummaryLoading = false; this.cdr.detectChanges(); }
    });
    this.reportsLoading = this.reports.length === 0;
    const list$ = this.reportScopeFilter === 'ALL'
      ? this.reporting.getReports()
      : this.reporting.getReportsByScope(this.reportScopeFilter);
    list$.pipe(timeout(8000)).subscribe({
      next: r => { this.reports = r; this.reportsLoading = false; this.cdr.detectChanges(); },
      error: err => {
        this.reportsLoading = false;
        this.reportsError = err.status === 0
          ? 'Cannot reach the Reporting service. Make sure it is running on port 5278.'
          : (err.error?.error || err.error?.message || 'Failed to load reports.');
        this.cdr.detectChanges();
      }
    });
  }

  onScopeFilterChange(scope: 'ALL' | KpiScopeName): void {
    this.reportScopeFilter = scope;
    this.reportsLoading = true;
    const list$ = scope === 'ALL' ? this.reporting.getReports() : this.reporting.getReportsByScope(scope);
    list$.pipe(timeout(8000)).subscribe({
      next: r => { this.reports = r; this.reportsLoading = false; this.cdr.detectChanges(); },
      error: () => { this.reportsLoading = false; this.cdr.detectChanges(); }
    });
  }

  deleteReport(report: KpiReport): void {
    if (!confirm(`Delete this ${this.scopeLabel(report.scope)} report?`)) return;
    this.reporting.deleteReport(report.reportId).subscribe({
      next: () => { this.reports = this.reports.filter(r => r.reportId !== report.reportId); this.loadReports(); this.cdr.detectChanges(); },
      error: err => {
        this.reportsError = err.status === 403 ? 'Only an Admin can delete reports.' : 'Could not delete report.';
        this.cdr.detectChanges();
      }
    });
  }

  downloadDashboardPdf(): void {
    this.pdfDownloading = true;
    this.reporting.downloadDashboardPdf()
      .pipe(finalize(() => { this.pdfDownloading = false; this.cdr.detectChanges(); }))
      .subscribe({
        next: blob => this.saveBlob(blob, `LifeSci360_Dashboard_${this.fileStamp()}.pdf`),
        error: () => { this.reportsError = 'Could not download dashboard PDF.'; this.cdr.detectChanges(); }
      });
  }

  downloadReportPdf(report: KpiReport): void {
    this.reporting.downloadReportPdf(report.reportId).subscribe({
      next: blob => this.saveBlob(blob, `KPI_Report_${report.scope}_${this.fileStamp()}.pdf`),
      error: () => { this.reportsError = 'Could not download report PDF.'; this.cdr.detectChanges(); }
    });
  }

  kpiValue(metrics: KpiMetrics | null | undefined, key: string): number {
    return metrics ? (metrics as any)[key] ?? 0 : 0;
  }
  trendValue(key: string): number {
    return this.reportSummary ? (this.reportSummary.trends as any)[key] ?? 0 : 0;
  }
  alertLevelLabel(level: number | string): string {
    return typeof level === 'string' ? level : (['Normal', 'Warning', 'Critical'][level] ?? 'Normal');
  }
  alertLevelClass(level: number | string): string { return this.alertLevelLabel(level).toLowerCase(); }
  scopeLabel(scope: string): string {
    const map: Record<string, string> = {
      'Enrollment': 'Enrollment', 'SampleProcessing': 'Sample Processing',
      'Compliance': 'Compliance', 'SitePerformance': 'Site Performance'
    };
    return map[scope] ?? scope;
  }

  openGenerateModal(): void {
    this.generateScope = 'Enrollment';
    this.generateProtocolId = '';
    this.generateError = '';
    this.showGenerateModal = true;
  }

  closeGenerateModal(): void { this.showGenerateModal = false; }

  submitGenerateReport(): void {
    this.generateLoading = true;
    this.generateError = '';
    const userId = localStorage.getItem('userId') ?? '';
    this.reporting.createReport({
      scope: this.scopeOptions.indexOf(this.generateScope),
      generatedByUserId: userId,
      protocolId: this.generateProtocolId.trim() || null
    }).subscribe({
      next: () => {
        this.generateLoading = false;
        this.showGenerateModal = false;
        this.loadReports();
        this.cdr.detectChanges();
      },
      error: err => {
        this.generateLoading = false;
        this.generateError = err.error?.error || err.error?.message || 'Failed to generate report.';
        this.cdr.detectChanges();
      }
    });
  }

  private saveBlob(blob: Blob, filename: string): void {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url; a.download = filename; a.click();
    URL.revokeObjectURL(url);
  }
  private fileStamp(): string { return new Date().toISOString().slice(0, 10).replace(/-/g, ''); }
}
