import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class EnrollmentService {
  private api = `${environment.patientApiUrl}/api/enrollments`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<any> {
    return this.http.get(this.api);
  }

  getById(id: string): Observable<any> {
    return this.http.get(`${this.api}/${id}`);
  }

  getProtocols(): Observable<any> {
    return this.http.get(`${this.api}/protocols`);
  }

  getSitesByProtocol(protocolId: string): Observable<any> {
    return this.http.get(`${this.api}/protocols/${protocolId}/sites`);
  }

  getActivePatientCount(protocolId: string): Observable<any> {
    return this.http.get(`${this.api}/protocols/${protocolId}/active-count`);
  }

  enroll(dto: { patientId: string; protocolSiteId: string }): Observable<any> {
    return this.http.post(this.api, dto);
  }

  withdraw(id: string): Observable<any> {
    return this.http.put(`${this.api}/${id}/withdraw`, {});
  }
}
