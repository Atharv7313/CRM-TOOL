import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class PatientService {
  private api = `${environment.patientApiUrl}/api/patients`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<any> {
    return this.http.get(this.api);
  }

  getById(id: string): Observable<any> {
    return this.http.get(`${this.api}/${id}`);
  }

  create(dto: { name: string; dateOfBirth: string; contactInfo: string }): Observable<any> {
    return this.http.post(this.api, dto);
  }

  update(id: string, dto: { name: string; dateOfBirth: string; contactInfo: string }): Observable<any> {
    return this.http.put(`${this.api}/${id}`, dto);
  }

  deactivate(id: string): Observable<any> {
    return this.http.put(`${this.api}/${id}/deactivate`, {});
  }
}
