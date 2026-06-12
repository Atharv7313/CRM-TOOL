import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { Router } from '@angular/router';
import { EnrollmentService } from '../../../services/enrollment.service';
import { VisitService } from '../../../services/visit.service';
import { Protocol } from '../../../models/enrollment';
import { TrialsNavService } from '../../../services/trials-nav.service';

interface VisitRow {
  visitName: string;
  visitDate: string;
  nameError: string;
  dateError: string;
}

interface ExistingVisit {
  visitName: string;
  visitDate: string;
  visitStatus: string;
}

@Component({
  selector: 'app-protocol-visits',
  standalone: false,
  templateUrl: './protocol-visits.html',
  styleUrls: ['./protocol-visits.css']
})
export class ProtocolVisits implements OnInit {

  protocols: Protocol[] = [];
  selectedProtocolId = '';
  selectedProtocol: Protocol | null = null;
  activePatientCount = 0;
  saving = false;

  existingVisits: ExistingVisit[] = [];
  existingDates = new Set<string>();

  visitRows: VisitRow[] = [
    { visitName: '', visitDate: '', nameError: '', dateError: '' }
  ];

  toastVisible = false;
  toastMessage = '';
  toastType = 'green';

  constructor(
    private enrollmentService: EnrollmentService,
    private visitService: VisitService,
    private router: Router,
    private cdr: ChangeDetectorRef,
    private trialsNav: TrialsNavService
  ) {}

  ngOnInit() {
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
    this.selectedProtocol = this.protocols.find(
      p => p.protocolId === this.selectedProtocolId
    ) || null;

    this.visitRows = [{ visitName: '', visitDate: '', nameError: '', dateError: '' }];
    this.activePatientCount = 0;
    this.existingVisits = [];
    this.existingDates = new Set<string>();

    if (!this.selectedProtocolId) return;

    this.enrollmentService.getActivePatientCount(this.selectedProtocolId).subscribe({
      next: (res: any) => {
        this.activePatientCount = res.data;
        this.cdr.detectChanges();
      },
      error: (err: any) => console.error(err)
    });

    this.loadExistingVisits();
  }

  loadExistingVisits() {
    if (!this.selectedProtocol) return;
    const title = this.selectedProtocol.title;

    this.visitService.getFiltered(undefined, undefined, undefined).subscribe({
      next: (res: any) => {
        const all: any[] = res.data || [];
        // Deduplicate by visitName+visitDate for this protocol
        const seen = new Set<string>();
        const filtered: ExistingVisit[] = [];
        for (const v of all) {
          if (v.protocolTitle !== title) continue;
          const key = v.visitName + '|' + v.visitDate?.substring(0, 10);
          if (!seen.has(key)) {
            seen.add(key);
            filtered.push({
              visitName: v.visitName,
              visitDate: v.visitDate?.substring(0, 10),
              visitStatus: v.visitStatus
            });
          }
        }
        // Sort by date
        filtered.sort((a, b) => a.visitDate.localeCompare(b.visitDate));
        this.existingVisits = filtered;
        this.existingDates = new Set(
          filtered.filter(v => v.visitStatus !== 'CANCELLED').map(v => v.visitDate)
        );
        this.cdr.detectChanges();
      },
      error: (err: any) => console.error(err)
    });
  }

  // ── Enrollment window calculations ──
  get enrollmentWindowEnd(): Date | null {
    if (!this.selectedProtocol?.endDate) return null;
    // Enrollment allowed until protocol end date
    return new Date(this.selectedProtocol.endDate);
  }

  get isEnrollmentOpen(): boolean {
    if (!this.enrollmentWindowEnd) return true;
    return new Date() <= this.enrollmentWindowEnd;
  }

  get daysUntilClose(): number {
    if (!this.enrollmentWindowEnd) return 0;
    const diff = this.enrollmentWindowEnd.getTime() - new Date().getTime();
    return Math.max(0, Math.ceil(diff / (1000 * 60 * 60 * 24)));
  }

  get minVisitDate(): string {
    // Allow scheduling from today or the protocol start date, whichever is later
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const protocolStart = this.selectedProtocol?.startDate
      ? new Date(this.selectedProtocol.startDate)
      : today;
    const base = protocolStart > today ? protocolStart : today;
    const yyyy = base.getFullYear();
    const mm   = String(base.getMonth() + 1).padStart(2, '0');
    const dd   = String(base.getDate()).padStart(2, '0');
    return `${yyyy}-${mm}-${dd}`;
  }

