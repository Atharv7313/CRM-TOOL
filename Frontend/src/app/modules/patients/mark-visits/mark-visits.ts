import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { VisitService } from '../../../services/visit.service';
import { EnrollmentService } from '../../../services/enrollment.service';
import { Visit } from '../../../models/visit';
import { Protocol, ProtocolSite } from '../../../models/enrollment';
import { TrialsNavService } from '../../../services/trials-nav.service';

@Component({
  selector: 'app-mark-visits',
  standalone: false,
  templateUrl: './mark-visits.html',
  styleUrls: ['./mark-visits.css']
})
export class MarkVisits implements OnInit {

  protocols: Protocol[] = [];
  reviewSites: ProtocolSite[] = [];

  searchQuery = '';
  reviewProtocolId = '';
  reviewSiteId = '';
  reviewDate = '';

  /** Today's date as YYYY-MM-DD — future dates cannot be marked */
  get todayDate(): string {
    const d = new Date();
    const yyyy = d.getFullYear();
    const mm   = String(d.getMonth() + 1).padStart(2, '0');
    const dd   = String(d.getDate()).padStart(2, '0');
    return `${yyyy}-${mm}-${dd}`;
  }

  reviewVisits: Visit[] = [];
  attendedIds: string[] = [];

  searched = false;
  today = new Date();

  showConfirmModal = false;
  attendedVisits: Visit[] = [];
  missedVisits: Visit[] = [];

  toastVisible = false;
  toastMessage = '';
  toastType = 'green';

  // Step counter removed — dynamic per protocol

  // ── Pagination ──
  currentPage = 1;
  readonly pageSize = 10;

  get totalPages(): number {
    return Math.ceil(this.reviewVisits.length / this.pageSize);
  }

  get pageStart(): number {
    return this.reviewVisits.length === 0
      ? 0
      : (this.currentPage - 1) * this.pageSize + 1;
  }

  get pageEnd(): number {
    return Math.min(this.currentPage * this.pageSize, this.reviewVisits.length);
  }

  get pagedVisits(): Visit[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.reviewVisits.slice(start, start + this.pageSize);
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

  constructor(
    private visitService: VisitService,
    private enrollmentService: EnrollmentService,
    private router: Router,
    private cdr: ChangeDetectorRef,
    private trialsNav: TrialsNavService
  ) {}

  ngOnInit() {
    this.reviewDate = this.todayDate;
    this.loadProtocols();
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
    this.reviewSiteId = '';
    this.reviewSites = [];
    this.reviewVisits = [];
    this.attendedIds = [];
    this.searched = false;
    if (!this.reviewProtocolId) return;

    this.enrollmentService.getSitesByProtocol(this.reviewProtocolId).subscribe({
      next: (res: any) => {
        this.reviewSites = res.data;
        this.cdr.detectChanges();
      },
      error: (err: any) => console.error(err)
    });
  }

  loadVisits() {
    this.searched = true;
    this.attendedIds = [];
    this.currentPage = 1;

    const site = this.reviewSiteId || undefined;

    // Fetch SCHEDULED and RESCHEDULED visits for the date — both must be markable
    forkJoin([
      this.visitService.getFiltered(this.reviewDate, site, 'SCHEDULED'),
      this.visitService.getFiltered(this.reviewDate, site, 'RESCHEDULED')
    ]).subscribe({
      next: ([scheduledRes, rescheduledRes]: [any, any]) => {
        let data = [...scheduledRes.data, ...rescheduledRes.data];
        if (this.searchQuery.trim()) {
          const q = this.searchQuery.trim().toLowerCase();
          data = data.filter((v: any) => v.patientName?.toLowerCase().includes(q));
        }
        this.reviewVisits = data;
        this.restoreDraft();
        this.cdr.detectChanges();
      },
      error: (err: any) => console.error(err)
    });
  }

  resetFilters() {
    this.clearDraft();
    this.searchQuery = '';
    this.reviewProtocolId = '';
    this.reviewSiteId = '';
    this.reviewDate = '';
    this.reviewSites = [];
    this.reviewVisits = [];
    this.attendedIds = [];
    this.searched = false;
    this.currentPage = 1;
    this.cdr.detectChanges();
  }


  // ── Session persistence — attendance survives accidental navigation ──────
  private readonly STORAGE_KEY = 'mark_visits_draft';

  private saveDraft() {
    sessionStorage.setItem(this.STORAGE_KEY, JSON.stringify({
      date:         this.reviewDate,
      protocolId:   this.reviewProtocolId,
      siteId:       this.reviewSiteId,
      attendedIds:  this.attendedIds
    }));
  }

  private restoreDraft() {
    const raw = sessionStorage.getItem(this.STORAGE_KEY);
    if (!raw) return;
    try {
      const draft = JSON.parse(raw);
      if (
        draft.date       === this.reviewDate &&
        draft.protocolId === this.reviewProtocolId &&
        draft.siteId     === this.reviewSiteId
      ) {
        // Only restore IDs that still exist in the loaded visit list
        this.attendedIds = (draft.attendedIds as string[])
          .filter(id => this.reviewVisits.some(v => v.visitId === id));
      }
    } catch { /* corrupt storage — ignore */ }
  }

  private clearDraft() {
    sessionStorage.removeItem(this.STORAGE_KEY);
  }
  // ──────────────────────────────────────────────────────────────────────────

  isAttended(v: Visit): boolean {
    return this.attendedIds.includes(v.visitId);
  }

  toggleAttended(v: Visit) {
    if (this.isAttended(v)) {
      this.attendedIds = this.attendedIds.filter(id => id !== v.visitId);
    } else {
      this.attendedIds.push(v.visitId);
    }
    this.saveDraft();
  }

  toggleAll(event: any) {
    this.attendedIds = (event.target as HTMLInputElement).checked
      ? this.reviewVisits.map(v => v.visitId)
      : [];
    this.saveDraft();
  }

  openConfirmModal() {
    this.attendedVisits = this.reviewVisits.filter(v =>
      this.attendedIds.includes(v.visitId)
    );
    this.missedVisits = this.reviewVisits.filter(v =>
      !this.attendedIds.includes(v.visitId)
    );
    this.showConfirmModal = true;
    this.cdr.detectChanges();
  }

  closeConfirmModal() {
    this.showConfirmModal = false;
    this.cdr.detectChanges();
  }

  confirmAndSave() {
    this.closeConfirmModal();
    if (!this.reviewDate) {
      this.showToast('Please select a date before saving.', 'red');
      return;
    }
    this.visitService.submitReview({
      date: this.reviewDate,
      protocolSiteId: this.reviewSiteId || undefined,
      attendedVisitIds: this.attendedIds
    }).subscribe({
      next: (res: any) => {
        this.clearDraft(); // attendance successfully saved — remove draft
        this.showToast(res.data || 'Review saved successfully', 'green');
        setTimeout(() => this.trialsNav.goVisits(), 1500);
      },
      error: (err: any) => {
        this.showToast('Failed to save review', 'red');
        console.error(err);
      }
    });
  }

  goBack() {
    this.trialsNav.goVisits();
  }

  showToast(msg: string, type = 'green') {
    this.toastMessage = msg;
    this.toastType = type;
    this.toastVisible = true;
    setTimeout(() => this.toastVisible = false, 3000);
    this.cdr.detectChanges();
  }
}


