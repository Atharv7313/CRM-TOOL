import { Component, OnInit, ChangeDetectorRef, HostListener } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { VisitService } from '../../../services/visit.service';
import { Visit } from '../../../models/visit';
import { TrialsNavService } from '../../../services/trials-nav.service';

@Component({
  selector: 'app-visit-detail',
  standalone: false,
  templateUrl: './visit-detail.html',
  styleUrls: ['./visit-detail.css']
})
export class VisitDetail implements OnInit {

  enrollmentId = '';
  patientName = '';
  protocolTitle = '';
  siteName = '';
  protocolEndDate = '';
  enrollmentWindowEnd = '';

  /** Earliest selectable visit date (day after enrollment window closes) */
  get minVisitDate(): string { return this.enrollmentWindowEnd; }

  /** Latest selectable visit date (protocol end date) */
  get maxVisitDate(): string { return this.protocolEndDate; }
  upcomingVisits: Visit[] = [];
  historyVisits: Visit[] = [];

  enrollmentStatus = '';

  showAddVisit = false;
  newVisitName = '';
  newVisitDate = '';

  showRescheduleModal = false;
  rescheduleVisitId = '';
  rescheduleVisitName = '';
  rescheduleDate = '';
  rescheduleDateError = '';
  addVisitNameError = '';
  addVisitDateError = '';

  get minDate(): string {
    return new Date().toISOString().split('T')[0];
  }

  activeActionId = '';   // which row has its action dropdown open

  showConfirmModal = false;
  confirmMessage = '';
  confirmAction: (() => void) | null = null;

  toastVisible = false;
  toastMessage = '';
  toastType = 'green';

