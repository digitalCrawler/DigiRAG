import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from './api.service';
import { ChatMessageUI, Citation, DocumentItem, KnowledgeBase, Tenant } from './models';
import { AppHeaderComponent } from './components/app-header.component';
import { KnowledgeBasePanelComponent } from './components/knowledge-base-panel.component';
import { DocumentPanelComponent } from './components/document-panel.component';
import { ChatPanelComponent } from './components/chat-panel.component';
import { CitationDialogComponent } from './components/citation-dialog.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    AppHeaderComponent,
    KnowledgeBasePanelComponent,
    DocumentPanelComponent,
    ChatPanelComponent,
    CitationDialogComponent
  ],
  template: `
    <div class="shell">
      <app-header
        [tenants]="tenants()"
        [selectedTenantId]="selectedTenantId()"
        (tenantChange)="onTenantChange($event)"
        (newTenant)="promptNewTenant()">
      </app-header>

      <div class="body">
        <aside class="sidebar">
          <app-knowledge-base-panel
            [knowledgeBases]="knowledgeBases()"
            [selectedKbId]="selectedKbId()"
            [error]="apiError()"
            (select)="selectKb($event)"
            (create)="createKnowledgeBase($event)">
          </app-knowledge-base-panel>

          <app-document-panel
            *ngIf="selectedKbId()"
            [documents]="documents()"
            [isUploading]="isUploading()"
            (upload)="uploadDocument($event)"
            (delete)="deleteDocument($event)">
          </app-document-panel>
        </aside>

        <main class="main">
          <app-chat-panel
            [messages]="chatMessages()"
            [isGenerating]="isGenerating()"
            [scopeName]="selectedKbName()"
            (send)="sendMessage($event)"
            (clear)="chatMessages.set([])"
            (openCitation)="activeCitation.set($event)">
          </app-chat-panel>
        </main>
      </div>

      <app-citation-dialog
        [citation]="activeCitation()"
        (close)="activeCitation.set(null)">
      </app-citation-dialog>
    </div>
  `,
  styles: [`
    .shell { display: flex; flex-direction: column; height: 100vh; overflow: hidden; }

    .body { flex: 1; display: flex; min-height: 0; }

    .sidebar {
      width: var(--sidebar-width);
      flex: none;
      display: flex; flex-direction: column;
      background: var(--bg-panel);
      border-right: 1px solid var(--border);
      overflow-y: auto;
    }

    .main { flex: 1; min-width: 0; }

    @media (max-width: 860px) {
      .sidebar { width: 240px; }
    }
  `]
})
export class AppComponent implements OnInit {
  private readonly api = inject(ApiService);

  readonly tenants = signal<Tenant[]>([]);
  readonly selectedTenantId = signal<string>('');

  readonly knowledgeBases = signal<KnowledgeBase[]>([]);
  readonly selectedKbId = signal<string>('');

  readonly documents = signal<DocumentItem[]>([]);
  readonly isUploading = signal<boolean>(false);

  readonly chatMessages = signal<ChatMessageUI[]>([]);
  readonly isGenerating = signal<boolean>(false);

  readonly activeCitation = signal<Citation | null>(null);

  /** Surfaces API failures instead of leaving the UI looking merely empty. */
  readonly apiError = signal<string>('');

  ngOnInit(): void {
    this.loadTenants();
  }

  selectedKbName(): string {
    return this.knowledgeBases().find(k => k.id === this.selectedKbId())?.name ?? '';
  }

  // --- Tenants ------------------------------------------------------------

  loadTenants(): void {
    this.apiError.set('');
    this.api.getTenants().subscribe({
      next: list => {
        this.tenants.set(list);
        if (list.length > 0 && !this.selectedTenantId()) {
          this.selectedTenantId.set(list[0].id);
          this.loadKnowledgeBases();
        }
      },
      error: err => {
        // Never invent a tenant here. An earlier fallback fabricated one client-side,
        // so a completely unreachable API looked like a working app with no data.
        this.tenants.set([]);
        this.apiError.set(`Cannot reach the API at ${this.api.apiUrl} (${err.status || 'network error'}).`);
      }
    });
  }

