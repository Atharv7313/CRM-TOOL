import { Component, OnInit, ChangeDetectorRef, Input } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { finalize, timeout } from 'rxjs';
import { AuthService, EnrolledUser, RoleOption, UpdateUserRequest } from '../../services/auth.service';

@Component({
  selector: 'app-users',
  standalone: false,
  templateUrl: './users.component.html',
  styleUrl: './users.component.css'
})
export class UsersComponent implements OnInit {
  @Input() role = '';

  users: EnrolledUser[] = [];
  roles: RoleOption[] = [];
  usersLoading = false;

  showEnrollPanel = false;
  enrollForm!: FormGroup;
  enrollLoading = false;
  enrollError = '';
  enrollSuccess = '';
  showEnrollPassword = false;

  showEditPanel = false;
  editForm!: FormGroup;
  editingUser: EnrolledUser | null = null;
  editLoading = false;
  editError = '';
  editSuccess = '';

  get isAdmin(): boolean { return this.role === 'ADMIN' || this.role === 'SYSTEM_ADMIN'; }

  constructor(private auth: AuthService, private fb: FormBuilder, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.enrollForm = this.fb.group({
      name:     ['', [Validators.required, Validators.minLength(2)]],
      email:    ['', [Validators.required, Validators.email]],
      phone:    ['', [Validators.pattern(/^\d{0,10}$/)]],
      password: ['', [Validators.required, Validators.minLength(8)]],
      roleName: ['', Validators.required]
    });
    this.editForm = this.fb.group({
      name:     ['', [Validators.required, Validators.minLength(2)]],
      phone:    ['', [Validators.pattern(/^\d{0,10}$/)]],
      roleName: ['', Validators.required],
      isActive: [true]
    });
    this.loadUsers();
  }

  loadUsers(): void {
    this.usersLoading = this.users.length === 0;
    this.auth.getUsers().pipe(timeout(5000)).subscribe({
      next: u => { this.users = u; this.usersLoading = false; this.cdr.detectChanges(); },
      error: () => { this.usersLoading = false; this.cdr.detectChanges(); }
    });
    if (this.roles.length === 0) {
      this.auth.getRoles().pipe(timeout(5000)).subscribe({
        next: r => { this.roles = r; this.cdr.detectChanges(); },
        error: () => {}
      });
    }
  }

  openEnrollPanel(): void {
    this.enrollForm.reset();
    this.enrollError = '';
    this.enrollSuccess = '';
    this.showEnrollPanel = true;
  }
  closeEnrollPanel(): void { this.showEnrollPanel = false; }

  submitEnroll(): void {
    if (this.enrollForm.invalid) return;
    this.enrollLoading = true;
    this.enrollError = '';
    this.enrollSuccess = '';
    this.auth.enrollUser(this.enrollForm.value).subscribe({
      next: newUser => {
        this.users.unshift(newUser);
        this.enrollSuccess = `"${newUser.name}" enrolled successfully.`;
        this.enrollLoading = false;
        this.enrollForm.reset();
        setTimeout(() => { this.showEnrollPanel = false; this.enrollSuccess = ''; this.cdr.detectChanges(); }, 2000);
      },
      error: err => {
        this.enrollError = err.error?.message || 'Enrollment failed. Please try again.';
        this.enrollLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  openEditPanel(user: EnrolledUser): void {
    if (!this.canEdit(user)) return;
    this.editingUser = user;
    this.editError = '';
    this.editSuccess = '';
    this.editForm.setValue({ name: user.name, phone: user.phone ?? '', roleName: user.role, isActive: user.isActive });
    if (this.roles.length === 0) this.auth.getRoles().subscribe({ next: r => this.roles = r });
    this.showEditPanel = true;
  }
  closeEditPanel(): void { this.showEditPanel = false; this.editingUser = null; }

  submitEdit(): void {
    if (this.editForm.invalid || !this.editingUser) return;
    this.editLoading = true;
    this.editError = '';
    this.editSuccess = '';
    const dto: UpdateUserRequest = this.editForm.value;
    this.auth.updateUser(this.editingUser.userId, dto)
      .pipe(finalize(() => { this.editLoading = false; this.cdr.detectChanges(); }))
      .subscribe({
        next: updated => {
          const idx = this.users.findIndex(u => u.userId === updated.userId);
          if (idx !== -1) this.users[idx] = { ...updated };
          this.editSuccess = `"${updated.name}" updated successfully.`;
          this.cdr.detectChanges();
          setTimeout(() => { this.showEditPanel = false; this.editingUser = null; this.editSuccess = ''; this.cdr.detectChanges(); }, 1800);
        },
        error: err => {
          this.editError = err.error?.message || 'Update failed. Please try again.';
          this.cdr.detectChanges();
        }
      });
  }

  canEdit(user: EnrolledUser): boolean { return user.role?.toUpperCase() !== 'SYSTEM_ADMIN'; }
  avatarFor(name: string): string { return name ? name.substring(0, 2).toUpperCase() : '??'; }
  onlyDigits(event: KeyboardEvent): boolean { return /\d/.test(event.key); }
}
