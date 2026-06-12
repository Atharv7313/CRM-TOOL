import { Component, OnInit, Output, EventEmitter, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { ProtocolService, SiteResponse, ProtocolSiteResponse, UpdateSiteRequest } from '../../../services/protocol.service';

@Component({
  selector: 'app-sites',
  standalone: false,
  templateUrl: './sites.component.html',
  styleUrl: './sites.component.css'
})
export class SitesComponent implements OnInit {
  @Output() navigate = new EventEmitter<string>();

  sites: SiteResponse[] = [];
  loading = false;
  error   = '';

  searchName     = '';
  searchLocation = '';

  // Pagination
  currentPage = 1;
  readonly pageSize = 12;

  get totalPages(): number {
    return Math.ceil(this.filteredSites.length / this.pageSize);
  }
  get pageNumbers(): number[] {
    return Array.from({ length: this.totalPages }, (_, i) => i + 1);
  }
  get pagedSites(): SiteResponse[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.filteredSites.slice(start, start + this.pageSize);
  }
  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages) this.currentPage = page;
  }

  // View modal
  viewSite: SiteResponse | null = null;
  viewProtocols: ProtocolSiteResponse[] = [];
  viewProtocolsLoading = false;

  // Edit slide panel
  showEditPanel  = false;
  editingSite: SiteResponse | null = null;
  editForm!: FormGroup;
  editLoading  = false;
  editError    = '';
  editSuccess  = '';

  // Delete confirm modal
  showDeleteModal = false;
  deletingSite: SiteResponse | null = null;
  deleteLoading   = false;
  deleteError     = '';

  constructor(
    private svc: ProtocolService,
    private fb:  FormBuilder,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.editForm = this.fb.group({
      name:     ['', [Validators.required, Validators.maxLength(150)]],
      location: ['', Validators.maxLength(255)]
    });
    this.loadSites();
  }

  loadSites(): void {
    this.loading = true;
    this.error   = '';
    this.currentPage = 1;
    this.svc.getSites().subscribe({
      next: data => { this.sites = data; this.loading = false; this.cdr.detectChanges(); },
      error: ()   => { this.error = 'Could not load sites. Is the service running?'; this.loading = false; this.cdr.detectChanges(); }
    });
  }

  /** Sort sites newest-first replicating SQL Server's uniqueidentifier byte comparison.
   *  Groups 4 & 5 are big-endian (compare as-is).
   *  Groups 1, 2, 3 are little-endian (reverse byte pairs before comparing). */
  private revPairs(hex: string): string {
    return (hex.match(/../g) ?? []).reverse().join('');
  }

  private sortByNewest(list: SiteResponse[]): SiteResponse[] {
    return list.sort((a, b) => {
      const ap = a.siteId.toUpperCase().split('-');
      const bp = b.siteId.toUpperCase().split('-');
      // Group 5 — bytes 10-15, big-endian, primary key
      if (ap[4] !== bp[4]) return bp[4].localeCompare(ap[4]);
      // Group 4 — bytes 8-9, big-endian
      if (ap[3] !== bp[3]) return bp[3].localeCompare(ap[3]);
      // Group 3 — bytes 6-7, little-endian → reverse pairs
      const r3a = this.revPairs(ap[2]), r3b = this.revPairs(bp[2]);
      if (r3a !== r3b) return r3b.localeCompare(r3a);
      // Group 2 — bytes 4-5, little-endian → reverse pairs
      const r2a = this.revPairs(ap[1]), r2b = this.revPairs(bp[1]);
      if (r2a !== r2b) return r2b.localeCompare(r2a);
      // Group 1 — bytes 0-3, little-endian → reverse pairs
      const r1a = this.revPairs(ap[0]), r1b = this.revPairs(bp[0]);
      return r1b.localeCompare(r1a);
    });
  }

  get filteredSites(): SiteResponse[] {
    let list = this.sortByNewest([...this.sites]);
    if (this.searchName.trim())
      list = list.filter(s => s.name.toLowerCase().includes(this.searchName.toLowerCase()));
    if (this.searchLocation.trim())
      list = list.filter(s => s.location?.toLowerCase().includes(this.searchLocation.toLowerCase()));
    return list;
  }

  clearFilters(): void { this.searchName = ''; this.searchLocation = ''; this.currentPage = 1; }

  get stats() {
    return { total: this.sites.length, active: this.sites.filter(s => s.protocolCount > 0).length };
  }

  goCreate(): void { this.navigate.emit('create-site'); }

  // View
  openView(s: SiteResponse): void {
    this.viewSite = s;
    this.viewProtocols = [];
    this.viewProtocolsLoading = true;
    this.svc.getSiteProtocols(s.siteId).subscribe({
      next: data => { this.viewProtocols = data; this.viewProtocolsLoading = false; this.cdr.detectChanges(); },
      error: ()   => { this.viewProtocolsLoading = false; this.cdr.detectChanges(); }
    });
  }
  closeView(): void { this.viewSite = null; this.viewProtocols = []; }

  // Edit
  openEdit(s: SiteResponse): void {
    this.editingSite = s;
    this.editError   = '';
    this.editSuccess = '';
    this.editForm.setValue({ name: s.name, location: s.location ?? '' });
    this.showEditPanel = true;
  }
  closeEdit(): void { this.showEditPanel = false; this.editingSite = null; }

  submitEdit(): void {
    if (this.editForm.invalid || !this.editingSite) return;
    this.editLoading = true;
    this.editError   = '';
    this.editSuccess = '';
    const dto: UpdateSiteRequest = {
      name: this.editForm.value.name.trim(),
      location: this.editForm.value.location?.trim() || undefined
    };
    this.svc.updateSite(this.editingSite.siteId, dto)
      .pipe(finalize(() => { this.editLoading = false; this.cdr.detectChanges(); }))
      .subscribe({
        next: () => {
          this.editSuccess = 'Site updated successfully.';
          this.loadSites();
          setTimeout(() => { this.closeEdit(); }, 1800);
        },
        error: err => { this.editError = err.error?.message || 'Update failed.'; }
      });
  }

  // Delete
  openDelete(s: SiteResponse): void {
    this.deletingSite  = s;
    this.deleteError   = '';
    this.deleteLoading = false;
    this.showDeleteModal = true;
  }
  closeDelete(): void { this.showDeleteModal = false; this.deletingSite = null; }

  submitDelete(): void {
    if (!this.deletingSite) return;
    this.deleteLoading = true;
    this.svc.deleteSite(this.deletingSite.siteId)
      .pipe(finalize(() => { this.deleteLoading = false; this.cdr.detectChanges(); }))
      .subscribe({
        next: () => { this.loadSites(); this.closeDelete(); },
        error: err => { this.deleteError = err.error?.message || 'Delete failed.'; }
      });
  }

  initials(name: string): string { return name ? name.substring(0, 2).toUpperCase() : 'SI'; }

  assignBg(s: string): string {
    return ({ ACTIVE: '#dcfce7', INACTIVE: '#fef9c3', CLOSED: '#fee2e2' })[s] ?? '#f5f5f5';
  }
  assignFg(s: string): string {
    return ({ ACTIVE: '#15803d', INACTIVE: '#a16207', CLOSED: '#991b1b' })[s] ?? '#555';
  }
  getProtocolsByStatus(status: string): ProtocolSiteResponse[] {
    return this.viewProtocols.filter(p => p.status === status);
  }
}
