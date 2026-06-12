import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { PatientService } from '../../../services/patient.service';
import { EnrollmentService } from '../../../services/enrollment.service';
import { Patient } from '../../../models/patient';
import { Router } from '@angular/router';
import { Enrollment, Protocol, ProtocolSite } from '../../../models/enrollment';
import { TrialsNavService } from '../../../services/trials-nav.service';

@Component({
  selector: 'app-patients-list',
  standalone: false,
  templateUrl: './patients-list.html',
  styleUrls: ['./patients-list.css']
})
export class PatientsList implements OnInit {

  patients: Patient[] = [];
  filteredPatients: Patient[] = [];

  searchQuery = '';
  statusFilter = '';
  filterProtocolId = '';
  filterSiteId = '';
  filterSites: ProtocolSite[] = [];

  showEditModal = false;
  showEnrollModal = false;
  showWithdrawModal = false;

  // ── Patient detail panel (deactivate flow) ──
  showDetailPanel = false;
  detailPatient: Patient | null = null;
  detailEnrollments: Enrollment[] = [];
  detailLoading = false;
  showDeactivateConfirm = false;
  withdrawingPatient: Patient | null = null;

  toastVisible = false;
  toastMessage = '';

  editPatient: any = {};
  editingId = '';
  editError = '';

  /** Max date for DOB — yesterday (cannot be today or future) */
  get maxDob(): string {
    const d = new Date();
    d.setDate(d.getDate() - 1);
    return d.toISOString().split('T')[0];
  }

  enrollingPatient: Patient | null = null;
  protocols: Protocol[] = [];
  sites: ProtocolSite[] = [];
  selectedProtocolId = '';
  selectedSiteId = '';
  enrollErrorMessage = '';

  stats = { active: 0, completed: 0, withdrawn: 0, cancelled: 0, notEnrolled: 0, inactive: 0 };

  // ── Pagination ──
  currentPage = 1;
  readonly pageSize = 10;

  get totalPages(): number {
    return Math.ceil(this.filteredPatients.length / this.pageSize);
  }

  get pageStart(): number {
    return this.filteredPatients.length === 0
      ? 0
      : (this.currentPage - 1) * this.pageSize + 1;
  }

  get pageEnd(): number {
    return Math.min(this.currentPage * this.pageSize, this.filteredPatients.length);
  }

