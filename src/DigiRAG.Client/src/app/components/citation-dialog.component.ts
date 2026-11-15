import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Citation } from '../models';

@Component({
  selector: 'app-citation-dialog',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="backdrop" *ngIf="citation" (click)="close.emit()">
      <div class="dialog" (click)="$event.stopPropagation()" role="dialog" aria-modal="true">
        <div class="head">
          <div class="titles">
            <h3>{{ citation.documentName }}</h3>
            <p class="sub">
              <span *ngIf="citation.pageNumber">Page {{ citation.pageNumber }}</span>
              <span *ngIf="citation.pageNumber && citation.headingPath" class="dot">·</span>
              <span *ngIf="citation.headingPath">{{ citation.headingPath }}</span>
              <span *ngIf="!citation.pageNumber && !citation.headingPath">Source passage</span>
            </p>
          </div>
          <button class="close" (click)="close.emit()" aria-label="Close">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"
                 stroke-linecap="round" aria-hidden="true">
              <path d="M18 6 6 18M6 6l12 12" />
            </svg>
          </button>
        </div>

        <blockquote class="excerpt">{{ citation.excerpt }}</blockquote>
      </div>
    </div>
  `,
  styles: [`
    .backdrop {
      position: fixed; inset: 0; z-index: 60;
      background: rgba(5, 8, 13, 0.7);
      display: grid; place-items: center; padding: 20px;
    }

    .dialog {
      width: 100%; max-width: 560px;
      background: var(--bg-raised);
      border: 1px solid var(--border-strong);
      border-radius: var(--radius-lg);
      box-shadow: var(--shadow);
      padding: 18px 20px 20px;
    }

    .head { display: flex; align-items: flex-start; gap: 12px; margin-bottom: 14px; }
    .titles { flex: 1; min-width: 0; }
    .head h3 {
      font-size: 15px; font-weight: 600; color: var(--text);
      overflow: hidden; text-overflow: ellipsis; white-space: nowrap;
    }
    .sub { font-size: 12px; color: var(--text-faint); margin-top: 2px; }
    .dot { margin: 0 5px; }

    .close {
      display: grid; place-items: center; flex: none;
      width: 26px; height: 26px; border-radius: var(--radius-sm);
      color: var(--text-faint); transition: background 0.15s, color 0.15s;
    }
    .close:hover { background: var(--bg-hover); color: var(--text); }
    .close svg { width: 15px; height: 15px; }

    .excerpt {
      background: var(--bg-base);
      border-left: 2px solid var(--accent);
      border-radius: 0 var(--radius-sm) var(--radius-sm) 0;
      padding: 13px 15px;
      font-size: 13.5px; line-height: 1.65; color: var(--text-muted);
      max-height: 320px; overflow-y: auto;
      white-space: pre-wrap; overflow-wrap: anywhere;
    }
  `]
})
export class CitationDialogComponent {
  @Input() citation: Citation | null = null;
  @Output() close = new EventEmitter<void>();
}
