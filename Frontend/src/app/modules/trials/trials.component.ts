import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-trials',
  standalone: false,
  templateUrl: './trials.component.html',
  styleUrl: './trials.component.css'
})
export class TrialsComponent {
  @Input() role = '';
}
