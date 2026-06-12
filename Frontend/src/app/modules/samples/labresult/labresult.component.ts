import { Component, OnInit, OnChanges, Input, Output, EventEmitter, ChangeDetectorRef } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, ValidationErrors, Validators } from '@angular/forms';
import { finalize, timeout } from 'rxjs';
import { SampleService, SampleListDto, LabResultListDto, LabResultCreateDto, LabResultUpdateDto } from '../../../services/sample.service';

// Validator 1 — result date cannot be in the future
function noFutureDateValidator(control: AbstractControl): ValidationErrors | null {
  if (!control.value) return null;
  const selected = new Date(control.value);
  const now      = new Date();
  return selected > now ? { futureDate: true } : null;
}

// Validator 2 — result date cannot be before sample collection date
function notBeforeCollectionDate(collectedDate: string) {
  return (control: AbstractControl): ValidationErrors | null => {
    if (!control.value || !collectedDate) return null;
    const resultDate   = new Date(control.value);
    const sampleDate   = new Date(collectedDate);
    // compare dates only (strip time)
    const rDate = new Date(resultDate.getFullYear(), resultDate.getMonth(), resultDate.getDate());
    const sDate = new Date(sampleDate.getFullYear(), sampleDate.getMonth(), sampleDate.getDate());
    return rDate < sDate ? { beforeCollection: true } : null;
  };
}

@Component({
  selector: 'app-labresult',
  standalone: false,
  templateUrl: './labresult.component.html',
  styleUrl: './labresult.component.css'
})
export class LabResultComponent implements OnInit, OnChanges {
  @Input() sample!: SampleListDto;
  @Input() role = '';
  @Input() currentUserId = '';
  @Output() back = new EventEmitter<void>();

  labResults: LabResultListDto[] = [];
  labResultsLoading = false;
  labResultsError = '';

  showCreatePanel = false;
  createForm!: FormGroup;
  createLoading = false;
  createError = '';
  createSuccess = '';

  showEditPanel = false;
  editForm!: FormGroup;
  editingResult: LabResultListDto | null = null;
  editLoading = false;
  editError = '';
  editSuccess = '';

  noteEditId = '';
  noteDraft = '';

  get isAdmin(): boolean { return this.role === 'ADMIN' || this.role === 'SYSTEM_ADMIN'; }
  get canManage(): boolean { return this.role === 'LAB_TECHNICIAN' || this.isAdmin; }

  constructor(private sampleSvc: SampleService, private fb: FormBuilder, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    const collectedDate = this.sample?.collectedDate ?? '';
    this.createForm = this.fb.group({
      sampleId:         [this.sample?.sampleId ?? ''],
      recordedByUserId: [this.currentUserId],
      testType:         ['', Validators.required],
      resultValue:      ['', Validators.required],
      resultStatus:     [''],   // optional — Normal, Abnormal-High etc.
      resultDate:       [
        new Date().toISOString().split('T')[0],
        [Validators.required, noFutureDateValidator, notBeforeCollectionDate(collectedDate)]
      ]
    });
    this.editForm = this.fb.group({
      testType:     [''],
      resultValue:  [''],
      resultStatus: [''],
      resultDate:   ['']
    });
    if (this.sample) this.loadLabResults();
  }

  ngOnChanges(): void {
    if (this.sample && this.createForm) {
      this.createForm.patchValue({ sampleId: this.sample.sampleId });
      // update resultDate validator with new sample's collection date
      const collectedDate = this.sample.collectedDate ?? '';
      this.createForm.get('resultDate')?.setValidators([
        Validators.required,
        noFutureDateValidator,
        notBeforeCollectionDate(collectedDate)
      ]);
      this.createForm.get('resultDate')?.updateValueAndValidity();
      this.loadLabResults();
    }
  }

  loadLabResults(): void {
    this.labResultsLoading = true;
    this.labResultsError = '';
    this.sampleSvc.getLabResultsBySample(this.sample.sampleId)
      .pipe(timeout(8000), finalize(() => { this.labResultsLoading = false; this.cdr.detectChanges(); }))
      .subscribe({
        next: data => {
          this.labResults = this.role === 'LAB_TECHNICIAN'
            ? data.filter(r => r.recordedByUserId === this.currentUserId)
            : data;
          this.cdr.detectChanges();
        },
        error: err => {
          this.labResultsError = err?.name === 'TimeoutError'
            ? 'SampleService is taking too long. Please restart it, then try again.'
            : (err?.error?.message || 'Failed to load lab results.');
          this.cdr.detectChanges();
        }
      });
  }

