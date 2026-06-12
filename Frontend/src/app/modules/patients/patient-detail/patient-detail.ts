import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { PatientService } from '../../../services/patient.service';
import { EnrollmentService } from '../../../services/enrollment.service';
import { Patient } from '../../../models/patient';
import { Enrollment } from '../../../models/enrollment';
import { TrialsNavService } from '../../../services/trials-nav.service';

@Component({
  selector: 'app-patient-detail',
  standalone: false,
  templateUrl: './patient-detail.html',
  styleUrls: ['./patient-detail.css']
})
export class PatientDetail implements OnInit {

  patient: Patient | null = null;
  enrollments: Enrollment[] = [];
  loading = true;

  showDeactivateModal = false;

  toastVisible = false;
  toastMessage = '';
  toastType    = 'green';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private patientService: PatientService,
    private enrollmentService: EnrollmentService,
    private cdr: ChangeDetectorRef,
    private trialsNav: TrialsNavService
  ) {}

  goBack() {
    this.trialsNav.setPending('patients-list');
    this.router.navigate(['/dashboard']);
  }

  ngOnInit() {
    const patientId = this.route.snapshot.paramMap.get('patientId') || '';
    this.loadPatient(patientId);
    this.loadEnrollments(patientId);
  }

  loadPatient(patientId: string) {
    this.patientService.getById(patientId).subscribe({
      next: (res: any) => {
        this.patient = res.data;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (err: any) => {
        console.error(err);
        this.loading = false;
      }
    });
  }

  loadEnrollments(patientId: string) {
    this.enrollmentService.getAll().subscribe({
      next: (res: any) => {
        this.enrollments = res.data.filter(
          (e: Enrollment) => e.patientId === patientId
        ).sort((a: Enrollment, b: Enrollment) =>
          new Date(b.enrolledAt).getTime() - new Date(a.enrolledAt).getTime()
        );
        this.cdr.detectChanges();
      },
      error: (err: any) => console.error(err)
    });
  }

  openDeactivateModal() {
    this.showDeactivateModal = true;
    this.cdr.detectChanges();
  }

  closeDeactivateModal() {
    this.showDeactivateModal = false;
    this.cdr.detectChanges();
  }

  confirmDeactivate() {
    if (!this.patient) return;
    this.patientService.deactivate(this.patient.patientId)
      .subscribe({
        next: () => {
          this.showDeactivateModal = false;
          this.showToast(`${this.patient?.name} deactivated successfully`, 'green');
          this.loadPatient(this.patient!.patientId);
          this.loadEnrollments(this.patient!.patientId);
          this.cdr.detectChanges();
        },
        error: (err: any) => {
          this.showDeactivateModal = false;
          this.showToast(err.error?.message || 'Deactivation failed.', 'red');
          console.error(err);
          this.cdr.detectChanges();
        }
      });
  }

  getStatusClass(status: string): string {
    const map: any = {
      'ACTIVE':    'b-active',
      'INACTIVE':  'b-withdrawn',
      'COMPLETED': 'b-completed',
      'WITHDRAWN': 'b-withdrawn'
    };
    return map[status] || 'b-not-enrolled';
  }

  showToast(msg: string, type = 'green') {
    this.toastMessage = msg;
    this.toastType    = type;
    this.toastVisible = true;
    setTimeout(() => this.toastVisible = false, 3000);
    this.cdr.detectChanges();
  }
}


