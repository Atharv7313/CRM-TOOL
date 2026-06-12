import { Component, OnInit, Output, EventEmitter, ChangeDetectorRef, HostListener } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import {
  ProtocolService,
  ProtocolResponse,
  ProtocolSiteResponse,
  SiteResponse,
  InvestigatorResponse,
  UpdateProtocolRequest,
  AssignSiteRequest
} from '../../../services/protocol.service';

@Component({
  selector: 'app-protocols',
  standalone: false,
  templateUrl: './protocols.component.html',
  styleUrl: './protocols.component.css'
})
export class ProtocolsComponent implements OnInit {
  @Output() navigate = new EventEmitter<string>();

  protocols: ProtocolResponse[] = [];
  loading = false;
  error   = '';

  // Filters
  searchTitle  = '';
  filterStatus = '';
  filterPhase  = '';

  // Pagination
  currentPage = 1;
  readonly pageSize = 10;

  get totalPages(): number {
    return Math.ceil(this.filteredProtocols.length / this.pageSize);
  }

  get pagedProtocols(): ProtocolResponse[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.filteredProtocols.slice(start, start + this.pageSize);
  }

  get pageNumbers(): number[] {
    return Array.from({ length: this.totalPages }, (_, i) => i + 1);
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages) this.currentPage = page;
  }

  // View modal
  viewProtocol: ProtocolResponse | null = null;
  viewSites: ProtocolSiteResponse[] = [];
  viewSitesLoading  = false;
  removingSiteId    = '';   // tracks which assignment is being removed

  // Edit modal (now a centered popup instead of side panel)
  showEditModal    = false;
  editingProtocol: ProtocolResponse | null = null;
  editForm!: FormGroup;
  editLoading  = false;
  editError    = '';
  editSuccess  = '';

  // Delete confirm modal
  showDeleteModal  = false;
  deletingProtocol: ProtocolResponse | null = null;
  deleteLoading    = false;
  deleteError      = '';

  // Assign site modal
  showAssignModal  = false;
  assigningProtocol: ProtocolResponse | null = null;
  assignForm!: FormGroup;
  assignLoading    = false;
  assignError      = '';
  assignSuccess    = '';
  sites: SiteResponse[] = [];
  sitesLoading        = false;
  investigators: InvestigatorResponse[] = [];
  investigatorsLoading = false;

  showSiteDropdown = false;
  showInvDropdown  = false;

  get sortedSites(): SiteResponse[] {
    return [...this.sites].sort((a, b) => a.name.localeCompare(b.name));
  }

  get sortedInvestigators(): InvestigatorResponse[] {
    return [...this.investigators].sort((a, b) => a.name.localeCompare(b.name));
  }

  getSelectedSiteName(): string {
    const id = this.assignForm.get('siteId')?.value;
    if (!id) return 'Choose a research site...';
    const s = this.sites.find(x => x.siteId === id);
    return s ? `${s.name}${s.location ? ' — ' + s.location : ''}` : 'Choose a research site...';
  }

  getSelectedInvName(): string {
    const id = this.assignForm.get('investigatorUserId')?.value;
    if (!id) return 'Select investigator...';
    const inv = this.investigators.find(x => x.userId === id);
    return inv ? `${inv.name} (${inv.email})` : 'Select investigator...';
  }

  selectSite(s: SiteResponse): void {
    this.assignForm.patchValue({ siteId: s.siteId });
    this.assignForm.get('siteId')?.markAsTouched();
    this.showSiteDropdown = false;
  }

  selectInvestigator(inv: InvestigatorResponse): void {
    this.assignForm.patchValue({ investigatorUserId: inv.userId });
    this.assignForm.get('investigatorUserId')?.markAsTouched();
    this.showInvDropdown = false;
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(e: MouseEvent): void {
    const target = e.target as HTMLElement;
    if (!target.closest('.custom-select')) {
      this.showSiteDropdown = false;
      this.showInvDropdown  = false;
    }
  }


  constructor(
    private svc: ProtocolService,
    private fb:  FormBuilder,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.editForm = this.fb.group({
      title:       ['', [Validators.required, Validators.maxLength(200)]],
      phase:       ['', [Validators.required, Validators.maxLength(50)]],
      description: [''],
      startDate:   ['', Validators.required],
      endDate:     ['', Validators.required]
    });

    this.assignForm = this.fb.group({
      siteId:             ['', Validators.required],
      investigatorUserId: ['', Validators.required]
    });

    this.loadProtocols();
  }

  // ── Data ──────────────────────────────────────────────────────────────────

  loadProtocols(): void {
    this.loading = true;
    this.error   = '';
    this.svc.getProtocols().subscribe({
      next: data => { this.protocols = data; this.loading = false; this.cdr.detectChanges(); },
      error: ()   => { this.error = 'Could not load protocols. Is the service running?'; this.loading = false; this.cdr.detectChanges(); }
    });
  }

  // ── Filters ──────────────────────────────────────────────────────────────

  get filteredProtocols(): ProtocolResponse[] {
    let list = this.protocols;
    if (this.searchTitle.trim())
      list = list.filter(p => p.title.toLowerCase().includes(this.searchTitle.toLowerCase()));
    if (this.filterStatus)
      list = list.filter(p => p.status === this.filterStatus);
    if (this.filterPhase.trim())
      list = list.filter(p => p.phase.toLowerCase().includes(this.filterPhase.toLowerCase()));
    return list;
  }

  clearFilters(): void { this.searchTitle = ''; this.filterStatus = ''; this.filterPhase = ''; this.currentPage = 1; }

  get stats() {
    const all = this.protocols;
    return {
      total:     all.length,
      upcoming:  all.filter(p => p.status === 'UPCOMING').length,
      ongoing:   all.filter(p => p.status === 'ONGOING').length,
      completed: all.filter(p => p.status === 'COMPLETED').length
    };
  }

  // ── Navigation ────────────────────────────────────────────────────────────

  goCreate(): void { this.navigate.emit('create-protocol'); }

  // ── View modal ────────────────────────────────────────────────────────────

  openView(p: ProtocolResponse): void {
    this.viewProtocol     = p;
    this.viewSites        = [];
    this.viewSitesLoading = true;
    this.svc.getProtocolSites(p.protocolId).subscribe({
      next: s => { this.viewSites = s.filter(x => x.status !== 'CLOSED'); this.viewSitesLoading = false; this.cdr.detectChanges(); },
      error: () => { this.viewSitesLoading = false; this.cdr.detectChanges(); }
    });
  }

  closeView(): void { this.viewProtocol = null; this.viewSites = []; this.removingSiteId = ''; }

  removeSiteAssignment(assignment: ProtocolSiteResponse): void {
    // Works from both view modal (viewProtocol) and edit modal (editingProtocol)
    const protocol = this.viewProtocol ?? this.editingProtocol;
    if (!protocol || this.removingSiteId) return;
    this.removingSiteId = assignment.protocolSiteId;
    this.svc.removeAssignment(protocol.protocolId, assignment.protocolSiteId)
      .pipe(finalize(() => { this.removingSiteId = ''; this.cdr.detectChanges(); }))
      .subscribe({
        next: () => {
          // Just remove from local list — table refreshes only when modal is closed
          this.viewSites = this.viewSites.filter(s => s.protocolSiteId !== assignment.protocolSiteId);
          this.cdr.detectChanges();
        },
        error: () => {}
      });
  }

  // ── Edit modal ────────────────────────────────────────────────────────────

  openEdit(p: ProtocolResponse): void {
    this.editingProtocol = p;
    this.editError   = '';
    this.editSuccess = '';
    this.editForm.setValue({
      title:       p.title,
      phase:       p.phase,
      description: p.description ?? '',
      startDate:   p.startDate ? p.startDate.substring(0, 10) : '',
      endDate:     p.endDate   ? p.endDate.substring(0, 10)   : ''
    });
    this.showEditModal = true;
    // Load assigned sites for removal in edit modal
    this.viewSites = [];
    this.viewSitesLoading = true;
    this.svc.getProtocolSites(p.protocolId).subscribe({
      next: s => { this.viewSites = s.filter(x => x.status !== 'CLOSED'); this.viewSitesLoading = false; this.cdr.detectChanges(); },
      error: () => { this.viewSitesLoading = false; this.cdr.detectChanges(); }
    });
  }

  closeEdit(): void { this.showEditModal = false; this.editingProtocol = null; this.loadProtocols(); }

  /** Minimum allowed end date = chosen start date */
  get editMinEndDate(): string {
    return this.editForm.get('startDate')?.value || '';
  }

  submitEdit(): void {
    if (this.editForm.invalid || !this.editingProtocol) {
      this.editForm.markAllAsTouched();
      return;
    }
    this.editLoading = true;
    this.editError   = '';
    this.editSuccess = '';

    const { title, phase, description, startDate, endDate } = this.editForm.value;
    const dto: UpdateProtocolRequest = {
      title:       title.trim(),
      phase,
      description: description?.trim() || undefined,
      startDate,
      endDate
    };

    this.svc.updateProtocol(this.editingProtocol.protocolId, dto)
      .pipe(finalize(() => { this.editLoading = false; this.cdr.detectChanges(); }))
      .subscribe({
        next: () => {
          this.closeEdit();
          this.loadProtocols();
        },
        error: err => { this.editError = err.error?.message || 'Update failed. Please try again.'; }
      });
  }

  // ── Delete modal ──────────────────────────────────────────────────────────

  openDelete(p: ProtocolResponse): void {
    this.deletingProtocol = p;
    this.deleteError      = '';
    this.deleteLoading    = false;
    this.showDeleteModal  = true;
  }

  closeDelete(): void { this.showDeleteModal = false; this.deletingProtocol = null; }

  submitDelete(): void {
    if (!this.deletingProtocol) return;
    this.deleteLoading = true;
    this.deleteError   = '';
    this.svc.deleteProtocol(this.deletingProtocol.protocolId)
      .pipe(finalize(() => { this.deleteLoading = false; this.cdr.detectChanges(); }))
      .subscribe({
        next: () => { this.loadProtocols(); this.closeDelete(); },
        error: err => { this.deleteError = err.error?.message || 'Delete failed.'; }
      });
  }

  // ── Assign site modal ─────────────────────────────────────────────────────

  openAssign(p: ProtocolResponse): void {
    this.assigningProtocol = p;
    this.assignError       = '';
    this.assignSuccess     = '';
    this.assignForm.reset({ siteId: '', investigatorUserId: '' });
    this.showAssignModal   = true;
    this.showSiteDropdown  = false;
    this.showInvDropdown   = false;
    if (this.sites.length === 0)        this.loadSites();
    if (this.investigators.length === 0) this.loadInvestigators();
  }

  closeAssign(): void { this.showAssignModal = false; this.assigningProtocol = null; }

  loadSites(): void {
    this.sitesLoading = true;
    this.svc.getSites().subscribe({
      next: s => { this.sites = s; this.sitesLoading = false; this.cdr.detectChanges(); },
      error: () => { this.sitesLoading = false; this.cdr.detectChanges(); }
    });
  }

  loadInvestigators(): void {
    this.investigatorsLoading = true;
    this.svc.getInvestigators().subscribe({
      next: data => {
        this.investigators = data;
        this.investigatorsLoading = false;
        this.cdr.detectChanges();
      },
      error: () => { this.investigatorsLoading = false; this.cdr.detectChanges(); }
    });
  }

  submitAssign(): void {
    if (this.assignForm.invalid || !this.assigningProtocol) return;
    this.assignLoading = true;
    this.assignError   = '';
    const { siteId, investigatorUserId } = this.assignForm.value;
    const dto: AssignSiteRequest = { siteId, investigatorUserId };
    this.svc.assignSite(this.assigningProtocol.protocolId, dto)
      .pipe(finalize(() => { this.assignLoading = false; this.cdr.detectChanges(); }))
      .subscribe({
        next: () => {
          this.closeAssign();
          this.loadProtocols();
        },
        error: err => { this.assignError = err.error?.message || 'Assignment failed. Please try again.'; }
      });
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  statusBg(status: string): string {
    return ({ UPCOMING: '#dbeafe', ONGOING: '#dcfce7', COMPLETED: '#f1f5f9', DELETED: '#fee2e2' })[status] ?? '#f5f5f5';
  }

  statusFg(status: string): string {
    return ({ UPCOMING: '#1d4ed8', ONGOING: '#15803d', COMPLETED: '#475569', DELETED: '#991b1b' })[status] ?? '#555';
  }

  phaseBg(phase: string): string {
    const map: Record<string, string> = {
      'Phase 1': '#fdf4ff', 'Phase 2': '#f0fdf4', 'Phase 3': '#eff6ff', 'Phase 4': '#fff7ed',
      'Phase I': '#fdf4ff', 'Phase II': '#f0fdf4', 'Phase III': '#eff6ff', 'Phase IV': '#fff7ed'
    };
    return map[phase] ?? '#f5f5f5';
  }

  phaseFg(phase: string): string {
    const map: Record<string, string> = {
      'Phase 1': '#7e22ce', 'Phase 2': '#15803d', 'Phase 3': '#1d4ed8', 'Phase 4': '#c2410c',
      'Phase I': '#7e22ce', 'Phase II': '#15803d', 'Phase III': '#1d4ed8', 'Phase IV': '#c2410c'
    };
    return map[phase] ?? '#555';
  }

  fmtDate(d?: string): string {
    if (!d) return '—';
    return new Date(d).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
  }

  assignStatusBg(s: string): string {
    return ({ ACTIVE: '#dcfce7', INACTIVE: '#fef9c3', CLOSED: '#f1f5f9' })[s] ?? '#f5f5f5';
  }
  assignStatusFg(s: string): string {
    return ({ ACTIVE: '#15803d', INACTIVE: '#a16207', CLOSED: '#64748b' })[s] ?? '#555';
  }
}
