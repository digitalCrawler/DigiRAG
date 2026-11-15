import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { KnowledgeBase } from '../models';

@Component({
  selector: 'app-knowledge-base-panel',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <section class="panel">
      <div class="panel-head">
        <h2>Knowledge bases</h2>
        <button class="add" (click)="startCreate()" title="New knowledge base">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"
               stroke-linecap="round" aria-hidden="true">
            <path d="M12 5v14M5 12h14" />
          </svg>
        </button>
      </div>

      <p *ngIf="error" class="notice error">{{ error }}</p>

      <p *ngIf="!error && knowledgeBases.length === 0" class="notice">
        No knowledge bases yet. Create one to get started.
      </p>

      <ul class="kb-list" *ngIf="knowledgeBases.length > 0">
        <li *ngFor="let kb of knowledgeBases">
          <button class="kb" [class.active]="kb.id === selectedKbId" (click)="select.emit(kb.id)">
            <span class="kb-name">{{ kb.name }}</span>
            <span class="kb-desc">{{ kb.description || 'General documents' }}</span>
          </button>
        </li>
      </ul>
    </section>

    <!-- Create dialog -->
    <div class="backdrop" *ngIf="creating" (click)="cancelCreate()">
      <div class="dialog" (click)="$event.stopPropagation()">
        <h3>New knowledge base</h3>
        <label for="kb-name">Name</label>
        <input id="kb-name" type="text" [(ngModel)]="draftName" placeholder="e.g. HR Policies" />
        <label for="kb-desc">Description</label>
        <textarea id="kb-desc" rows="3" [(ngModel)]="draftDescription"
                  placeholder="What lives in this knowledge base?"></textarea>
        <div class="dialog-actions">
          <button class="btn secondary" (click)="cancelCreate()">Cancel</button>
          <button class="btn primary" [disabled]="!draftName.trim()" (click)="submitCreate()">Create</button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .panel { padding: 14px 12px; border-bottom: 1px solid var(--border); }

    .panel-head {
      display: flex; align-items: center; justify-content: space-between;
      margin-bottom: 10px; padding: 0 4px;
    }
    .panel-head h2 {
      font-size: 11px; font-weight: 600; letter-spacing: 0.07em;
      text-transform: uppercase; color: var(--text-faint);
    }

    .add {
      display: grid; place-items: center; width: 24px; height: 24px;
      border-radius: var(--radius-sm); color: var(--text-muted);
      transition: background 0.15s, color 0.15s;
    }
    .add:hover { background: var(--bg-hover); color: var(--text); }
    .add svg { width: 15px; height: 15px; }

    .notice { font-size: 12.5px; color: var(--text-faint); padding: 6px 4px; line-height: 1.5; }
    .notice.error {
      color: var(--danger);
      border-left: 2px solid var(--danger);
      padding-left: 10px;
    }

    .kb-list { list-style: none; display: flex; flex-direction: column; gap: 2px; }

    .kb {
      display: flex; flex-direction: column; gap: 1px;
      width: 100%; text-align: left; padding: 8px 10px;
      border-radius: var(--radius-sm);
      border-left: 2px solid transparent;
      transition: background 0.15s, border-color 0.15s;
    }
    .kb:hover { background: var(--bg-hover); }
    .kb.active { background: var(--accent-soft); border-left-color: var(--accent); }

    .kb-name { font-size: 13.5px; font-weight: 500; color: var(--text); }
    .kb.active .kb-name { color: var(--accent); }
    .kb-desc {
      font-size: 11.5px; color: var(--text-faint);
      overflow: hidden; text-overflow: ellipsis; white-space: nowrap;
    }

    /* Dialog */
    .backdrop {
      position: fixed; inset: 0; z-index: 50;
      background: rgba(5, 8, 13, 0.7);
      display: grid; place-items: center; padding: 20px;
    }
    .dialog {
      width: 100%; max-width: 420px;
      background: var(--bg-raised);
      border: 1px solid var(--border-strong);
      border-radius: var(--radius-lg);
      box-shadow: var(--shadow);
      padding: 20px;
      display: flex; flex-direction: column; gap: 8px;
    }
    .dialog h3 { font-size: 15px; font-weight: 600; margin-bottom: 4px; }
    .dialog label { font-size: 12px; color: var(--text-faint); margin-top: 4px; }
    .dialog input, .dialog textarea {
      background: var(--bg-input);
      border: 1px solid var(--border);
      border-radius: var(--radius-sm);
      padding: 9px 11px; color: var(--text); width: 100%; resize: vertical;
    }
    .dialog input:focus, .dialog textarea:focus { border-color: var(--accent); outline: none; }

    .dialog-actions { display: flex; justify-content: flex-end; gap: 8px; margin-top: 12px; }

    .btn {
      padding: 8px 15px; border-radius: var(--radius-sm);
      font-size: 13px; font-weight: 500; transition: background 0.15s;
    }
    .btn.secondary { background: var(--bg-input); color: var(--text-muted); }
    .btn.secondary:hover { background: var(--bg-hover); color: var(--text); }
    .btn.primary { background: var(--accent); color: #fff; }
    .btn.primary:hover:enabled { background: var(--accent-hover); }
    .btn.primary:disabled { opacity: 0.45; cursor: not-allowed; }
  `]
})
export class KnowledgeBasePanelComponent {
  @Input() knowledgeBases: KnowledgeBase[] = [];
  @Input() selectedKbId = '';
  @Input() error = '';

  @Output() select = new EventEmitter<string>();
  @Output() create = new EventEmitter<{ name: string; description: string }>();

  creating = false;
  draftName = '';
  draftDescription = '';

  startCreate(): void {
    this.draftName = '';
    this.draftDescription = '';
    this.creating = true;
  }

  cancelCreate(): void {
    this.creating = false;
  }

  submitCreate(): void {
    if (!this.draftName.trim()) return;
    this.create.emit({ name: this.draftName.trim(), description: this.draftDescription.trim() });
    this.creating = false;
  }
}