  get maxVisitDate(): string {
    if (!this.selectedProtocol?.endDate) return '';
    // Use local date parts to avoid UTC timezone shift
    const d = new Date(this.selectedProtocol.endDate);
    const yyyy = d.getFullYear();
    const mm   = String(d.getMonth() + 1).padStart(2, '0');
    const dd   = String(d.getDate()).padStart(2, '0');
    return `${yyyy}-${mm}-${dd}`;
  }

  addVisitRow() {
    this.visitRows.push({ visitName: '', visitDate: '', nameError: '', dateError: '' });
  }

  removeVisitRow(index: number) {
    if (this.visitRows.length === 1) return;
    this.visitRows.splice(index, 1);
  }

  private validate(): boolean {
    let valid = true;
    const seenCombos = new Set<string>();
    const seenDates  = new Set<string>();

    for (let i = 0; i < this.visitRows.length; i++) {
      const row = this.visitRows[i];
      row.nameError = '';
      row.dateError = '';

      if (!row.visitName.trim()) {
        row.nameError = 'Visit name is required.';
        valid = false;
      }

      if (!row.visitDate) {
        row.dateError = 'Visit date is required.';
        valid = false;
      } else {
        const visitDate = new Date(row.visitDate);

        // Must be today or later (within the active protocol period)
        const today = new Date();
        today.setHours(0, 0, 0, 0);
        if (visitDate < today) {
          row.dateError = 'Visit date cannot be in the past.';
          valid = false;
        }

        // Cannot exceed protocol end date
        if (this.selectedProtocol?.endDate && !row.dateError) {
          const endDate = new Date(this.selectedProtocol.endDate);
          endDate.setHours(0, 0, 0, 0);
          if (visitDate > endDate) {
            row.dateError = `Visit date cannot exceed the protocol end date (${
              endDate.toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' })
            }).`;
            valid = false;
          }
        }

        // Ascending order
        if (i > 0 && this.visitRows[i - 1].visitDate && row.visitDate && !row.dateError) {
          const prevDate = new Date(this.visitRows[i - 1].visitDate);
          const currDate = new Date(row.visitDate);
          if (currDate <= prevDate) {
            row.dateError = 'Date must be after the previous visit.';
            valid = false;
          }
        }

        // Duplicate date in existing scheduled visits
        if (row.visitDate && !row.dateError && this.existingDates.has(row.visitDate)) {
          row.dateError = 'You have already created a visit on this date.';
          valid = false;
        }

        // Duplicate date in current batch
        if (row.visitDate && !row.dateError) {
          if (seenDates.has(row.visitDate)) {
            row.dateError = 'Each visit must have a unique date.';
            valid = false;
          } else {
            seenDates.add(row.visitDate);
          }
        }
      }

      // Duplicate name+date in batch
      const combo = `${row.visitName.trim().toLowerCase()}|${row.visitDate}`;
      if (row.visitName && row.visitDate) {
        if (seenCombos.has(combo)) {
          row.nameError = 'Duplicate visit name and date in this batch.';
          valid = false;
        } else {
          seenCombos.add(combo);
        }
      }
    }

    this.cdr.detectChanges();

    // Auto-clear all row errors after 5 seconds
    if (!valid) {
      setTimeout(() => {
        this.visitRows.forEach(r => { r.nameError = ''; r.dateError = ''; });
        this.cdr.detectChanges();
      }, 5000);
    }

    return valid;
  }

  saveSchedule() {
    if (!this.validate()) return;
    this.saving = true;

    this.visitService.bulkSchedule({
      protocolId: this.selectedProtocolId,
      visits: this.visitRows.map(r => ({
        visitName: r.visitName.trim(),
        visitDate: r.visitDate
      }))
    }).subscribe({
      next: (res: any) => {
        this.saving = false;
        this.showToast(res.data || 'Visits scheduled successfully', 'green');
        this.visitRows = [{ visitName: '', visitDate: '', nameError: '', dateError: '' }];
        this.loadExistingVisits();
        this.cdr.detectChanges();
      },
      error: (err: any) => {
        this.saving = false;
        this.showToast(err.error?.message || 'Failed to schedule visits.', 'red');
        console.error(err);
        this.cdr.detectChanges();
      }
    });
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


