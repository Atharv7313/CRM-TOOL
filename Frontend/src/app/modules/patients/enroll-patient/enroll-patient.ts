import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { Router } from '@angular/router';
import { PatientService } from '../../../services/patient.service';
import { EnrollmentService } from '../../../services/enrollment.service';
import { Patient } from '../../../models/patient';
import { Protocol, ProtocolSite } from '../../../models/enrollment';
import { TrialsNavService } from '../../../services/trials-nav.service';

@Component({
  selector: 'app-enroll-patient',
  standalone: false,
  templateUrl: './enroll-patient.html',
  styleUrls: ['./enroll-patient.css']
})
export class EnrollPatient implements OnInit {

  patients: Patient[] = [];
  eligiblePatients: Patient[] = [];
  protocols: Protocol[] = [];
  sites: ProtocolSite[] = [];

  selectedProtocolId = '';
  selectedSiteId = '';
  statusFilter = '';
  selected: Patient[] = [];

  toastVisible = false;
  toastMessage = '';
  toastType = 'green';

  // ── Pagination ──
  currentPage = 1;
  readonly pageSize = 10;

  get totalPages(): number {
    return Math.ceil(this.eligiblePatients.length / this.pageSize);
  }

  get pageStart(): number {
    return this.eligiblePatients.length === 0
      ? 0
      : (this.currentPage - 1) * this.pageSize + 1;
  }

  get pageEnd(): number {
    return Math.min(this.currentPage * this.pageSize, this.eligiblePatients.length);
  }

  get pagedPatients(): Patient[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.eligiblePatients.slice(start, start + this.pageSize);
  }

  get pageNumbers(): number[] {
    const total = this.totalPages;
    const current = this.currentPage;
    const pages: number[] = [];

    if (total <= 7) {
      for (let i = 1; i <= total; i++) pages.push(i);
      return pages;
    }

    pages.push(1);
    if (current > 3) pages.push(-1);

    const start = Math.max(2, current - 1);
    const end   = Math.min(total - 1, current + 1);
    for (let i = start; i <= end; i++) pages.push(i);

    if (current < total - 2) pages.push(-1);
    pages.push(total);

    return pages;
  }

  goToPage(page: number) {
    if (page < 1 || page > this.totalPages) return;
    this.currentPage = page;
    this.cdr.detectChanges();
  }

  // ── Enrollment window helpers ──
  getEnrollmentWindowEnd(p: Protocol): Date | null {
    if (!p.endDate) return null;
    // Enrollment allowed until protocol end date
    return new Date(p.endDate);
  }

  isWindowClosed(p: Protocol): boolean {
    const windowEnd = this.getEnrollmentWindowEnd(p);
    if (!windowEnd) return false;
    return new Date() > windowEnd;
  }

  /** Only protocols whose enrollment window is still OPEN */
  get openProtocols(): Protocol[] {
    return this.protocols.filter(p => !this.isWindowClosed(p));
  }

  get enrollmentWindowClosed(): boolean {
    if (!this.selectedProtocolId) return false;
    const p = this.protocols.find(x => x.protocolId === this.selectedProtocolId);
    return p ? this.isWindowClosed(p) : false;
  }

  get enrollmentWindowEnd(): Date | null {
    if (!this.selectedProtocolId) return null;
    const p = this.protocols.find(x => x.protocolId === this.selectedProtocolId);
    return p ? this.getEnrollmentWindowEnd(p) : null;
  }

  get daysUntilClose(): number {
    if (!this.enrollmentWindowEnd) return 0;
    const diff = this.enrollmentWindowEnd.getTime() - new Date().getTime();
    return Math.max(0, Math.ceil(diff / (1000 * 60 * 60 * 24)));
  }

  constructor(
    private patientService: PatientService,
    private enrollmentService: EnrollmentService,
    private router: Router,
    private cdr: ChangeDetectorRef,
    private trialsNav: TrialsNavService
  ) {}

  ngOnInit() {
    this.loadPatients();
    this.loadProtocols();
  }

