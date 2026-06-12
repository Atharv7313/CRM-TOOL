import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../environments/environment';

// ── Response Models ────────────────────────────────────────────────────────
export interface InvestigatorResponse {
  userId: string;
  name:   string;
  email:  string;
  role:   string;
}

export interface ProtocolResponse {
  protocolId:       string;
  title:            string;
  phase:            string;
  status:           string;
  suggestedStatus?: string;
  description?:     string;
  startDate?:       string;
  endDate?:         string;
  createdByUserId:  string;
  siteCount:        number;
}

export interface SiteResponse {
  siteId:        string;
  name:          string;
  location?:     string;
  protocolCount: number;
}

export interface ProtocolSiteResponse {
  protocolSiteId:    string;
  protocolId:        string;
  protocolTitle:     string;
  siteId:            string;
  siteName:          string;
  siteLocation?:     string;
  investigatorUserId: string;
  investigatorName:  string;
  status:            string;
}

// ── Request Models ─────────────────────────────────────────────────────────
export interface CreateProtocolRequest {
  title:        string;
  phase:        string;
  description?: string;
  startDate:    string;
  endDate:      string;
  // status intentionally removed — auto-computed by backend from dates
}

export interface UpdateProtocolRequest {
  title:        string;
  phase:        string;
  description?: string;
  startDate:    string;
  endDate:      string;
}

export interface CreateSiteRequest {
  name:      string;
  location?: string;
}

export interface UpdateSiteRequest {
  name:      string;
  location?: string;
}

export interface AssignSiteRequest {
  siteId:             string;
  investigatorUserId: string;
}

// ── Generic API Envelope ───────────────────────────────────────────────────
// NSwag serializes C# "bool Success" → JSON "isSuccess" (is + camelCase)
interface ApiEnvelope<T> {
  isSuccess: boolean;
  message:   string;
  data?:     T;
  errors?:   string[];
}

@Injectable({ providedIn: 'root' })
export class ProtocolService {
  private base = `${environment.protocolApiUrl}/api`;

  constructor(private http: HttpClient) {}

  // ── Protocols ────────────────────────────────────────────────────────────

  getProtocols(status?: string, phase?: string, title?: string): Observable<ProtocolResponse[]> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    if (phase)  params = params.set('phase',  phase);
    if (title)  params = params.set('title',  title);
    return this.http
      .get<ApiEnvelope<ProtocolResponse[]>>(`${this.base}/protocols`, { params })
      .pipe(map(r => r.data ?? []));
  }

  getProtocol(id: string): Observable<ProtocolResponse> {
    return this.http
      .get<ApiEnvelope<ProtocolResponse>>(`${this.base}/protocols/${id}`)
      .pipe(map(r => r.data!));
  }

  createProtocol(dto: CreateProtocolRequest): Observable<void> {
    return this.http
      .post<ApiEnvelope<void>>(`${this.base}/protocols`, dto)
      .pipe(map(() => void 0));
  }

  updateProtocol(id: string, dto: UpdateProtocolRequest): Observable<void> {
    return this.http
      .put<ApiEnvelope<void>>(`${this.base}/protocols/${id}`, dto)
      .pipe(map(() => void 0));
  }

  updateProtocolStatus(id: string, status: string): Observable<void> {
    return this.http
      .patch<ApiEnvelope<void>>(`${this.base}/protocols/${id}/status`, { status })
      .pipe(map(() => void 0));
  }

  deleteProtocol(id: string): Observable<void> {
    return this.http
      .delete<ApiEnvelope<void>>(`${this.base}/protocols/${id}`)
      .pipe(map(() => void 0));
  }

  // ── Protocol Sites ────────────────────────────────────────────────────────

  getProtocolSites(protocolId: string): Observable<ProtocolSiteResponse[]> {
    return this.http
      .get<ApiEnvelope<ProtocolSiteResponse[]>>(`${this.base}/protocols/${protocolId}/sites`)
      .pipe(map(r => r.data ?? []));
  }

  assignSite(protocolId: string, dto: AssignSiteRequest): Observable<void> {
    return this.http
      .post<ApiEnvelope<void>>(`${this.base}/protocols/${protocolId}/sites`, dto)
      .pipe(map(() => void 0));
  }

  removeAssignment(protocolId: string, assignmentId: string): Observable<void> {
    return this.http
      .delete<ApiEnvelope<void>>(`${this.base}/protocols/${protocolId}/sites/${assignmentId}`)
      .pipe(map(() => void 0));
  }

  getSiteProtocols(siteId: string): Observable<ProtocolSiteResponse[]> {
    return this.http
      .get<ApiEnvelope<ProtocolSiteResponse[]>>(`${this.base}/sites/${siteId}/protocols`)
      .pipe(map(r => r.data ?? []));
  }

  // ── Investigators ─────────────────────────────────────────────────────────

  getInvestigators(): Observable<InvestigatorResponse[]> {
    return this.http
      .get<ApiEnvelope<InvestigatorResponse[]>>(`${this.base}/investigators`)
      .pipe(map(r => r.data ?? []));
  }

  // ── Sites ─────────────────────────────────────────────────────────────────

  getSites(name?: string, location?: string): Observable<SiteResponse[]> {
    let params = new HttpParams();
    if (name)     params = params.set('name',     name);
    if (location) params = params.set('location', location);
    return this.http
      .get<ApiEnvelope<SiteResponse[]>>(`${this.base}/sites`, { params })
      .pipe(map(r => r.data ?? []));
  }

  getSite(id: string): Observable<SiteResponse> {
    return this.http
      .get<ApiEnvelope<SiteResponse>>(`${this.base}/sites/${id}`)
      .pipe(map(r => r.data!));
  }

  createSite(dto: CreateSiteRequest): Observable<void> {
    return this.http
      .post<ApiEnvelope<void>>(`${this.base}/sites`, dto)
      .pipe(map(() => void 0));
  }

  updateSite(id: string, dto: UpdateSiteRequest): Observable<void> {
    return this.http
      .put<ApiEnvelope<void>>(`${this.base}/sites/${id}`, dto)
      .pipe(map(() => void 0));
  }

  deleteSite(id: string): Observable<void> {
    return this.http
      .delete<ApiEnvelope<void>>(`${this.base}/sites/${id}`)
      .pipe(map(() => void 0));
  }
}
