import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

const STORAGE_KEY = 'trials_pending_view';

@Injectable({ providedIn: 'root' })
export class TrialsNavService {
  private navSubject = new BehaviorSubject<string>('');
  nav$ = this.navSubject.asObservable();

  go(view: string)  { this.navSubject.next(view); }
  goHome()          { this.navSubject.next('home'); }
  goVisits()        { this.navSubject.next('visits'); }

  /** Call before navigating away to a separate page (e.g. visit-detail) */
  setPending(view: string) { sessionStorage.setItem(STORAGE_KEY, view); }

  /** Dashboard calls this on init to restore tab after a full navigation */
  consumePending(): string {
    const v = sessionStorage.getItem(STORAGE_KEY) || '';
    sessionStorage.removeItem(STORAGE_KEY);
    return v;
  }
}
