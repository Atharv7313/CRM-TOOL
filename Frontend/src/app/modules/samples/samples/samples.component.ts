import { Component, OnInit, ChangeDetectorRef, Input } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, ValidationErrors, Validators } from '@angular/forms';
import { finalize, timeout } from 'rxjs';
import { SampleService, SampleListDto, SampleCreateDto, SampleUpdateDto } from '../../../services/sample.service';
import { EnrollmentService } from '../../../services/enrollment.service';
import { Enrollment } from '../../../models/enrollment';

// Validator 1 — collected date cannot be in the future
function noFutureDateValidator(control: AbstractControl): ValidationErrors | null {
  if (!control.value) return null;
  const selected = new Date(control.value);
  const now      = new Date();
  return selected > now ? { futureDate: true } : null;
}

@Component({
  selector: 'app-samples',
  standalone: false,
  templateUrl: './samples.component.html',
  styleUrl: './samples.component.css'
})
export class SamplesComponent implements OnInit {
  @Input() role = '';
  @Input() currentUserId = '';

  samples: SampleListDto[] = [];
  samplesLoading = false;
  samplesError = '';
  showLabResultsView = false;
  selectedSample: SampleListDto | null = null;

  sampleStatuses = ['COLLECTED', 'IN_PROCESS', 'TESTED', 'ANALYZED', 'REJECTED'];

  showCreatePanel = false;
  createForm!: FormGroup;
  createLoading = false;
  createError = '';
  createSuccess = '';

  enrollments: Enrollment[] = [];
  enrollmentsLoading = false;
  enrollmentSearch = '';
  enrollmentDropdownOpen = false;
  selectedEnrollmentLabel = '';

  showEditPanel = false;
  editForm!: FormGroup;
  editingSample: SampleListDto | null = null;
  editLoading = false;
  editError = '';
  editSuccess = '';

  showStatusPanel = false;
  statusForm!: FormGroup;
  statusSample: SampleListDto | null = null;
  statusLoading = false;
  statusError = '';
  statusSuccess = '';

  get isAdmin(): boolean { return this.role === 'ADMIN' || this.role === 'SYSTEM_ADMIN'; }
  get canCreate(): boolean  { return this.role === 'LAB_TECHNICIAN' || this.isAdmin; }
  get canEdit(): boolean    { return this.role === 'LAB_TECHNICIAN' || this.isAdmin; }
  get canUpdateStatus(): boolean { return this.role === 'LAB_TECHNICIAN' || this.role === 'RESEARCH_SCIENTIST' || this.isAdmin; }

  constructor(
    private sampleSvc: SampleService,
    private enrollmentSvc: EnrollmentService,
    private fb: FormBuilder,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.createForm = this.fb.group({
      enrollmentId:      ['', Validators.required],
      collectedByUserId: [this.currentUserId],
      sampleType:        ['', Validators.required],
      collectedDate:     ['', [Validators.required, noFutureDateValidator]]
    });
    this.editForm = this.fb.group({ sampleType: [''] });
    this.statusForm = this.fb.group({ status: ['', Validators.required] });
    this.loadSamples();
  }

  loadSamples(): void {
    this.samplesLoading = this.samples.length === 0;
    this.samplesError = '';
    this.sampleSvc.getAllSamples()
      .pipe(timeout(8000), finalize(() => { this.samplesLoading = false; this.cdr.detectChanges(); }))
      .subscribe({
        next: data => {
          this.samples = this.role === 'LAB_TECHNICIAN'
            ? data.filter(s => s.collectedByUserId === this.currentUserId)
            : data;
          this.cdr.detectChanges();
        },
        error: err => {
          this.samplesError = err?.name === 'TimeoutError'
            ? 'SampleService is not running. Start it on port 5025.'
            : (err?.error?.message || 'Failed to load samples. Check SampleService is running on port 5025.');
          this.cdr.detectChanges();
        }
      });
  }

  viewLabResults(sample: SampleListDto): void {
    this.selectedSample = sample;
    this.showLabResultsView = true;
  }

  backToSamples(): void {
    this.showLabResultsView = false;
    this.selectedSample = null;
  }

  openCreatePanel(): void {
    this.createForm.reset({ collectedByUserId: this.currentUserId });
    this.createError = '';
    this.createSuccess = '';
    this.enrollmentSearch = '';
    this.selectedEnrollmentLabel = '';
    this.enrollmentDropdownOpen = false;
    this.showCreatePanel = true;
    this.loadEnrollments();
  }
  closeCreatePanel(): void { this.showCreatePanel = false; this.enrollmentDropdownOpen = false; }

  loadEnrollments(): void {
    this.enrollmentsLoading = true;
    this.enrollmentSvc.getAll().subscribe({
      next: (res: any) => {
        this.enrollments = (res.data ?? res) as Enrollment[];
        this.enrollmentsLoading = false;
        this.cdr.detectChanges();
      },
      error: () => { this.enrollmentsLoading = false; this.cdr.detectChanges(); }
    });
  }

  get filteredEnrollments(): Enrollment[] {
    const q = this.enrollmentSearch.toLowerCase().trim();
    return this.enrollments.filter(e =>
      e.enrollmentStatus === 'ACTIVE' &&
      (!q ||
        e.patientName.toLowerCase().includes(q) ||
        (e.patientEmail ?? '').toLowerCase().includes(q) ||   // email search
        e.protocolTitle.toLowerCase().includes(q) ||
        e.siteName.toLowerCase().includes(q))
    );
  }

