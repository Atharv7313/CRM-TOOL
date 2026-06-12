import { Component, OnInit, Output, EventEmitter, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { ProtocolService, CreateSiteRequest } from '../../../services/protocol.service';

@Component({
  selector: 'app-create-site',
  standalone: false,
  templateUrl: './create-site.component.html',
  styleUrl: './create-site.component.css'
})
export class CreateSiteComponent implements OnInit {
  @Output() navigate = new EventEmitter<string>();

  form!: FormGroup;
  loading = false;
  error   = '';
  success = '';

  constructor(
    private svc: ProtocolService,
    private fb:  FormBuilder,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      name:     ['', [Validators.required, Validators.maxLength(150)]],
      location: ['', [Validators.required, Validators.maxLength(255)]]
    });
  }

  get f() { return this.form.controls; }

  cancel(): void { this.navigate.emit('sites'); }

  submit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.loading = true;
    this.error   = '';
    this.success = '';

    const dto: CreateSiteRequest = {
      name:     this.form.value.name.trim(),
      location: this.form.value.location?.trim() || undefined
    };

    this.svc.createSite(dto)
      .pipe(finalize(() => { this.loading = false; this.cdr.detectChanges(); }))
      .subscribe({
        next: () => {
          this.success = `Site "${dto.name}" registered successfully!`;
          this.form.reset();
          setTimeout(() => { this.navigate.emit('sites'); }, 2000);
        },
        error: err => {
          this.error = err.error?.message || (err.error?.errors?.join(', ')) || 'Failed to register site.';
        }
      });
  }
}
