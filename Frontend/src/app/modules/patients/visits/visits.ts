import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { Router } from '@angular/router';
import { VisitService } from '../../../services/visit.service';
import { EnrollmentService } from '../../../services/enrollment.service';
import { Visit } from '../../../models/visit';
import { Protocol, ProtocolSite } from '../../../models/enrollment';
import { TrialsNavService } from '../../../services/trials-nav.service';

@Component({
  selector: 'app-visits',
  standalone: false,
  templateUrl: './visits.html',
  styleUrls: ['./visits.css']
})
export class Visits implements OnInit {

  visits: Visit[] = [];
  filteredVisits: Visit[] = [];
  protocols: Protocol[] = [];
  sites: ProtocolSite[] = [];

  filterDate = '';
  filterProtocolId = '';
  filterSiteId = '';
  filterStatus = 'SCHEDULED'; // default — show scheduled visits only
  searchQuery = '';

  selectedVisits: Visit[] = [];

  actionType = '';
  rescheduleDate = '';
  rescheduleDateError = '';
  bulkVisitName = '';
  bulkVisitDate = '';
  bulkVisitNameError = '';

  toastVisible = false;
  toastMessage = '';
  toastType = 'green';

  // Step counter is now dynamic — based on visit date order, not hardcoded names

  // ── Pagination ──
  currentPage = 1;
  readonly pageSize = 10;

  get totalPages(): number {
    return Math.ceil(this.filteredVisits.length / this.pageSize);
  }

  get pageStart(): number {
    return this.filteredVisits.length === 0
      ? 0
      : (this.currentPage - 1) * this.pageSize + 1;
  }

  get pageEnd(): number {
    return Math.min(this.currentPage * this.pageSize, this.filteredVisits.length);
  }

