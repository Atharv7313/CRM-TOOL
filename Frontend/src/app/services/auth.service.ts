

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';   
import { Router } from '@angular/router';            
import { Observable, map, tap } from 'rxjs';             
import { environment } from '../../environments/environment'; 


export interface LoginRequest  { email: string; password: string; }

export interface LoginResponse {
  token: string;     
  userId: string;   
  name: string;     
  email: string;     
  role: string;     
  expiresIn: number; 
}

export interface EnrollUserRequest  { name: string; email: string; phone?: string; password: string; roleName: string; }

export interface UpdateUserRequest  { name: string; phone?: string; roleName: string; isActive: boolean; }

export interface EnrolledUser { userId: string; name: string; email: string; phone?: string; role: string; isActive: boolean; createdAt: string; }

export interface RoleOption { roleId: string; roleName: string; }

export interface DashboardStats {
  activeUsers: number;      // How many users are currently active
  totalEnrolled: number;    // Total users registered in the system
  activeSessions: number;   // How many users are currently logged in
  auditEventsToday: number; // How many actions were logged today
}

export interface ActiveSession {
  name: string; email: string; role: string; ipAddress?: string;
  loginTime: string; expiresAt: string; status: string;
}
export interface AuditLogEntry {
  id: number;
  actorUserId?: string;
  actorName: string;
  actorEmail?: string;
  action: string;
  serviceName: string;
  description?: string;
  entityId?: string;
  entityName?: string;
  ipAddress?: string;
  isSuccess: boolean;
  errorMessage?: string;
  createdAt: string;
}
@Injectable({ providedIn: 'root' })
export class AuthService {

  private apiUrl     = `${environment.apiUrl}/api/auth`;
  private auditApiUrl = `${environment.auditApiUrl}/api/auditlogs`;
  constructor(private http: HttpClient, private router: Router) {}
  login(credentials: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, credentials).pipe(
      tap(res => {
        localStorage.setItem('token',  res.token);   // JWT token (sent with every request via interceptor)
        localStorage.setItem('role',   res.role);    // e.g. "LAB_TECHNICIAN" — used for role-based UI
        localStorage.setItem('userId', res.userId);  // Used when creating samples/lab results
        localStorage.setItem('email',  res.email);   // Displayed in sidebar
        localStorage.setItem('name',   res.name);    // Displayed in greeting "Good evening, Priya Sharma!"
      })
    );
  }
  resetPassword(dto: { email: string; newPassword: string }): Observable<any> {
    return this.http.post(`${this.apiUrl}/reset-password`, dto);
  }
  logout(): Observable<any> {
    return this.http.post(`${this.apiUrl}/logout`, {})
      .pipe(tap(() => this.clearSession())); // After server confirms logout, clear localStorage
  }
  enrollUser(dto: EnrollUserRequest): Observable<EnrolledUser> {
    return this.http.post<EnrolledUser>(`${this.apiUrl}/enroll`, dto);
  }

  updateUser(userId: string, dto: UpdateUserRequest): Observable<EnrolledUser> {
    return this.http.put<EnrolledUser>(`${this.apiUrl}/users/${userId}`, dto);
  }
  getUsers():          Observable<EnrolledUser[]>   { return this.http.get<EnrolledUser[]>(`${this.apiUrl}/users`); }
  getRoles():          Observable<RoleOption[]>      { return this.http.get<RoleOption[]>(`${this.apiUrl}/roles`); }
  getDashboardStats(): Observable<DashboardStats>    { return this.http.get<DashboardStats>(`${this.apiUrl}/stats`); }
  getActiveSessions(): Observable<ActiveSession[]>   { return this.http.get<ActiveSession[]>(`${this.apiUrl}/sessions`); }
  getAuditLogs(): Observable<AuditLogEntry[]> {
    return this.http
      .get<{ data: AuditLogEntry[] }>(`${this.auditApiUrl}`)
      .pipe(map(r => r.data ?? []));
  }
  getToken():   string | null { return localStorage.getItem('token'); }  // JWT token — sent in every API request
  getRole():    string | null { return localStorage.getItem('role'); }   // User's role — used for role-based UI
  getEmail():   string | null { return localStorage.getItem('email'); }  // User's email — shown in sidebar
  getName():    string | null { return localStorage.getItem('name'); }   // User's full name — shown in greeting
  isLoggedIn(): boolean { return !!this.getToken(); }
  clearSession(): void {
    localStorage.clear();            // Wipes token, role, userId, email, name
    this.router.navigate(['/login']); // Sends user back to login page
  }
}