  selectEnrollment(e: Enrollment): void {
    this.createForm.patchValue({ enrollmentId: e.enrollmentId });
    this.selectedEnrollmentLabel = `${e.patientName} — ${e.protocolTitle}`;
    this.enrollmentSearch = '';
    this.enrollmentDropdownOpen = false;
    this.cdr.detectChanges();
  }

  clearEnrollment(): void {
    this.createForm.patchValue({ enrollmentId: '' });
    this.selectedEnrollmentLabel = '';
    this.enrollmentSearch = '';
    this.cdr.detectChanges();
  }

  submitCreate(): void {
    if (this.createForm.invalid) return;
    this.createLoading = true;
    this.createError = '';
    const v = this.createForm.value;
    const dto: SampleCreateDto = {
      enrollmentId:      v.enrollmentId,
      collectedByUserId: v.collectedByUserId || this.currentUserId,
      sampleType:        v.sampleType,
      collectedDate:     v.collectedDate  // send as local time — no UTC conversion
    };
    this.sampleSvc.createSample(dto)
      .pipe(finalize(() => { this.createLoading = false; this.cdr.detectChanges(); }))
      .subscribe({
        next: sample => {
          this.samples.unshift(sample);
          this.createSuccess = `Sample "${sample.sampleType}" created successfully.`;
          this.cdr.detectChanges();
          setTimeout(() => { this.showCreatePanel = false; this.createSuccess = ''; this.cdr.detectChanges(); }, 1800);
        },
        error: err => {
          this.createError = err.error?.message || 'Failed to create sample. Check Enrollment ID is valid.';
          this.cdr.detectChanges();
        }
      });
  }

  openEditPanel(sample: SampleListDto): void {
    this.editingSample = sample;
    this.editError = '';
    this.editSuccess = '';
    this.editForm.patchValue({ sampleType: sample.sampleType });
    this.showEditPanel = true;
  }
  closeEditPanel(): void { this.showEditPanel = false; this.editingSample = null; }

  submitEdit(): void {
    if (!this.editingSample) return;
    this.editLoading = true;
    this.editError = '';
    const dto: SampleUpdateDto = this.editForm.value;
    this.sampleSvc.updateSample(this.editingSample.sampleId, dto)
      .pipe(finalize(() => { this.editLoading = false; this.cdr.detectChanges(); }))
      .subscribe({
        next: updated => {
          const idx = this.samples.findIndex(s => s.sampleId === updated.sampleId);
          if (idx !== -1) this.samples[idx] = { ...updated };
          this.editSuccess = 'Sample updated successfully.';
          this.cdr.detectChanges();
          setTimeout(() => { this.showEditPanel = false; this.editingSample = null; this.editSuccess = ''; this.cdr.detectChanges(); }, 1800);
        },
        error: err => {
          this.editError = err.error?.message || 'Failed to update sample.';
          this.cdr.detectChanges();
        }
      });
  }

  openStatusPanel(sample: SampleListDto): void {
    this.statusSample = sample;
    this.statusError = '';
    this.statusSuccess = '';
    this.statusForm.setValue({ status: sample.status });
    this.showStatusPanel = true;
  }
  closeStatusPanel(): void { this.showStatusPanel = false; this.statusSample = null; }

  submitStatus(): void {
    if (!this.statusSample || this.statusForm.invalid) return;
    this.statusLoading = true;
    this.statusError = '';
    const status = this.statusForm.value.status;
    this.sampleSvc.updateSampleStatus(this.statusSample.sampleId, status)
      .pipe(finalize(() => { this.statusLoading = false; this.cdr.detectChanges(); }))
      .subscribe({
        next: () => {
          const idx = this.samples.findIndex(s => s.sampleId === this.statusSample?.sampleId);
          if (idx !== -1) this.samples[idx] = { ...this.samples[idx], status };
          // Also update selectedSample so notes card refreshes immediately
          if (this.selectedSample?.sampleId === this.statusSample?.sampleId)
            this.selectedSample = { ...this.selectedSample!, status } as typeof this.selectedSample;
          this.statusSuccess = `Status updated to "${status}".`;
          this.cdr.detectChanges();
          setTimeout(() => { this.showStatusPanel = false; this.statusSample = null; this.statusSuccess = ''; this.cdr.detectChanges(); }, 1800);
        },
        error: err => {
          this.statusError = err.error?.message || 'Failed to update status.';
          this.cdr.detectChanges();
        }
      });
  }

  deleteSample(id: string, sampleType: string): void {
    if (!confirm(`Delete sample "${sampleType}"? This will also remove all linked lab results.`)) return;
    this.sampleSvc.deleteSample(id).pipe(timeout(5000)).subscribe({
      next: () => { this.samples = this.samples.filter(s => s.sampleId !== id); this.cdr.detectChanges(); },
      error: () => { this.cdr.detectChanges(); }
    });
  }

  statusColor(status: string): string {
    const map: Record<string, string> = {
      'COLLECTED': '#dbeafe', 'IN_PROCESS': '#fef3c7',
      'TESTED':    '#dcfce7', 'ANALYZED':  '#e0f2fe', 'REJECTED': '#fee2e2'
    };
    return map[status] ?? '#f5f5f5';
  }

  statusTextColor(status: string): string {
    const map: Record<string, string> = {
      'COLLECTED': '#1d4ed8', 'IN_PROCESS': '#92400e',
      'TESTED':    '#15803d', 'ANALYZED':  '#0369a1', 'REJECTED': '#b91c1c'
    };
    return map[status] ?? '#555';
  }

  avatarFor(name: string): string { return name ? name.substring(0, 2).toUpperCase() : '??'; }
}
