import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Tenant } from '../models';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <header class="header">
      <div class="brand">
        <span class="mark" aria-hidden="true">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"
               stroke-linecap="round" stroke-linejoin="round">
            <path d="M12 3 3 7.5v9L12 21l9-4.5v-9L12 3Z" />
            <path d="m3 7.5 9 4.5 9-4.5M12 21v-9" />
          </svg>
        </span>
        <span class="title">DigiRAG</span>
      </div>

      <div class="actions">
        <div class="tenant">
          <label for="tenant-select">Tenant</label>
          <select id="tenant-select"
                  [ngModel]="selectedTenantId"
                  (ngModelChange)="tenantChange.emit($event)"
                  [disabled]="tenants.length === 0">
            <option *ngFor="let t of tenants" [value]="t.id">{{ t.name }}</option>
            <option *ngIf="tenants.length === 0" value="">No tenants</option>
          </select>
          <button class="ghost" (click)="newTenant.emit()" title="Create a tenant">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"
                 stroke-linecap="round" aria-hidden="true">
              <path d="M12 5v14M5 12h14" />
            </svg>
            <span>Tenant</span>
          </button>
        </div>

        <a class="ghost" href="/hangfire" target="_blank" rel="noopener" title="Background jobs">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"
               stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
            <path d="M13 2 3 14h8l-1 8 10-12h-8l1-8Z" />
          </svg>
          <span>Jobs</span>
        </a>
      </div>
    </header>
  `,
  styles: [`
    .header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 16px;
      height: 56px;
      padding: 0 18px;
      background: var(--bg-panel);
      border-bottom: 1px solid var(--border);
      flex: none;
    }

    .brand { display: flex; align-items: center; gap: 10px; min-width: 0; }

    .mark {
      display: grid;
      place-items: center;
      width: 30px;
      height: 30px;
      border-radius: 8px;
      background: var(--accent-soft);
      color: var(--accent);
      flex: none;
    }
    .mark svg { width: 17px; height: 17px; }

    .title { font-size: 15px; font-weight: 650; letter-spacing: -0.01em; }

    .actions { display: flex; align-items: center; gap: 10px; }

    .tenant { display: flex; align-items: center; gap: 8px; }
    .tenant label { font-size: 12px; color: var(--text-faint); }

    .tenant select {
      background: var(--bg-input);
      border: 1px solid var(--border);
      color: var(--text);
      padding: 6px 10px;
      border-radius: var(--radius-sm);
      max-width: 190px;
      font-size: 13px;
    }
    .tenant select:hover:enabled { border-color: var(--border-strong); }
    .tenant select:disabled { opacity: 0.55; }

    .ghost {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      padding: 6px 11px;
      font-size: 13px;
      font-weight: 500;
      color: var(--text-muted);
      background: var(--bg-input);
      border: 1px solid var(--border);
      border-radius: var(--radius-sm);
      text-decoration: none;
      transition: background 0.15s, color 0.15s, border-color 0.15s;
    }
    .ghost:hover { background: var(--bg-hover); color: var(--text); border-color: var(--border-strong); }
    .ghost svg { width: 15px; height: 15px; }

    @media (max-width: 720px) {
      .tenant label, .ghost span { display: none; }
      .ghost { padding: 6px 8px; }
    }
  `]
})
export class AppHeaderComponent {
  @Input() tenants: Tenant[] = [];
  @Input() selectedTenantId = '';

  @Output() tenantChange = new EventEmitter<string>();
  @Output() newTenant = new EventEmitter<void>();
}
