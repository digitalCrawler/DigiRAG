import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DocumentItem, formatBytes, statusClass, statusLabel } from '../models';

@Component({
  selector: 'app-document-panel',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section class="panel">
      <div class="panel-head">
        <h2>Documents</h2>
        <span class="count" *ngIf="documents.length">{{ documents.length }}</span>
      </div>

      <label class="dropzone" [class.busy]="isUploading">
        <input type="file" (change)="onFileSelected($event)"
               accept=".pdf,.docx,.txt,.md" [disabled]="isUploading" />
        <svg *ngIf="!isUploading" viewBox="0 0 24 24" fill="none" stroke="currentColor"
             stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
          <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4M17 8l-5-5-5 5M12 3v12" />
        </svg>
        <span *ngIf="!isUploading"><strong>Upload a document</strong><br />PDF, DOCX, TXT or MD</span>
        <span *ngIf="isUploading" class="busy-text">Uploading and queuing ingestion…</span>
      </label>

      <p *ngIf="documents.length === 0" class="notice">No documents yet.</p>

      <ul class="doc-list">
        <li *ngFor="let doc of documents" class="doc">
          <div class="doc-top">
            <span class="doc-name" [title]="doc.fileName">{{ doc.fileName }}</span>
            <button class="remove" (click)="delete.emit(doc.id)" [attr.aria-label]="'Delete ' + doc.fileName">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"
                   stroke-linecap="round" aria-hidden="true">
                <path d="M18 6 6 18M6 6l12 12" />
              </svg>
            </button>
          </div>
          <div class="doc-bottom">
            <span class="badge" [ngClass]="statusClass(doc.status)">{{ statusLabel(doc.status) }}</span>
            <span class="meta">{{ formatBytes(doc.fileSizeBytes) }} · {{ doc.chunkCount }} chunks</span>
          </div>
          <p *ngIf="doc.status === 4 && doc.errorMessage" class="doc-error" [title]="doc.errorMessage">
            {{ doc.errorMessage }}
          </p>
        </li>
      </ul>
    </section>
  `,
  styles: [`
    .panel { padding: 14px 12px; display: flex; flex-direction: column; min-height: 0; }

    .panel-head {
      display: flex; align-items: center; gap: 8px;
      margin-bottom: 10px; padding: 0 4px;
    }
    .panel-head h2 {
      font-size: 11px; font-weight: 600; letter-spacing: 0.07em;
      text-transform: uppercase; color: var(--text-faint);
    }
    .count {
      font-size: 11px; color: var(--text-faint);
      background: var(--bg-input); border-radius: 20px; padding: 1px 7px;
    }

    .dropzone {
      display: flex; flex-direction: column; align-items: center; gap: 7px;
      padding: 16px 12px; margin-bottom: 12px;
      border: 1px dashed var(--border-strong);
      border-radius: var(--radius);
      color: var(--text-faint); font-size: 12px; text-align: center;
      cursor: pointer; transition: border-color 0.15s, background 0.15s, color 0.15s;
    }
    .dropzone:hover { border-color: var(--accent); background: var(--accent-soft); color: var(--text-muted); }
    .dropzone.busy { cursor: progress; border-style: solid; border-color: var(--accent); }
    .dropzone input { display: none; }
    .dropzone svg { width: 20px; height: 20px; }
    .dropzone strong { color: var(--text); font-weight: 550; }
    .busy-text { color: var(--accent); }

    .notice { font-size: 12.5px; color: var(--text-faint); padding: 4px; }

    .doc-list {
      list-style: none; display: flex; flex-direction: column; gap: 6px;
      overflow-y: auto; min-height: 0;
    }

    .doc {
      background: var(--bg-raised);
      border: 1px solid var(--border);
      border-radius: var(--radius-sm);
      padding: 9px 10px;
    }

    .doc-top { display: flex; align-items: center; gap: 8px; }
    .doc-name {
      flex: 1; min-width: 0; font-size: 12.5px; color: var(--text);
      overflow: hidden; text-overflow: ellipsis; white-space: nowrap;
    }

    .remove {
      display: grid; place-items: center; width: 20px; height: 20px; flex: none;
      border-radius: 4px; color: var(--text-faint); transition: background 0.15s, color 0.15s;
    }
    .remove:hover { background: rgba(242, 112, 109, 0.14); color: var(--danger); }
    .remove svg { width: 13px; height: 13px; }

    .doc-bottom { display: flex; align-items: center; gap: 8px; margin-top: 6px; }
    .meta { font-size: 11px; color: var(--text-faint); }

    .badge {
      font-size: 10.5px; font-weight: 600; letter-spacing: 0.03em;
      padding: 1px 7px; border-radius: 20px;
      background: var(--bg-input); color: var(--text-muted);
    }
    .status-queued { background: rgba(245, 181, 68, 0.14); color: var(--warning); }
    .status-processing { background: rgba(77, 142, 247, 0.16); color: var(--accent); }
    .status-indexed { background: rgba(62, 207, 142, 0.14); color: var(--success); }
    .status-failed { background: rgba(242, 112, 109, 0.14); color: var(--danger); }

    .doc-error {
      margin-top: 6px; font-size: 11px; color: var(--danger);
      display: -webkit-box; -webkit-line-clamp: 3; -webkit-box-orient: vertical;
      overflow: hidden; line-height: 1.45;
    }
  `]
})
export class DocumentPanelComponent {
  @Input() documents: DocumentItem[] = [];
  @Input() isUploading = false;

  @Output() upload = new EventEmitter<File>();
  @Output() delete = new EventEmitter<string>();

  readonly statusLabel = statusLabel;
  readonly statusClass = statusClass;
  readonly formatBytes = formatBytes;

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length) return;
    this.upload.emit(input.files[0]);
    // Reset so selecting the same file twice still fires a change event.
    input.value = '';
  }
}
