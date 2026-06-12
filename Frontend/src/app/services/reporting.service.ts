import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

// ── Models (mirror Shared.DTOs / KPIReportDto.cs) ──────────────────────────────

export interface KpiMetrics {
  enrollmentRate: number;
  sampleProcessingRate: number;
  complianceScore: number;
  sitePerformanceScore: number;
}

export interface KpiTrend {
  enrollmentRateChange: number;
  sampleProcessingRateChange: number;
  complianceScoreChange: number;
  sitePerformanceScoreChange: number;
}

export interface ScopeCount { scope: string; count: number; }

// Backend enum KpiAlertLevel { Normal=0, Warning=1, Critical=2 }.
// System.Text.Json serializes it as a number by default, but tolerate strings too.
export type KpiAlertLevel = number | string;

export interface KpiAlert {
  kpiName: string;
  currentValue: number;
  level: KpiAlertLevel;
  message: string;
}

export interface KpiReport {
  reportId: string;
  protocolId?: string | null;
  protocolTitle?: string | null;
  generatedByUserId: string;
  generatedByUserName: string;
  scope: string;
  parsedMetrics?: KpiMetrics | null;
  rawMetrics?: string | null;
  generatedAt: string;
}

export interface DashboardSummary {
  currentKpis: KpiMetrics;
  previousPeriodKpis: KpiMetrics;
  trends: KpiTrend;
  reportsByScope: ScopeCount[];
  totalReports: number;
  activeProtocols: number;
  totalEnrolledPatients: number;
  totalSamplesProcessed: number;
  alerts: KpiAlert[];
  lastRefreshed: string;
}

// KpiScope enum order — index doubles as the numeric value the API accepts on POST.
export const KPI_SCOPES = ['Enrollment', 'SampleProcessing', 'Compliance', 'SitePerformance'] as const;
export type KpiScopeName = typeof KPI_SCOPES[number];

export interface CreateKpiReportRequest {
  protocolId?: string | null;
  generatedByUserId: string;
  scope: number; // send numeric enum value for System.Text.Json compatibility
}

@Injectable({ providedIn: 'root' })
export class ReportingService {
  private apiUrl = `${environment.reportingUrl}/api/reporting`;

  constructor(private http: HttpClient) {}

  getDashboard(): Observable<DashboardSummary> {
    return this.http.get<DashboardSummary>(`${this.apiUrl}/dashboard`);
  }

  getReports(): Observable<KpiReport[]> {
    return this.http.get<KpiReport[]>(`${this.apiUrl}/reports`);
  }

  getReportsByScope(scope: KpiScopeName): Observable<KpiReport[]> {
    return this.http.get<KpiReport[]>(`${this.apiUrl}/reports/scope/${scope}`);
  }

  createReport(dto: CreateKpiReportRequest): Observable<KpiReport> {
    return this.http.post<KpiReport>(`${this.apiUrl}/reports`, dto);
  }

  deleteReport(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/reports/${id}`);
  }

  downloadDashboardPdf(): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/pdf/dashboard`, { responseType: 'blob' });
  }

  downloadReportPdf(id: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/pdf/report/${id}`, { responseType: 'blob' });
  }
}