  loadPatients() {
    this.patientService.getAll().subscribe({
      next: (res: any) => {
        this.patients = [...res.data];
        this.filterEligiblePatients();
        this.cdr.detectChanges();
      },
      error: (err: any) => console.error(err)
    });
  }

  loadProtocols() {
    this.enrollmentService.getProtocols().subscribe({
      next: (res: any) => {
        this.protocols = res.data;
        this.cdr.detectChanges();
      },
      error: (err: any) => console.error(err)
    });
  }

  onProtocolChange() {
    this.selectedSiteId = '';
    this.sites = [];
    this.selected = [];
    this.filterEligiblePatients();

    if (this.selectedProtocolId) {
      this.enrollmentService.getSitesByProtocol(this.selectedProtocolId).subscribe({
        next: (res: any) => {
          this.sites = res.data;
          this.cdr.detectChanges();
        },
        error: (err: any) => console.error(err)
      });
    }
  }

  filterEligiblePatients() {
    // exclude both Active and Inactive patients
    let eligible = this.patients.filter(
      p => p.enrollmentStatus !== 'Active' && p.enrollmentStatus !== 'Inactive'
    );

    if (this.selectedProtocolId) {
      const selectedProtocol = this.protocols.find(
        p => p.protocolId === this.selectedProtocolId
      );
      if (selectedProtocol) {
        eligible = eligible.filter(
          p => !(p.previousProtocols?.includes(selectedProtocol.title))
        );
      }
    }

    if (this.statusFilter) {
      eligible = eligible.filter(p => p.enrollmentStatus === this.statusFilter);
    }

    this.selected = this.selected.filter(s =>
      eligible.some(e => e.patientId === s.patientId)
    );

    this.eligiblePatients = eligible;
    this.currentPage = 1;
    this.cdr.detectChanges();
  }

  clearFilters() {
    this.statusFilter = '';
    this.filterEligiblePatients();
  }

  isSelected(p: Patient): boolean {
    return this.selected.some(s => s.patientId === p.patientId);
  }

  toggleSelect(p: Patient) {
    if (this.isSelected(p)) {
      this.selected = this.selected.filter(s => s.patientId !== p.patientId);
    } else {
      this.selected.push(p);
    }
  }

  toggleAll(event: any) {
    if (event.target.checked) {
      this.selected = [...this.eligiblePatients];
    } else {
      this.selected = [];
    }
  }

  saveEnroll() {
    if (!this.selectedProtocolId || !this.selectedSiteId || this.selected.length === 0) return;
    if (this.enrollmentWindowClosed) return;

    let completed = 0;
    let failed = 0;
    const total = this.selected.length;

    this.selected.forEach(p => {
      this.enrollmentService.enroll({
        patientId: p.patientId,
        protocolSiteId: this.selectedSiteId
      }).subscribe({
        next: () => {
          completed++;
          if (completed + failed === total) {
            const msg = `${completed} patient${completed !== 1 ? 's' : ''} enrolled successfully`;
            this.showToast(msg, 'green');
            this.selected = [];
            this.loadPatients();
          }
        },
        error: (err: any) => {
          failed++;
          console.error('Enroll error:', err.error);
          if (completed + failed === total) {
            const msg = completed > 0
              ? `${completed} enrolled · ${failed} failed`
              : 'Enrollment failed. Please try again.';
            this.showToast(msg, completed > 0 ? 'green' : 'amber');
          }
        }
      });
    });
  }

  getBadgeClass(status: string): string {
    const map: any = {
      'Active':       'b-active',
      'Completed':    'b-completed',
      'Withdrawn':    'b-withdrawn',
      'Not enrolled': 'b-not-enrolled',
      'Inactive':     'b-inactive'
    };
    return map[status] || 'b-not-enrolled';
  }

  goHome() { this.trialsNav.goHome(); }

  showToast(msg: string, type = 'green') {
    this.toastMessage = msg;
    this.toastType = type;
    this.toastVisible = true;
    setTimeout(() => this.toastVisible = false, 3000);
    this.cdr.detectChanges();
  }
}


