import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

export interface ApiResponse<T> {
  isSuccess: boolean;
  message: string;
  data: T;
}

export interface SampleListDto {
  sampleId: string;
  enrollmentId: string;
  collectedByUserId: string;
  collectedByUserName: string;
  collectedByUserRole: string;
  sampleType: string;
  collectedDate: string;
  status: string;
  notes?: string;
  patientName?: string;
  patientEmail?: string;
  protocolTitle?: string;
}

export interface SampleCreateDto {
  enrollmentId: string;
  collectedByUserId: string;
  sampleType: string;
  collectedDate: string;
}

export interface SampleUpdateDto {
  sampleType?: string;
  status?: string;
  notes?: string;
}

export interface LabResultListDto {
  resultId: string;
  sampleId: string;
  recordedByUserId: string;
  recordedByUserName: string;
  testType: string;
  resultValue: string;
  resultStatus?: string;  // optional — Normal/Abnormal/Critical etc.
  resultDate: string;
}

export interface LabResultCreateDto {
  sampleId: string;
  recordedByUserId: string;
  testType: string;
  resultValue: string;
  resultStatus?: string;  // optional
  resultDate: string;
}

export interface LabResultUpdateDto {
  testType?: string;
  resultValue?: string;
  resultStatus?: string;  // optional
  resultDate?: string;
}

@Injectable({ providedIn: 'root' })
export class SampleService {
  private samplesUrl    = 'http://localhost:5025/api/samples';
  private labResultsUrl = 'http://localhost:5025/api/labresults';

  constructor(private http: HttpClient) {}

  // ── Samples ──────────────────────────────────────────────
  getAllSamples(): Observable<SampleListDto[]> {
    return this.http.get<ApiResponse<SampleListDto[]>>(this.samplesUrl)
      .pipe(map(r => r.data ?? []));
  }

  getSampleById(id: string): Observable<SampleListDto> {
    return this.http.get<ApiResponse<SampleListDto>>(`${this.samplesUrl}/${id}`)
      .pipe(map(r => r.data));
  }

  getSamplesByEnrollment(enrollmentId: string): Observable<SampleListDto[]> {
    return this.http.get<ApiResponse<SampleListDto[]>>(`${this.samplesUrl}/enrollment/${enrollmentId}`)
      .pipe(map(r => r.data ?? []));
  }

  createSample(dto: SampleCreateDto): Observable<SampleListDto> {
    return this.http.post<ApiResponse<SampleListDto>>(this.samplesUrl, dto) //sends post req to backend
      .pipe(map(r => r.data));
  }

  updateSample(id: string, dto: SampleUpdateDto): Observable<SampleListDto> {
    return this.http.put<ApiResponse<SampleListDto>>(`${this.samplesUrl}/${id}`, dto)
      .pipe(map(r => r.data));//obs-api call that takes time,sub-wait for response 
  }

  updateSampleStatus(id: string, status: string): Observable<string> {
    const headers = new HttpHeaders({ 'Content-Type': 'application/json' });
    return this.http.put<ApiResponse<string>>(
      `${this.samplesUrl}/${id}/status`,
      JSON.stringify(status),
      { headers }
    ).pipe(map(r => r.data));
  }

  deleteSample(id: string): Observable<void> {
    return this.http.delete<any>(`${this.samplesUrl}/${id}`)
      .pipe(map(() => void 0));
  }

  // ── Lab Results ──────────────────────────────────────────
  getLabResultsBySample(sampleId: string): Observable<LabResultListDto[]> {
    return this.http.get<ApiResponse<LabResultListDto[]>>(`${this.labResultsUrl}/sample/${sampleId}`)
      .pipe(map(r => r.data ?? []));
  }

  getLabResultById(id: string): Observable<LabResultListDto> {
    return this.http.get<ApiResponse<LabResultListDto>>(`${this.labResultsUrl}/${id}`)
      .pipe(map(r => r.data));
  }

  createLabResult(dto: LabResultCreateDto): Observable<LabResultListDto> {
    return this.http.post<ApiResponse<LabResultListDto>>(this.labResultsUrl, dto)
      .pipe(map(r => r.data));
  }

  updateLabResult(id: string, dto: LabResultUpdateDto): Observable<LabResultListDto> {
    return this.http.put<ApiResponse<LabResultListDto>>(`${this.labResultsUrl}/${id}`, dto)
      .pipe(map(r => r.data));
  }

  deleteLabResult(id: string): Observable<void> {
    return this.http.delete<any>(`${this.labResultsUrl}/${id}`)
      .pipe(map(() => void 0));
  }
}
