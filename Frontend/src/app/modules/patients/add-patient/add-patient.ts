import { Component, ChangeDetectorRef } from '@angular/core';
import { Router } from '@angular/router';
import { PatientService } from '../../../services/patient.service';
import { TrialsNavService } from '../../../services/trials-nav.service';

interface CsvRow {
  name: string;
  dateOfBirth: string;
  contactInfo: string;
}

interface SkippedRow {
  line: number;
  name: string;
  reason: string;
}

interface ImportSummary {
  success: number;
  duplicateRows: string[];
  missingFieldRows: number;
}

@Component({
  selector: 'app-add-patient',
  standalone: false,
  templateUrl: './add-patient.html',
  styleUrls: ['./add-patient.css']
})
export class AddPatient {

  name = '';
  dateOfBirth = '';
  contactInfo = '';
  errorMessage = '';

  showBulkModal = false;
  csvPreview: CsvRow[] = [];
  csvParseError = '';
  parsedSkipped = 0;
  skippedRows: SkippedRow[] = [];
  selectedFileName = '';
  importing = false;
  importDone = 0;
  importTotal = 0;
  importSummary: ImportSummary | null = null;

  /** Yesterday's date as YYYY-MM-DD — prevents selecting today or future */
  get maxDob(): string {
    const d    = new Date();
    d.setDate(d.getDate() - 1);
    const yyyy = d.getFullYear();
    const mm   = String(d.getMonth() + 1).padStart(2, '0');
    const dd   = String(d.getDate()).padStart(2, '0');
    return `${yyyy}-${mm}-${dd}`;
  }

  constructor(
    private patientService: PatientService,
    private router: Router,
    private cdr: ChangeDetectorRef,
    private trialsNav: TrialsNavService
  ) {}

  private showError(msg: string) {
    this.errorMessage = msg;
    setTimeout(() => { this.errorMessage = ''; }, 5000);
  }

  save() {
    this.errorMessage = '';

    if (!this.name.trim())            { this.showError('Name is required.'); return; }
    if (!this.dateOfBirth)            { this.showError('Date of birth is required.'); return; }
    const dob = new Date(this.dateOfBirth);
    const today = new Date(); today.setHours(0, 0, 0, 0);
    if (dob >= today)                 { this.showError('Date of birth cannot be today or a future date.'); return; }
    if (!this.contactInfo.trim())     { this.showError('Contact info (email) is required.'); return; }
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(this.contactInfo.trim())) { this.showError('Please enter a valid email address.'); return; }

