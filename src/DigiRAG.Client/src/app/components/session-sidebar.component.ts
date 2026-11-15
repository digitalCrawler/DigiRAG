import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ChatSession } from '../models';

@Component({
  selector: 'app-session-sidebar',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="wrap">
      <button class="new-chat" (click)="newChat.emit()">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"
             stroke-linecap="round" aria-hidden="true">
          <path d="M12 5v14M5 12h14" />
        </svg>
        <span>New chat</span>
      </button>

      <p *ngIf="sessions.length === 0" class="empty">No conversations yet.</p>

      <ul class="list">
        <li *ngFor="let s of sessions">
          <button class="item" [class.active]="s.id === activeSessionId" (click)="select.emit(s.id)">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7"
                 stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
              <path d="M21 11.5a8.4 8.4 0 0 1-9 8.4 8.4 8.4 0 0 1-3.8-.9L3 21l1.9-5.2A8.4 8.4 0 0 1 12 3a8.4 8.4 0 0 1 9 8.5Z" />
            </svg>
            <span class="label">{{ s.title }}</span>
          </button>
        </li>
      </ul>
    </div>
  `,
  styles: [`
    .wrap { padding: 12px; display: flex; flex-direction: column; gap: 10px; min-height: 0; }

    .new-chat {
      display: flex; align-items: center; gap: 8px; width: 100%;
      padding: 9px 12px; border-radius: var(--radius);
      border: 1px solid var(--border-strong);
      font-size: 13.5px; font-weight: 500; color: var(--text);
      transition: background 0.15s, border-color 0.15s;
    }
    .new-chat:hover { background: var(--bg-hover); border-color: var(--accent); }
    .new-chat svg { width: 15px; height: 15px; }

    .empty { font-size: 12.5px; color: var(--text-faint); padding: 4px; }

    .list { list-style: none; display: flex; flex-direction: column; gap: 1px; overflow-y: auto; min-height: 0; }

    .item {
      display: flex; align-items: center; gap: 9px; width: 100%; text-align: left;
      padding: 8px 10px; border-radius: var(--radius-sm);
      font-size: 13px; color: var(--text-muted);
      transition: background 0.15s, color 0.15s;
    }
    .item:hover { background: var(--bg-hover); color: var(--text); }
    .item.active { background: var(--accent-soft); color: var(--accent); }
    .item svg { width: 14px; height: 14px; flex: none; opacity: 0.75; }

    .label { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
  `]
})
export class SessionSidebarComponent {
  @Input() sessions: ChatSession[] = [];
  @Input() activeSessionId = '';

  @Output() newChat = new EventEmitter<void>();
  @Output() select = new EventEmitter<string>();
}
