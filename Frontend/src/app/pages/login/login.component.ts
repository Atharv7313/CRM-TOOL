import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, ValidationErrors, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { firstValueFrom } from 'rxjs';

export interface RecentUser {
  name: string;
  email: string;
  role: string;
  initials: string;
  color: string;
  text: string;
}

@Component({
  selector: 'app-login',
  standalone: false,
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent implements OnInit {
  loginForm: FormGroup;
  errorMessage = '';
  loading = false;
  showPassword = false;
  recentUsers: RecentUser[] = [];

  // Forgot password modal
  showForgotModal = false;
  forgotForm: FormGroup;
  forgotLoading = false;
  forgotError = '';
  forgotSuccess = false;
  showNewPassword = false;
  showConfirmPassword = false;

  private readonly STORAGE_KEY = 'ls360_recent_users';

  private roleStyles: Record<string, { color: string; text: string }> = {
    ADMIN: { color: '#e8f5e9', text: '#2e7d32' },
    RESEARCHER: { color: '#e3f2fd', text: '#1565c0' },
    LAB_TECH: { color: '#fff3e0', text: '#e65100' },
    CLINICAL_TRIAL: { color: '#f3e5f5', text: '#6a1b9a' },
    DATA_MANAGER: { color: '#fce4ec', text: '#880e4f' },
  };

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', Validators.required],
      rememberMe: [false]
    });

    this.forgotForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      newPassword: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', Validators.required]
    }, { validators: this.passwordMatchValidator });
  }

  ngOnInit(): void {
    if (this.auth.isLoggedIn()) {
      this.router.navigate(['/dashboard'], { replaceUrl: true });
      return;
    }
    this.recentUsers = this.loadRecentUsers();
  }

  get email() { return this.loginForm.get('email'); }
  get password() { return this.loginForm.get('password'); }

  togglePassword(): void { this.showPassword = !this.showPassword; }

  selectRecentUser(user: RecentUser): void {
    this.loginForm.patchValue({ email: user.email });
    // focus password field
    setTimeout(() => {
      const pw = document.getElementById('password');
      if (pw) pw.focus();
    }, 50);
  }

  removeRecentUser(email: string, event: Event): void {
    event.stopPropagation();
    this.recentUsers = this.recentUsers.filter(u => u.email !== email);
    localStorage.setItem(this.STORAGE_KEY, JSON.stringify(this.recentUsers));
  }

  onSubmit(): void {
    if (this.loginForm.invalid) return;
    this.loading = true;
    this.errorMessage = '';

    this.auth.login({
      email: this.loginForm.value.email,
      password: this.loginForm.value.password
    }).subscribe({
      next: (res) => {
        this.saveRecentUser(res);
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Invalid email or password.';
        this.loading = false;
      }
    });
  }

  private saveRecentUser(res: any): void {
    const style = this.roleStyles[res.role] ?? { color: '#f5f5f5', text: '#333' };
    const email: string = res.email ?? this.loginForm.value.email;
    const name: string = res.name ?? res.email.split('@')[0];
    const entry: RecentUser = {
      name,
      email,
      role: res.role??'',
      initials: name.substring(0, 2).toUpperCase(),
      color: style.color,
      text: style.text
    };
    // keep unique by email, most recent first, max 5
    let list: RecentUser[] = this.loadRecentUsers().filter(u => u.email !== entry.email);
    list.unshift(entry);
    list = list.slice(0, 5);
    localStorage.setItem(this.STORAGE_KEY, JSON.stringify(list));
  }

  private loadRecentUsers(): RecentUser[] {
    try {
      const raw = localStorage.getItem(this.STORAGE_KEY);
      return raw ? JSON.parse(raw) : [];
    } catch {
      return [];
    }
  }

  // ── Forgot password modal ──────────────────────────────────
  private passwordMatchValidator(group: AbstractControl): ValidationErrors | null {
    const pw  = group.get('newPassword')?.value;
    const cpw = group.get('confirmPassword')?.value;
    return pw === cpw ? null : { passwordMismatch: true };
  }

  get forgotEmail()    { return this.forgotForm.get('email'); }
  get newPassword()    { return this.forgotForm.get('newPassword'); }
  get confirmPassword(){ return this.forgotForm.get('confirmPassword'); }

  openForgotModal(): void {
    this.forgotForm.reset();
    this.forgotError   = '';
    this.forgotSuccess = false;
    this.showForgotModal = true;
  }

  closeForgotModal(): void {
    this.showForgotModal = false;
  }

  async submitForgotPassword(): Promise<void> {
    if (this.forgotForm.invalid) { this.forgotForm.markAllAsTouched(); return; }
    this.forgotLoading = true;
    this.forgotError   = '';

    try {
      await firstValueFrom(this.auth.resetPassword({
        email:       this.forgotForm.value.email,
        newPassword: this.forgotForm.value.newPassword
      }));
      this.forgotSuccess = true;
    } catch (err: any) {
      this.forgotError = err.error?.message || 'Something went wrong. Please try again.';
    } finally {
      this.forgotLoading = false;
      this.cdr.detectChanges();
    }
  }
}