  get isActiveEnrollment(): boolean {
    return this.enrollmentStatus === 'ACTIVE';
  }

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private visitService: VisitService,
    private cdr: ChangeDetectorRef,
    private trialsNav: TrialsNavService
  ) {}

  ngOnInit() {
    this.enrollmentId = this.route.snapshot.paramMap.get('enrollmentId') || '';
    const histState = history.state;
    if (histState?.patientName) {
      this.patientName = histState.patientName;
    }
    this.loadDetail();
  }

  loadDetail() {
    this.visitService.getByEnrollment(this.enrollmentId).subscribe({
      next: (res: any) => {
        this.upcomingVisits = (res.data.Upcoming || res.data.upcoming || [])
          .sort((a: Visit, b: Visit) =>
            new Date(a.visitDate).getTime() - new Date(b.visitDate).getTime());
        this.historyVisits = (res.data.History || res.data.history || [])
          .sort((a: Visit, b: Visit) =>
            new Date(a.visitDate).getTime() - new Date(b.visitDate).getTime());

        const allVisits = [...this.upcomingVisits, ...this.historyVisits];
        if (allVisits.length > 0) {
          const first = allVisits[0] as any;
          this.enrollmentStatus   = first.enrollmentStatus   || '';
          this.protocolTitle      = first.protocolTitle      || '';
          this.siteName           = first.siteName           || '';
          this.protocolEndDate    = first.protocolEndDate
            ? first.protocolEndDate.split('T')[0] : '';
          this.enrollmentWindowEnd = first.enrollmentWindowEnd
            ? first.enrollmentWindowEnd.split('T')[0] : '';
        }

        this.cdr.detectChanges();
      },
      error: (err: any) => console.error(err)
    });
  }

  saveAddVisit() {
    this.addVisitNameError = '';
    this.addVisitDateError = '';

    if (!this.newVisitName?.trim()) {
      this.addVisitNameError = 'Visit name is required.';
      this.cdr.detectChanges();
      setTimeout(() => { this.addVisitNameError = ''; this.cdr.detectChanges(); }, 5000);
      return;
    }
    if (!this.newVisitDate) {
      this.addVisitDateError = 'Visit date is required.';
      this.cdr.detectChanges();
      setTimeout(() => { this.addVisitDateError = ''; this.cdr.detectChanges(); }, 5000);
      return;
    }
    this.visitService.add({
      enrollmentId: this.enrollmentId,
      visitName: this.newVisitName,
      visitDate: this.newVisitDate
    }).subscribe({
      next: () => {
        this.showAddVisit = false;
        this.newVisitName = '';
        this.newVisitDate = '';
        this.addVisitNameError = '';
        this.addVisitDateError = '';
        this.loadDetail();
        this.showToast('Visit added', 'green');
      },
      error: (err: any) => {
        const msg: string = err.error?.message || '';
        const isDuplicate = msg.toLowerCase().includes('already scheduled');
        // Show inline error under the date field
        this.addVisitDateError = isDuplicate
          ? 'A visit already exists on this date'
          : (msg || 'Failed to add visit.');
        this.cdr.detectChanges();
        setTimeout(() => { this.addVisitDateError = ''; this.cdr.detectChanges(); }, 5000);
      }
    });
  }

  @HostListener('document:click')
  onDocumentClick() {
    if (this.activeActionId) {
      this.activeActionId = '';
      this.cdr.detectChanges();
    }
  }

  toggleAction(visitId: string, event: MouseEvent) {
    event.stopPropagation();
    this.activeActionId = this.activeActionId === visitId ? '' : visitId;
    this.cdr.detectChanges();
  }

  closeAction() {
    this.activeActionId = '';
    this.cdr.detectChanges();
  }

  openReschedule(v: Visit) {
    this.rescheduleVisitId = v.visitId;
    this.rescheduleVisitName = v.visitName;
    this.rescheduleDate = '';
    this.rescheduleDateError = '';
    this.showRescheduleModal = true;
  }

  private setRescheduleError(msg: string) {
    this.rescheduleDateError = msg;
    this.cdr.detectChanges();
    setTimeout(() => { this.rescheduleDateError = ''; this.cdr.detectChanges(); }, 5000);
  }

  saveReschedule() {
    this.rescheduleDateError = '';
    if (!this.rescheduleDate) { this.setRescheduleError('Please select a new date.'); return; }
    const selected = new Date(this.rescheduleDate);
    const today = new Date(); today.setHours(0, 0, 0, 0);
    if (selected < today) { this.setRescheduleError('Reschedule date cannot be in the past.'); return; }
    if (this.enrollmentWindowEnd && this.rescheduleDate < this.enrollmentWindowEnd) {
      this.setRescheduleError(`Date must be after the enrollment window closes (${this.enrollmentWindowEnd}).`); return;
    }
    if (this.protocolEndDate && this.rescheduleDate > this.protocolEndDate) {
      this.setRescheduleError(`Date cannot be after the protocol ends (${this.protocolEndDate}).`); return;
    }
    this.visitService.reschedule(this.rescheduleVisitId, { newDate: this.rescheduleDate }).subscribe({
      next: () => {
        this.showRescheduleModal = false;
        this.loadDetail();
        this.showToast('Visit rescheduled', 'amber');
      },
      error: (err: any) => console.error(err)
    });
  }

  confirmRemove(v: Visit) {
    const date = new Date(v.visitDate).toLocaleDateString('en-GB', {
      day: '2-digit', month: 'short', year: 'numeric'
    });
    this.confirmMessage = `Are you sure you want to remove "${v.visitName}" scheduled on ${date}? This action will mark the visit as Cancelled and cannot be undone.`;
    this.confirmAction = () => {
      this.visitService.cancel(v.visitId).subscribe({
        next: () => {
          this.showConfirmModal = false;
          this.loadDetail();
          this.showToast('Visit marked as Cancelled', 'grey');
        },
        error: (err: any) => console.error(err)
      });
    };
    this.showConfirmModal = true;
  }

  getBadgeClass(status: string): string {
    const map: any = {
      'SCHEDULED':   'b-scheduled',
      'COMPLETED':   'b-completed',
      'RESCHEDULED': 'b-rescheduled',
      'MISSED':      'b-missed',
      'CANCELLED':   'b-cancelled'
    };
    return map[status] || 'b-scheduled';
  }

  goBack() {
    this.trialsNav.setPending('visits');
    this.router.navigate(['/dashboard']);
  }

  showToast(msg: string, type = 'green') {
    this.toastMessage = msg;
    this.toastType = type;
    this.toastVisible = true;
    setTimeout(() => this.toastVisible = false, 3000);
    this.cdr.detectChanges();
  }
}