  get pagedVisits(): Visit[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.filteredVisits.slice(start, start + this.pageSize);
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

  get hasNonActiveSelected(): boolean {
    return this.selectedVisits.some(v => v.enrollmentStatus !== 'ACTIVE');
  }

  /** Today's date as YYYY-MM-DD */
  get minDate(): string {
    return new Date().toISOString().split('T')[0];
  }

  /** Earliest reschedule date — latest enrollmentWindowEnd among selected visits */
  get bulkRescheduleMin(): string {
    if (!this.selectedVisits.length) return this.minDate;
    const dates = this.selectedVisits
      .map(v => (v as any).enrollmentWindowEnd?.split('T')[0])
      .filter((d: string) => !!d);
    if (!dates.length) return this.minDate;
    // Use the latest (most restrictive) window end
    return dates.sort().reverse()[0];
  }

  /** Latest reschedule date — earliest protocolEndDate among selected visits */
  get bulkRescheduleMax(): string {
    if (!this.selectedVisits.length) return '';
    const dates = this.selectedVisits
      .map(v => (v as any).protocolEndDate?.split('T')[0])
      .filter((d: string) => !!d);
    if (!dates.length) return '';
    // Use the earliest (most restrictive) end date
    return dates.sort()[0];
  }

  /** True when viewing Cancelled or Completed — read-only mode, no actions */
  get isReadOnlyView(): boolean {
    return this.filterStatus === 'CANCELLED' || this.filterStatus === 'COMPLETED' || this.filterStatus === 'MISSED';
  }

  constructor(
    private visitService: VisitService,
    private enrollmentService: EnrollmentService,
    private router: Router,
    private cdr: ChangeDetectorRef,
    public trialsNav: TrialsNavService
  ) {}

  ngOnInit() {
    this.loadProtocols();
    this.loadVisits();
  }

  getNextVisit(v: any): string {
    return v.nextVisit || v.NextVisit || '';
  }

  private deduplicateToCurrentVisit(allVisits: any[]): any[] {
    const enrollmentMap = new Map<string, any>();

    for (const v of allVisits) {
      const existing = enrollmentMap.get(v.enrollmentId);
      const isActive = v.visitStatus === 'SCHEDULED' || v.visitStatus === 'RESCHEDULED';
      const existingActive = existing &&
        (existing.visitStatus === 'SCHEDULED' || existing.visitStatus === 'RESCHEDULED');

      if (!existing) {
        enrollmentMap.set(v.enrollmentId, v);
      } else if (isActive && !existingActive) {
        enrollmentMap.set(v.enrollmentId, v);
      } else if (isActive && existingActive) {
        // Among active visits, pick the earliest (soonest upcoming)
        if (new Date(v.visitDate) < new Date(existing.visitDate)) {
          enrollmentMap.set(v.enrollmentId, v);
        }
      } else if (!isActive && !existingActive) {
        // Among completed/missed, pick the latest (most recent)
        if (new Date(v.visitDate) > new Date(existing.visitDate)) {
          enrollmentMap.set(v.enrollmentId, v);
        }
      }
    }

    return [...enrollmentMap.values()];
  }

  loadVisits() {
    // Always load with SCHEDULED as the default status
    this.visitService.getFiltered(undefined, undefined, 'SCHEDULED').subscribe({
      next: (res: any) => {
        // Deduplicate to one row per patient, keep only ACTIVE enrollments
        let deduplicated = this.deduplicateToCurrentVisit(res.data)
          .filter((v: any) => v.enrollmentStatus === 'ACTIVE');
        if (this.searchQuery.trim()) {
          const q = this.searchQuery.trim().toLowerCase();
          deduplicated = deduplicated.filter((v: any) => v.patientName?.toLowerCase().includes(q));
        }
        this.visits = deduplicated;
        this.filteredVisits = deduplicated;
        this.selectedVisits = [];
        this.currentPage = 1;
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

  onProtocolFilter() {
    this.filterSiteId = '';
    this.sites = [];
    if (this.filterProtocolId) {
      this.enrollmentService.getSitesByProtocol(this.filterProtocolId).subscribe({
        next: (res: any) => { this.sites = res.data; this.cdr.detectChanges(); },
        error: (err: any) => console.error(err)
      });
    }
  }

  applyFilters() {
    this.visitService.getFiltered(
      this.filterDate   || undefined,
      this.filterSiteId || undefined,
      this.filterStatus || undefined
    ).subscribe({
      next: (res: any) => {
        let result: any[] = [...res.data];

        // Apply protocol filter client-side (API has no protocol param)
        if (this.filterProtocolId) {
          const selectedTitle = this.protocols
            .find(p => p.protocolId === this.filterProtocolId)?.title;
          if (selectedTitle) {
            result = result.filter(v => v.protocolTitle === selectedTitle);
          }
        }

        // Always deduplicate
        result = this.deduplicateToCurrentVisit(result);

        // Filter by patient name search
        if (this.searchQuery.trim()) {
          const q = this.searchQuery.trim().toLowerCase();
          result = result.filter((v: any) => v.patientName?.toLowerCase().includes(q));
        }

        this.visits = result;
        this.filteredVisits = result;
        this.selectedVisits = [];
        this.currentPage = 1;
        this.cdr.detectChanges();
      },
      error: (err: any) => console.error(err)
    });
  }

  clearFilters() {
    this.searchQuery = '';
    this.filterDate = '';
    this.filterProtocolId = '';
    this.filterSiteId = '';
    this.filterStatus = 'SCHEDULED';
    this.sites = [];
    this.currentPage = 1;
    this.loadVisits();
  }

  isSelected(v: Visit): boolean {
    return this.selectedVisits.some(s => s.visitId === v.visitId);
  }

  toggleSelect(v: Visit) {
    if (this.isSelected(v)) {
      this.selectedVisits = this.selectedVisits.filter(s => s.visitId !== v.visitId);
    } else {
      this.selectedVisits.push(v);
    }
    if (this.selectedVisits.length === 0) this.actionType = '';
  }

  toggleAll(event: any) {
    if ((event.target as HTMLInputElement).checked) {
      this.selectedVisits = [...this.filteredVisits];
    } else {
      this.selectedVisits = [];
      this.actionType = '';
    }
  }

  openBulk(type: string) {
    this.actionType = type;
    this.rescheduleDate = '';
    this.rescheduleDateError = '';
    this.bulkVisitName = '';
    this.bulkVisitDate = '';
    this.bulkVisitNameError = '';
    this.cdr.detectChanges();
  }

  closeBulk() {
    this.actionType = '';
    this.rescheduleDateError = '';
    this.bulkVisitNameError = '';
  }

  private setVisitError(prop: 'rescheduleDateError' | 'bulkVisitNameError', msg: string) {
    this[prop] = msg;
    this.cdr.detectChanges();
    setTimeout(() => { this[prop] = ''; this.cdr.detectChanges(); }, 5000);
  }

  saveBulkAdd() {
    this.bulkVisitNameError = '';
    if (!this.bulkVisitName) { this.setVisitError('bulkVisitNameError', 'Please enter a visit name.'); return; }
    if (!this.bulkVisitDate) { this.setVisitError('bulkVisitNameError', 'Please select a visit date.'); return; }
    let completed = 0;
    let failed = 0;
    const total = this.selectedVisits.length;
    this.selectedVisits.forEach(v => {
      this.visitService.add({
        enrollmentId: v.enrollmentId,
        visitName: this.bulkVisitName,
        visitDate: this.bulkVisitDate
      }).subscribe({
        next: () => {
          completed++;
          if (completed + failed === total) {
            this.closeBulk();
            this.loadVisits();
            this.showToast(`Visit added for ${completed} patient(s)`, 'green');
          }
        },
        error: (err: any) => {
          failed++;
          const errMsg: string = err.error?.message || '';
          const isDuplicate = errMsg.toLowerCase().includes('already exists');
          if (completed + failed === total) {
            if (isDuplicate && completed === 0) {
              // Show error INSIDE the modal — do NOT close it
              this.setVisitError('bulkVisitNameError',
                `A visit is already scheduled on ${new Date(this.bulkVisitDate).toLocaleDateString('en-GB', { day:'2-digit', month:'short', year:'numeric' })} for the selected patient(s). Please choose a different date.`);
            } else if (isDuplicate && completed > 0) {
              this.closeBulk();
              this.loadVisits();
              this.showToast(`${completed} added · ${failed} skipped (already scheduled on that date)`, 'amber');
            } else if (completed > 0) {
              this.closeBulk();
              this.loadVisits();
              this.showToast(`${completed} added · ${failed} skipped`, 'amber');
            } else {
              // Show other errors inside modal too
              this.setVisitError('bulkVisitNameError', errMsg || 'Failed to add visit. Please try again.');
            }
          }
        }
      });
    });
  }

  saveBulkReschedule() {
    this.rescheduleDateError = '';
    if (!this.rescheduleDate) {
      this.setVisitError('rescheduleDateError', 'Please select a new date to reschedule the selected visits.'); return;
    }
    const selected = new Date(this.rescheduleDate);
    const today = new Date(); today.setHours(0, 0, 0, 0);
    if (selected < today) {
      this.setVisitError('rescheduleDateError', 'Reschedule date cannot be in the past.'); return;
    }
    if (this.bulkRescheduleMin && this.rescheduleDate < this.bulkRescheduleMin) {
      this.setVisitError('rescheduleDateError', `Date must be after the enrollment window closes (${this.bulkRescheduleMin}).`); return;
    }
    if (this.bulkRescheduleMax && this.rescheduleDate > this.bulkRescheduleMax) {
      this.setVisitError('rescheduleDateError', `Date cannot be after the protocol ends (${this.bulkRescheduleMax}).`); return;
    }
    let completed = 0;
    const total = this.selectedVisits.length;
    this.selectedVisits.forEach(v => {
      this.visitService.reschedule(v.visitId, { newDate: this.rescheduleDate }).subscribe({
        next: () => {
          completed++;
          if (completed === total) {
            this.closeBulk();
            this.loadVisits();
            this.showToast(`${total} visit(s) rescheduled`, 'amber');
          }
        },
        error: (err: any) => console.error(err)
      });
    });
  }

  saveBulkCancel() {
    let completed = 0;
    const total = this.selectedVisits.length;
    this.selectedVisits.forEach(v => {
      this.visitService.cancel(v.visitId).subscribe({
        next: () => {
          completed++;
          if (completed === total) {
            this.closeBulk();
            this.loadVisits();
            this.showToast(`${total} visit(s) removed`, 'grey');
          }
        },
        error: (err: any) => console.error(err)
      });
    });
  }

  viewDetail(v: Visit) {
    this.trialsNav.setPending('visits');
    this.router.navigate(['/visit-detail', v.enrollmentId], {
      state: { patientName: v.patientName }
    });
  }

  getBadgeClass(status: string): string {
    const map: Record<string, string> = {
      'SCHEDULED':   'b-scheduled',
      'COMPLETED':   'b-completed',
      'RESCHEDULED': 'b-rescheduled',
      'MISSED':      'b-missed',
      'CANCELLED':   'b-cancelled'
    };
    return map[status] ?? 'b-scheduled';
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


