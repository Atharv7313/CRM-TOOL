import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class VisitService {
  private api       = `${environment.patientApiUrl}/api/visits`;
  private reviewApi = `${environment.patientApiUrl}/api/visit-review`;

  constructor(private http: HttpClient) {}

  getByEnrollment(enrollmentId: string): Observable<any> {
    return this.http.get(`${this.api}/enrollment/${enrollmentId}`);
  }

  getFiltered(date?: string, protocolSiteId?: string, status?: string): Observable<any> {
    let params = new HttpParams();
    if (date)             params = params.set('date', date);
    if (protocolSiteId)   params = params.set('protocolSiteId', protocolSiteId);
    if (status)           params = params.set('status', status);
    return this.http.get(this.api, { params });
  }

  add(dto: { enrollmentId: string; visitName: string; visitDate: string }): Observable<any> {
    return this.http.post(this.api, dto);
  }

  reschedule(id: string, dto: { newDate: string }): Observable<any> {
    return this.http.put(`${this.api}/${id}/reschedule`, dto);
  }

  cancel(id: string): Observable<any> {
    return this.http.put(`${this.api}/${id}/cancel`, {});
  }

  bulkSchedule(dto: {
    protocolId: string;
    visits: { visitName: string; visitDate: string }[];
  }): Observable<any> {
    return this.http.post(`${this.api}/bulk-schedule`, dto);
  }

  submitReview(dto: {
    date: string;
    protocolSiteId?: string;
    attendedVisitIds: string[];
  }): Observable<any> {
    return this.http.post(`${this.reviewApi}/submit`, dto);
  }
}
