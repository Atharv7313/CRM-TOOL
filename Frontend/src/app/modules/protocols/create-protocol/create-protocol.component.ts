import { Component, OnInit, Output, EventEmitter, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { ProtocolService, CreateProtocolRequest } from '../../../services/protocol.service';

@Component({
  selector: 'app-create-protocol',
  standalone: false,
  templateUrl: './create-protocol.component.html',
  styleUrl: './create-protocol.component.css'
})
export class CreateProtocolComponent implements OnInit {
  @Output() navigate = new EventEmitter<string>();

  form!: FormGroup;
  loading    = false;
  error      = '';
  success    = '';
  minEndDate = '';

  phases = ['Phase 1', 'Phase 2', 'Phase 3', 'Phase 4'];

  constructor(
    private svc: ProtocolService,
    private fb:  FormBuilder,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      title:       ['', [Validators.required, Validators.maxLength(200)]],
      phase:       ['', [Validators.required]],
      description: ['', Validators.maxLength(4000)],
      startDate:   ['', Validators.required],
      endDate:     ['', Validators.required]
      // status removed — auto-computed by backend from dates
    }, { validators: this.dateRangeValidator });

    this.form.get('startDate')!.valueChanges.subscribe(value => {
      if (value) {
        const d = new Date(value);
        d.setDate(d.getDate() + 1);
        this.minEndDate = d.toISOString().substring(0, 10);
      } else {
        this.minEndDate = '';
      }
      // clear end date if it no longer satisfies the new minimum
      const endVal = this.form.get('endDate')?.value;
      if (endVal && endVal <= value) {
        this.form.get('endDate')!.setValue('');
      }
    });
  }

  private dateRangeValidator(g: FormGroup) {
    const start = g.get('startDate')?.value;
    const end   = g.get('endDate')?.value;
    if (start && end && new Date(end) <= new Date(start)) {
      return { dateRange: true };
    }
    return null;
  }

  get f() { return this.form.controls; }

  get today(): string {
    return new Date().toISOString().substring(0, 10);
  }

  cancel(): void { this.navigate.emit('protocols'); }

  clear(): void {
    this.form.reset({ title: '', phase: '', description: '', startDate: '', endDate: '' });
    this.error   = '';
    this.success = '';
  }

  submit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.loading = true;
    this.error   = '';
    this.success = '';

    const { title, phase, description, startDate, endDate } = this.form.value;
    const dto: CreateProtocolRequest = {
      title:       title.trim(),
      phase,
      description: description?.trim() || undefined,
      startDate,
      endDate
      // status NOT sent — backend computes it automatically from dates
    };

    this.svc.createProtocol(dto)
      .pipe(finalize(() => { this.loading = false; this.cdr.detectChanges(); }))
      .subscribe({
        next: () => {
          this.success = `Protocol "${dto.title}" created successfully!`;
          this.form.reset();
          setTimeout(() => { this.navigate.emit('protocols'); }, 2000);
        },
        error: err => {
          this.error = err.error?.message || (err.error?.errors?.join(', ')) || 'Failed to create protocol.';
        }
      });
  }
}