    this.patientService.create({
      name: this.name.trim(),
      dateOfBirth: this.dateOfBirth,
      contactInfo: this.contactInfo.trim().toLowerCase()
    }).subscribe({
      next: () => this.trialsNav.go('patients-list'),
      error: (err: any) => this.showError(err.error?.message || 'Failed to add patient.')
    });
  }

  closeBulkModal() {
    this.showBulkModal = false;
    this.csvPreview = [];
    this.csvParseError = '';
    this.parsedSkipped = 0;
    this.skippedRows = [];
    this.selectedFileName = '';
    this.importing = false;
    this.importDone = 0;
    this.importTotal = 0;
    this.importSummary = null;
  }

  onFileSelected(event: any) {
    this.csvParseError = '';
    this.csvPreview = [];
    this.parsedSkipped = 0;
    this.skippedRows = [];
    this.importSummary = null;
    this.selectedFileName = '';

    const file: File = event.target.files[0];
    if (!file) return;

    this.selectedFileName = file.name;

    if (!file.name.endsWith('.csv')) {
      this.csvParseError = 'Please select a valid .csv file.';
      return;
    }

    const reader = new FileReader();
    reader.onload = (e: any) => {
      const result = this.parseCsv(e.target.result as string);
      if (result.error) {
        this.csvParseError = result.error;
      } else {
        this.csvPreview    = result.rows;
        this.parsedSkipped = result.skipped;
        this.skippedRows   = result.skippedRows;
      }
      this.cdr.detectChanges();
    };
    reader.readAsText(file);
  }

  private parseCsv(text: string): { rows: CsvRow[]; skipped: number; skippedRows: SkippedRow[]; error?: string } {
    const lines = text.trim().split('\n').map(l => l.trim()).filter(l => l);

    if (lines.length < 2) {
      return { rows: [], skipped: 0, skippedRows: [], error: 'CSV must have a header row and at least one data row.' };
    }

    const header = lines[0].toLowerCase().split(',').map(h => h.trim());
    const nameIdx  = header.indexOf('name');
    const dobIdx   = header.indexOf('dateofbirth');
    const emailIdx = header.indexOf('contactinfo');

    if (nameIdx === -1 || dobIdx === -1 || emailIdx === -1) {
      return { rows: [], skipped: 0, skippedRows: [], error: 'CSV must have columns: name, dateOfBirth, contactInfo' };
    }

    const rows: CsvRow[] = [];
    const skippedRows: SkippedRow[] = [];
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    const todayMidnight = new Date(); todayMidnight.setHours(0, 0, 0, 0);

    for (let i = 1; i < lines.length; i++) {
      const cols = lines[i].split(',').map(c => c.trim());
      const name        = cols[nameIdx] || '';
      const dateOfBirth = cols[dobIdx]  || '';
      const contactInfo = cols[emailIdx] ? cols[emailIdx].toLowerCase() : '';

      if (!name && !dateOfBirth && !contactInfo) continue;

      const label = name || `Row ${i}`;

      if (!name || !dateOfBirth || !contactInfo) {
        skippedRows.push({ line: i, name: label, reason: 'Missing required fields' });
        continue;
      }

      if (!emailRegex.test(contactInfo)) {
        skippedRows.push({ line: i, name: label, reason: 'Invalid email address' });
        continue;
      }

      const dobRegex = /^\d{4}-\d{2}-\d{2}$/;
      if (!dobRegex.test(dateOfBirth)) {
        skippedRows.push({ line: i, name: label, reason: 'Invalid date format (use YYYY-MM-DD)' });
        continue;
      }

      const [year, month, day] = dateOfBirth.split('-').map(Number);

      if (month < 1 || month > 12) {
        skippedRows.push({ line: i, name: label, reason: 'Invalid month in date of birth' });
        continue;
      }

      const daysInMonth = new Date(year, month, 0).getDate();
      if (day < 1 || day > daysInMonth) {
        skippedRows.push({ line: i, name: label, reason: 'Invalid day in date of birth' });
        continue;
      }

      const dobDate = new Date(year, month - 1, day);
      if (dobDate >= todayMidnight) {
        skippedRows.push({ line: i, name: label, reason: 'Date of birth cannot be today or a future date' });
        continue;
      }

      rows.push({ name, dateOfBirth, contactInfo });
    }

    if (rows.length === 0 && skippedRows.length === 0) {
      return { rows: [], skipped: 0, skippedRows: [], error: 'No valid rows found in the CSV.' };
    }

    return { rows, skipped: skippedRows.length, skippedRows };
  }

  importCsv() {
    if (this.csvPreview.length === 0) return;

    this.importing = true;
    this.importDone = 0;
    this.importTotal = this.csvPreview.length;
    this.importSummary = null;

    let success = 0;
    let failed = 0;
    const duplicateRows: string[] = [];
    const total = this.csvPreview.length;
    const missingFieldRows = this.parsedSkipped;

    this.csvPreview.forEach((row) => {
      this.patientService.create({
        name: row.name,
        dateOfBirth: row.dateOfBirth,
        contactInfo: row.contactInfo
      }).subscribe({
        next: () => {
          success++;
          this.importDone++;
          this.finishIfDone(success, failed, duplicateRows, missingFieldRows, total);
          this.cdr.detectChanges();
        },
        error: () => {
          failed++;
          this.importDone++;
          duplicateRows.push(row.name);
          this.finishIfDone(success, failed, duplicateRows, missingFieldRows, total);
          this.cdr.detectChanges();
        }
      });
    });
  }

  private finishIfDone(
    success: number,
    failed: number,
    duplicateRows: string[],
    missingFieldRows: number,
    total: number
  ) {
    if (success + failed === total) {
      this.importing = false;
      this.importSummary = { success, duplicateRows, missingFieldRows };
      this.csvPreview = [];
      this.cdr.detectChanges();
    }
  }

  clearForm() {
    this.name = '';
    this.dateOfBirth = '';
    this.contactInfo = '';
    this.errorMessage = '';
  }

  goToDashboard() {
    this.closeBulkModal();
    this.trialsNav.go('patients-list');
  }
}