  openCreatePanel(): void {
    this.createForm.reset({
      sampleId: this.sample.sampleId,
      recordedByUserId: this.currentUserId,
      resultDate: new Date().toISOString().split('T')[0]
    });
    this.createError = '';
    this.createSuccess = '';
    this.showCreatePanel = true;
  }
  closeCreatePanel(): void { this.showCreatePanel = false; }

  submitCreate(): void {
    if (this.createForm.invalid) return;
    this.createLoading = true;
    this.createError = '';
    const v = this.createForm.value;
    const dto: LabResultCreateDto = {
      sampleId:         v.sampleId,
      recordedByUserId: v.recordedByUserId || this.currentUserId,
      testType:         v.testType,
      resultValue:      v.resultValue,
      resultStatus:     v.resultStatus   || undefined,
      resultDate:       v.resultDate     // send as local date — no UTC conversion
    };
    this.sampleSvc.createLabResult(dto)
      .pipe(finalize(() => { this.createLoading = false; this.cdr.detectChanges(); }))
      .subscribe({
        next: result => {
          this.labResults.unshift(result);
          this.createSuccess = `Lab result "${result.testType}" recorded successfully.`;
          this.cdr.detectChanges();
          setTimeout(() => { this.showCreatePanel = false; this.createSuccess = ''; this.cdr.detectChanges(); }, 1800);
        },
        error: err => {
          this.createError = err.error?.message || 'Failed to record lab result.';
          this.cdr.detectChanges();
        }
      });
  }

  openEditPanel(result: LabResultListDto): void {
    this.editingResult = result;
    this.editError = '';
    this.editSuccess = '';
    this.editForm.setValue({
      testType:     result.testType,
      resultValue:  result.resultValue,
      resultStatus: result.resultStatus ?? '',
      resultDate:   result.resultDate ? result.resultDate.split('T')[0] : ''
    });
    this.showEditPanel = true;
  }
  closeEditPanel(): void { this.showEditPanel = false; this.editingResult = null; }

  submitEdit(): void {
    if (!this.editingResult) return;
    this.editLoading = true;
    this.editError = '';
    const v = this.editForm.value;
    const dto: LabResultUpdateDto = {
      testType:    v.testType    || undefined,
      resultValue: v.resultValue || undefined,
      resultDate:  v.resultDate  || undefined  // send as local date — no UTC conversion
    };
    this.sampleSvc.updateLabResult(this.editingResult.resultId, dto)
      .pipe(finalize(() => { this.editLoading = false; this.cdr.detectChanges(); }))
      .subscribe({
        next: updated => {
          const idx = this.labResults.findIndex(r => r.resultId === updated.resultId);
          if (idx !== -1) this.labResults[idx] = { ...updated };
          this.editSuccess = 'Lab result updated successfully.';
          this.cdr.detectChanges();
          setTimeout(() => { this.showEditPanel = false; this.editingResult = null; this.editSuccess = ''; this.cdr.detectChanges(); }, 1800);
        },
        error: err => {
          this.editError = err.error?.message || 'Failed to update lab result.';
          this.cdr.detectChanges();
        }
      });
  }

  deleteResult(id: string, testType: string): void {
    if (!confirm(`Delete lab result "${testType}"?`)) return;
    this.sampleSvc.deleteLabResult(id).subscribe({
      next: () => { this.labResults = this.labResults.filter(r => r.resultId !== id); this.cdr.detectChanges(); },
      error: () => { this.cdr.detectChanges(); }
    });
  }

  openNoteEdit(sampleId: string): void {
    this.noteEditId = sampleId;
    this.noteDraft = this.sample?.notes ?? '';
    this.cdr.detectChanges();
  }

  saveNote(sampleId: string): void {
    const text = this.noteDraft.trim();
    this.sampleSvc.updateSample(sampleId, { notes: text }).subscribe({
      next: updated => {
        if (this.sample?.sampleId === sampleId) this.sample = { ...this.sample, notes: text };
        this.noteEditId = '';
        this.cdr.detectChanges();
      },
      error: () => { this.noteEditId = ''; this.cdr.detectChanges(); }
    });
  }

  clearNote(sampleId: string): void {
    this.sampleSvc.updateSample(sampleId, { notes: '' }).subscribe({
      next: () => {
        if (this.sample?.sampleId === sampleId) this.sample = { ...this.sample, notes: '' };
        this.noteEditId = '';
        this.cdr.detectChanges();
      },
      error: () => { this.cdr.detectChanges(); }
    });
  }

  avatarFor(name: string): string { return name ? name.substring(0, 2).toUpperCase() : '??'; }

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
}