  onTenantChange(tenantId: string): void {
    this.selectedTenantId.set(tenantId);
    this.selectedKbId.set('');
    this.documents.set([]);
    this.loadKnowledgeBases();
  }

  promptNewTenant(): void {
    const name = prompt('Organisation / tenant name:');
    if (!name?.trim()) return;
    const slug = name.toLowerCase().replace(/[^a-z0-9]/g, '-');
    this.api.createTenant(name.trim(), slug).subscribe({
      next: tenant => {
        this.tenants.update(list => [...list, tenant]);
        this.onTenantChange(tenant.id);
      },
      error: err => alert(`Could not create tenant: ${ApiService.describeError(err)}`)
    });
  }

  // --- Knowledge bases ----------------------------------------------------

  loadKnowledgeBases(): void {
    const tenantId = this.selectedTenantId();
    if (!tenantId) return;

    this.api.getKnowledgeBases(tenantId).subscribe({
      next: list => {
        this.apiError.set('');
        this.knowledgeBases.set(list);
        if (list.length > 0) {
          this.selectKb(list[0].id);
        } else {
          this.selectedKbId.set('');
          this.documents.set([]);
        }
      },
      error: err => {
        // Without this, a failed request was indistinguishable from "none exist".
        this.knowledgeBases.set([]);
        this.apiError.set(`Failed to load knowledge bases (${err.status || 'network error'}).`);
      }
    });
  }

  selectKb(kbId: string): void {
    this.selectedKbId.set(kbId);
    this.loadDocuments();
  }

  createKnowledgeBase(draft: { name: string; description: string }): void {
    this.api.createKnowledgeBase(this.selectedTenantId(), draft.name, draft.description).subscribe({
      next: kb => {
        this.knowledgeBases.update(list => [...list, kb]);
        this.selectKb(kb.id);
      },
      error: err => alert(`Could not create knowledge base: ${ApiService.describeError(err)}`)
    });
  }

  // --- Documents ----------------------------------------------------------

  loadDocuments(): void {
    const kbId = this.selectedKbId();
    if (!kbId) return;
    this.api.getDocuments(this.selectedTenantId(), kbId).subscribe({
      next: docs => this.documents.set(docs)
    });
  }

  uploadDocument(file: File): void {
    this.isUploading.set(true);
    this.api.uploadDocument(this.selectedTenantId(), this.selectedKbId(), file).subscribe({
      next: () => {
        this.isUploading.set(false);
        this.loadDocuments();
      },
      error: err => {
        this.isUploading.set(false);
        alert(`Upload failed: ${ApiService.describeError(err)}`);
      }
    });
  }

  deleteDocument(documentId: string): void {
    if (!confirm('Delete this document and its vector embeddings?')) return;
    this.api.deleteDocument(this.selectedTenantId(), documentId).subscribe({
      next: () => this.loadDocuments()
    });
  }

  // --- Chat ---------------------------------------------------------------

  async sendMessage(question: string): Promise<void> {
    if (this.isGenerating()) return;

    this.chatMessages.update(msgs => [...msgs, { role: 'user', content: question }]);
    const index = this.chatMessages().length;
    this.chatMessages.update(msgs => [
      ...msgs,
      { role: 'assistant', content: '', citations: [], isStreaming: true }
    ]);
    this.isGenerating.set(true);

    const patch = (change: (msg: ChatMessageUI) => void) => {
      this.chatMessages.update(msgs => {
        const copy = [...msgs];
        copy[index] = { ...copy[index] };
        change(copy[index]);
        return copy;
      });
    };

    try {
      await this.api.streamChat(this.selectedTenantId(), this.selectedKbId() || null, question, {
        onToken: text => patch(m => (m.content += text)),
        onCitations: citations => patch(m => (m.citations = citations)),
        onError: message => patch(m => (m.errorText = message))
      });
    } catch (err: any) {
      patch(m => (m.errorText = err?.message ?? 'Unknown error'));
    } finally {
      this.isGenerating.set(false);
      patch(m => (m.isStreaming = false));
    }
  }
}