  get pagedPatients(): Patient[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.filteredPatients.slice(start, start + this.pageSize);
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
    const end = Math.min(total - 1, current + 1);
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

  get enrollmentWindowClosed(): boolean {
    if (!this.selectedProtocolId) return false;
    const p = this.protocols.find(x => x.protocolId === this.selectedProtocolId);
    return p ? this.isWindowClosed(p) : false;
  }



  constructor(
    private patientService: PatientService,
    private enrollmentService: EnrollmentService,
    private router: Router,
    private cdr: ChangeDetectorRef,
    private trialsNav: TrialsNavService
  ) { }

  ngOnInit() {
    this.loadPatients();
    this.loadProtocols();

    // Reload patients whenever navigation returns to home/patients view
    // (e.g. after adding a new patient from AddPatient component)
    this.trialsNav.nav$.subscribe(view => {
      if (view === 'home' || view === 'patients' || view === 'patients-list') {
        this.loadPatients();
        this.cdr.detectChanges();
      }
    });
  }

  loadPatients() {
    this.patientService.getAll().subscribe({
      next: (res: any) => {
        this.patients = [...res.data];
        this.filteredPatients = [...res.data];
        this.currentPage = 1;
        this.calculateStats();
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

  goTo(view: string) { this.trialsNav.go(view); }

  viewPatient(p: Patient) {
    this.trialsNav.setPending('patients-list');
    this.router.navigate(['/patient-detail', p.patientId]);
  }

  closeDetailPanel() {
    this.showDetailPanel = false;
    this.showDeactivateConfirm = false;
    this.detailPatient = null;
    this.detailEnrollments = [];
  }

  confirmDetailDeactivate() { this.showDeactivateConfirm = true; }

  executeDeactivate() {
    if (!this.detailPatient) return;
    this.patientService.deactivate(this.detailPatient.patientId).subscribe({
      next: () => {
        this.showDeactivateConfirm = false;
        this.closeDetailPanel();
        this.loadPatients();
        this.showToast(`${this.detailPatient?.name ?? 'Patient'} deactivated successfully`);
      },
      error: (err: any) => {
        this.showDeactivateConfirm = false;
        this.showToast(err.error?.message || 'Deactivation failed.');
      }
    });
  }

  getDetailStatusClass(status: string): string {
    const map: any = {
      'ACTIVE':    'b-active',
      'INACTIVE':  'b-withdrawn'
    };
    return map[status] || 'b-not-enrolled';
  }

  onFilterProtocolChange() {
    this.filterSiteId = '';
    this.filterSites = [];
    if (!this.filterProtocolId) return;
    // Only load sites for the dropdown — search runs only on Apply
    this.enrollmentService.getSitesByProtocol(this.filterProtocolId).subscribe({
      next: (res: any) => {
        this.filterSites = res.data;
        this.cdr.detectChanges();
      },
      error: (err: any) => console.error(err)
    });
  }

  clearFilters() {
    this.searchQuery = '';
    this.filterProtocolId = '';
    this.filterSiteId = '';
    this.filterSites = [];
    this.statusFilter = '';
    this.filteredPatients = [...this.patients];
    this.currentPage = 1;
  }

  runSearch() {
    const q = this.searchQuery.toLowerCase();
    this.filteredPatients = this.patients.filter(p => {
      const matchSearch = !q ||
        p.name.toLowerCase().includes(q) ||
        (p.contactInfo?.toLowerCase().includes(q) ?? false);
      const matchStatus = !this.statusFilter ||
        p.enrollmentStatus === this.statusFilter;
      const matchProtocol = !this.filterProtocolId ||
        p.protocolTitle === this.protocols.find(
          pr => pr.protocolId === this.filterProtocolId
        )?.title;
      const matchSite = !this.filterSiteId ||
        p.siteName === this.filterSites.find(
          s => s.protocolSiteId === this.filterSiteId
        )?.siteName;
      return matchSearch && matchStatus && matchProtocol && matchSite;
    });
    this.currentPage = 1;
  }

  onProtocolChange() {
    this.selectedSiteId = '';
    this.sites = [];
    this.enrollErrorMessage = '';
    if (!this.selectedProtocolId) return;

    const selected = this.protocols.find(p => p.protocolId === this.selectedProtocolId);
    if (selected && this.isWindowClosed(selected)) {
      this.enrollErrorMessage = `Enrollment window closed for "${selected.title}" on ${
        this.getEnrollmentWindowEnd(selected)?.toLocaleDateString('en-GB', {
          day: '2-digit', month: 'short', year: 'numeric'
        })
      }. New enrollments are not allowed.`;
      this.cdr.detectChanges();
      return;
    }

    if (this.enrollingPatient) {
      if (selected &&
          this.enrollingPatient.previousProtocols?.includes(selected.title)) {
        this.enrollErrorMessage =
          `${this.enrollingPatient.name} has already participated in ` +
          `"${selected.title}". Please choose a different protocol.`;
        this.cdr.detectChanges();
        return;
      }
    }

    this.enrollmentService.getSitesByProtocol(this.selectedProtocolId).subscribe({
      next: (res: any) => { this.sites = res.data; this.cdr.detectChanges(); },
      error: (err: any) => console.error(err)
    });
  }

  calculateStats() {
    this.stats.active      = this.patients.filter(p => p.enrollmentStatus === 'Active').length;
    this.stats.completed   = this.patients.filter(p => p.enrollmentStatus === 'Completed').length;
    this.stats.withdrawn   = this.patients.filter(p => p.enrollmentStatus === 'Withdrawn').length;
    this.stats.cancelled   = this.patients.filter(p => p.enrollmentStatus === 'Cancelled').length;
    this.stats.notEnrolled = this.patients.filter(p => p.enrollmentStatus === 'Not enrolled').length;
    this.stats.inactive    = this.patients.filter(p => p.patientStatus === 'INACTIVE').length;
  }

  openWithdrawModal(p: Patient) {
    this.withdrawingPatient = p;
    this.showWithdrawModal = true;
  }

  closeWithdrawModal() {
    this.showWithdrawModal = false;
    this.withdrawingPatient = null;
  }

  confirmWithdraw() {
    if (!this.withdrawingPatient || !this.withdrawingPatient.enrollmentId) return;
    this.enrollmentService.withdraw(this.withdrawingPatient.enrollmentId).subscribe({
      next: () => {
        const name = this.withdrawingPatient?.name;
        this.closeWithdrawModal();
        this.loadPatients();
        this.showToast(`${name} withdrawn successfully`);
      },
      error: (err: any) => console.error(err)
    });
  }

  openEnrollModal(p: Patient) {
    this.enrollingPatient = p;
    this.selectedProtocolId = '';
    this.selectedSiteId = '';
    this.sites = [];
    this.enrollErrorMessage = '';
    this.showEnrollModal = true;
  }

  closeEnrollModal() {
    this.showEnrollModal = false;
    this.enrollingPatient = null;
    this.selectedProtocolId = '';
    this.selectedSiteId = '';
    this.sites = [];
    this.enrollErrorMessage = '';
  }

  saveEnroll() {
    if (!this.enrollingPatient || !this.selectedProtocolId || !this.selectedSiteId) return;
    if (this.enrollmentWindowClosed) return;

    this.enrollmentService.enroll({
      patientId: this.enrollingPatient.patientId,
      protocolSiteId: this.selectedSiteId
    }).subscribe({
      next: () => {
        const name = this.enrollingPatient?.name;
        this.closeEnrollModal();
        this.loadPatients();
        this.showToast(`${name} enrolled successfully`);
      },
      error: (err: any) => {
        this.enrollErrorMessage = err.error?.message || 'Enrollment failed. Please try again.';
        this.cdr.detectChanges();
      }
    });
  }

  openEditModal(p: Patient) {
    this.editingId   = p.patientId;
    this.editError   = '';
    this.editPatient = { name: p.name, dateOfBirth: p.dateOfBirth, contactInfo: p.contactInfo };
    this.showEditModal = true;
  }

  closeEditModal() {
    this.showEditModal = false;
    this.editingId = '';
    this.editError = '';
  }

  private setEditError(msg: string) {
    this.editError = msg;
    setTimeout(() => { this.editError = ''; this.cdr.detectChanges(); }, 5000);
  }

  saveEdit() {
    this.editError = '';

    if (!this.editPatient.name?.trim())  { this.setEditError('Patient name is required.'); return; }
    if (!this.editPatient.dateOfBirth)   { this.setEditError('Date of birth is required.'); return; }
    const dob = new Date(this.editPatient.dateOfBirth);
    const today = new Date(); today.setHours(0, 0, 0, 0);
    if (dob >= today) { this.setEditError('Date of birth cannot be today or a future date.'); return; }
    if (this.editPatient.contactInfo?.trim()) {
      const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
      if (!emailRegex.test(this.editPatient.contactInfo.trim())) {
        this.setEditError('Contact info must be a valid email address (e.g. john@email.com).'); return;
      }
    }

    this.patientService.update(this.editingId, this.editPatient).subscribe({
      next: (res: any) => {
        this.closeEditModal();
        this.loadPatients();
        this.showToast(`${res.data.name} updated successfully`);
      },
      error: (err: any) => this.setEditError(err.error?.message || 'Update failed. Please try again.')
    });
  }

  getStatusDotClass(status: string): string {
    const map: Record<string, string> = {
      'Active':       'pl-status-active',
      'Completed':    'pl-status-completed',
      'Withdrawn':    'pl-status-withdrawn',
      'Cancelled':    'pl-status-cancelled',
      'Not enrolled': 'pl-status-not-enrolled',
      'Inactive':     'pl-status-inactive'
    };
    return map[status] ?? 'pl-status-not-enrolled';
  }

  getBadgeClass(status: string): string {
    const map: Record<string, string> = {
      'Active':       'b-active',
      'Completed':    'b-completed',
      'Withdrawn':    'b-withdrawn',
      'Cancelled':    'b-cancelled',
      'Not enrolled': 'b-not-enrolled',
      'Inactive':     'b-inactive'
    };
    return map[status] ?? 'b-not-enrolled';
  }

  showToast(msg: string) {
    this.toastMessage = msg;
    this.toastVisible = true;
    setTimeout(() => {
      this.toastVisible = false;
      this.cdr.detectChanges();
    }, 4000);
  }
}


