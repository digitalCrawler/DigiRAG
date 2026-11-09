import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { environment } from '../environments/environment';

export interface Tenant {
  id: string;
  name: string;
  slug: string;
}

export interface KnowledgeBase {
  id: string;
  name: string;
  description?: string;
}

export interface DocumentItem {
  id: string;
  fileName: string;
  fileSizeBytes: number;
  status: number; // 0=Uploaded, 1=Queued, 2=Processing, 3=Indexed, 4=Failed
  chunkCount: number;
  errorMessage?: string;
  createdAtUtc: string;
}

export interface Citation {
  documentId: string;
  chunkId: string;
  documentName: string;
  pageNumber?: number;
  headingPath?: string;
  excerpt: string;
}

export interface ChatMessageUI {
  role: 'user' | 'assistant';
  content: string;
  citations?: Citation[];
  isStreaming?: boolean;
}

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="rag-container">
      <!-- Top Navigation Bar -->
      <header class="rag-header">
        <div class="logo-area">
          <div class="logo-badge">RAG</div>
          <div>
            <h1>DigiRAG Enterprise</h1>
            <span class="subtext">.NET 9 • PostgreSQL pgvector • Local/Gemini/OpenAI</span>
          </div>
        </div>

        <div class="header-actions">
          <div class="tenant-selector">
            <label>Tenant:</label>
            <select [ngModel]="selectedTenantId()" (ngModelChange)="onTenantChange($event)">
              <option *ngFor="let t of tenants()" [value]="t.id">{{ t.name }}</option>
            </select>
            <button class="btn-sm btn-secondary" (click)="promptNewTenant()">+ Tenant</button>
          </div>

          <a href="http://localhost:5000/hangfire" target="_blank" class="hangfire-link">
            ⚡ Hangfire Dashboard
          </a>
        </div>
      </header>

      <!-- Main Dual Panel Layout -->
      <div class="rag-body">
        <!-- Sidebar: Knowledge Bases & Uploads -->
        <aside class="sidebar">
          <div class="panel-section">
            <div class="section-header">
              <h2>Knowledge Bases</h2>
              <button class="btn-sm btn-primary" (click)="showNewKbModal = true">+ KB</button>
            </div>

            <div class="kb-list">
              <div 
                *ngFor="let kb of knowledgeBases()" 
                class="kb-item" 
                [class.active]="selectedKbId() === kb.id"
                (click)="selectKb(kb.id)">
                <div class="kb-title">{{ kb.name }}</div>
                <div class="kb-desc">{{ kb.description || 'General documents' }}</div>
              </div>
              <div *ngIf="apiError()" class="empty-state api-error">
                {{ apiError() }}
              </div>
              <div *ngIf="!apiError() && knowledgeBases().length === 0" class="empty-state">
                No Knowledge Bases found. Create one to begin.
              </div>
            </div>
          </div>

          <!-- Document Upload & Management Section -->
          <div class="panel-section" *ngIf="selectedKbId()">
            <div class="section-header">
              <h2>Documents</h2>
              <label class="btn-sm btn-accent upload-btn">
                <span>+ Upload</span>
                <input type="file" (change)="onFileSelected($event)" accept=".pdf,.docx,.txt,.md" style="display:none;" />
              </label>
            </div>

            <div class="upload-dropzone" [class.uploading]="isUploading()">
              <span *ngIf="!isUploading()">Upload PDF, Word (DOCX) or Markdown</span>
              <span *ngIf="isUploading()" class="spinner-text">⏳ Uploading and queuing ingestion...</span>
            </div>

            <div class="document-list">
              <div *ngFor="let doc of documents()" class="doc-card">
                <div class="doc-main">
                  <span class="doc-icon">📄</span>
                  <div class="doc-info">
                    <div class="doc-name" [title]="doc.fileName">{{ doc.fileName }}</div>
                    <div class="doc-meta">
                      {{ formatBytes(doc.fileSizeBytes) }} • {{ doc.chunkCount }} chunks
                    </div>
                  </div>
                </div>

                <div class="doc-status-badge" [ngClass]="getStatusClass(doc.status)">
                  {{ getStatusText(doc.status) }}
                </div>

                <button class="btn-icon delete-btn" (click)="deleteDocument(doc.id)" title="Delete Document">✕</button>
              </div>

              <div *ngIf="documents().length === 0" class="empty-state">
                No documents uploaded yet.
              </div>
            </div>
          </div>
        </aside>

        <!-- Main Chat Area -->
        <main class="chat-main">
          <div class="chat-header">
            <div>
              <h3>Grounded Retrieval & Chat</h3>
              <p class="chat-kb-label">
                Scope: <span class="badge">{{ getSelectedKbName() }}</span>
              </p>
            </div>
            <button class="btn-sm btn-secondary" (click)="clearChat()">Clear Chat</button>
          </div>

          <!-- Messages Stream Feed -->
          <div class="messages-container" #messagesContainer>
            <div *ngIf="chatMessages().length === 0" class="welcome-box">
              <div class="welcome-icon">🔍</div>
              <h4>Ask anything about your knowledge base</h4>
              <p>Answers are grounded strictly in your indexed documents, complete with exact page citations and anti-hallucination guardrails.</p>
            </div>

            <div *ngFor="let msg of chatMessages()" class="message-bubble" [class.user]="msg.role === 'user'" [class.assistant]="msg.role === 'assistant'">
              <div class="msg-avatar">{{ msg.role === 'user' ? '👤' : '🤖' }}</div>
              <div class="msg-body">
                <div class="msg-content">{{ msg.content }}</div>

                <!-- Verified Citations / Evidence Section -->
                <div *ngIf="msg.citations && msg.citations.length > 0" class="citations-box">
                  <div class="citations-title">Verified Citations ({{ msg.citations.length }}):</div>
                  <div class="citations-grid">
                    <div *ngFor="let c of msg.citations" class="citation-pill" (click)="previewCitation(c)">
                      <span class="cite-doc">📄 {{ c.documentName }}</span>
                      <span *ngIf="c.pageNumber" class="cite-page">p. {{ c.pageNumber }}</span>
                      <span *ngIf="c.headingPath" class="cite-heading">[{{ c.headingPath }}]</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- Chat Input -->
          <div class="chat-input-bar">
            <textarea 
              [(ngModel)]="userInput" 
              (keydown.enter)="onEnterPress($event)" 
              placeholder="Ask a question about your documents... (Shift+Enter for newline)"
              rows="2"
              [disabled]="isGenerating()"></textarea>
            <button class="btn-primary send-btn" (click)="sendMessage()" [disabled]="!userInput.trim() || isGenerating()">
              <span *ngIf="!isGenerating()">Send</span>
              <span *ngIf="isGenerating()">...</span>
            </button>
          </div>
        </main>
      </div>

      <!-- New KB Modal -->
      <div class="modal-backdrop" *ngIf="showNewKbModal">
        <div class="modal-card">
          <h3>Create Knowledge Base</h3>
          <input type="text" [(ngModel)]="newKbName" placeholder="Knowledge Base Name (e.g. HR Policies)" class="input-field" />
          <textarea [(ngModel)]="newKbDesc" placeholder="Description..." rows="3" class="input-field"></textarea>
          <div class="modal-actions">
            <button class="btn-secondary" (click)="showNewKbModal = false">Cancel</button>
            <button class="btn-primary" (click)="createKnowledgeBase()">Create</button>
          </div>
        </div>
      </div>

      <!-- Citation Excerpt Modal -->
      <div class="modal-backdrop" *ngIf="activeCitationModal">
        <div class="modal-card">
          <h3>Citation Source Preview</h3>
          <p><strong>Document:</strong> {{ activeCitationModal.documentName }}</p>
          <p *ngIf="activeCitationModal.pageNumber"><strong>Page:</strong> {{ activeCitationModal.pageNumber }}</p>
          <p *ngIf="activeCitationModal.headingPath"><strong>Heading:</strong> {{ activeCitationModal.headingPath }}</p>
          <div class="excerpt-quote">
            "{{ activeCitationModal.excerpt }}"
          </div>
          <div class="modal-actions">
            <button class="btn-primary" (click)="activeCitationModal = null">Close</button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .rag-container { display: flex; flex-direction: column; height: 100vh; background-color: #0b1120; color: #f8fafc; font-family: 'Inter', sans-serif; }
    .rag-header { display: flex; justify-content: space-between; align-items: center; padding: 12px 24px; background-color: #111827; border-bottom: 1px solid #1f2937; }
    .logo-area { display: flex; align-items: center; gap: 12px; }
    .logo-badge { background: linear-gradient(135deg, #0284c7, #38bdf8); color: white; font-weight: 800; padding: 6px 12px; border-radius: 8px; font-size: 14px; }
    .rag-header h1 { font-size: 18px; font-weight: 700; margin: 0; }
    .subtext { font-size: 11px; color: #94a3b8; }
    .header-actions { display: flex; align-items: center; gap: 20px; }
    .tenant-selector { display: flex; align-items: center; gap: 8px; font-size: 13px; }
    .tenant-selector select { background: #1f2937; border: 1px solid #374151; color: #f8fafc; padding: 6px 10px; border-radius: 6px; }
    .hangfire-link { background: #1e1b4b; border: 1px solid #4338ca; color: #a5b4fc; text-decoration: none; padding: 6px 12px; border-radius: 6px; font-size: 12px; font-weight: 600; }
    .hangfire-link:hover { background: #312e81; }

    .rag-body { display: flex; flex: 1; overflow: hidden; }
    .sidebar { width: 340px; background-color: #0f172a; border-right: 1px solid #1e293b; display: flex; flex-direction: column; gap: 16px; padding: 16px; overflow-y: auto; }
    .panel-section { background: #1e293b; border-radius: 8px; padding: 14px; border: 1px solid #334155; }
    .section-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 12px; }
    .section-header h2 { font-size: 14px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px; color: #cbd5e1; }

    .kb-list { display: flex; flex-direction: column; gap: 8px; }
    .kb-item { padding: 10px; border-radius: 6px; background: #0f172a; border: 1px solid #334155; cursor: pointer; transition: all 0.2s; }
    .kb-item:hover { border-color: #38bdf8; }
    .kb-item.active { border-color: #38bdf8; background: #075985; }
    .kb-title { font-weight: 600; font-size: 14px; }
    .kb-desc { font-size: 11px; color: #94a3b8; margin-top: 2px; }
    .empty-state { font-size: 12px; color: #94a3b8; padding: 4px 0; line-height: 1.5; }
    .api-error { color: #f87171; border-left: 3px solid #f87171; padding-left: 10px; }

    .upload-dropzone { border: 2px dashed #475569; border-radius: 6px; padding: 12px; text-align: center; font-size: 12px; color: #94a3b8; margin-bottom: 12px; }
    .document-list { display: flex; flex-direction: column; gap: 8px; }
    .doc-card { display: flex; align-items: center; justify-content: space-between; background: #0f172a; padding: 8px 10px; border-radius: 6px; border: 1px solid #334155; gap: 6px; }
    .doc-main { display: flex; align-items: center; gap: 8px; overflow: hidden; flex: 1; }
    .doc-icon { font-size: 16px; }
    .doc-name { font-size: 13px; font-weight: 500; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; max-width: 140px; }
    .doc-meta { font-size: 10px; color: #94a3b8; }
    .doc-status-badge { font-size: 10px; font-weight: 700; padding: 2px 6px; border-radius: 4px; text-transform: uppercase; }
    .status-queued { background: #78350f; color: #fde68a; }
    .status-processing { background: #1e3a8a; color: #93c5fd; }
    .status-indexed { background: #064e3b; color: #6ee7b7; }
    .status-failed { background: #7f1d1d; color: #fca5a5; }

    .chat-main { flex: 1; display: flex; flex-direction: column; background-color: #0b1120; }
    .chat-header { display: flex; justify-content: space-between; align-items: center; padding: 12px 24px; border-bottom: 1px solid #1e293b; background: #0f172a; }
    .chat-header h3 { font-size: 16px; font-weight: 600; }
    .chat-kb-label { font-size: 12px; color: #94a3b8; margin-top: 2px; }
    .badge { background: #0284c7; color: white; padding: 2px 8px; border-radius: 12px; font-weight: 600; }

    .messages-container { flex: 1; overflow-y: auto; padding: 24px; display: flex; flex-direction: column; gap: 16px; }
    .welcome-box { text-align: center; margin: auto; max-width: 480px; padding: 32px; background: #1e293b; border-radius: 12px; border: 1px solid #334155; }
    .welcome-icon { font-size: 36px; margin-bottom: 12px; }
    .welcome-box h4 { font-size: 18px; margin-bottom: 8px; font-weight: 600; }
    .welcome-box p { font-size: 13px; color: #94a3b8; line-height: 1.6; }

    .message-bubble { display: flex; gap: 12px; max-width: 85%; }
    .message-bubble.user { margin-left: auto; flex-direction: row-reverse; }
    .msg-avatar { font-size: 20px; width: 32px; height: 32px; display: flex; align-items: center; justify-content: center; background: #1e293b; border-radius: 50%; flex-shrink: 0; }
    .message-bubble.user .msg-body { background: #0284c7; color: white; border-radius: 12px 12px 0 12px; }
    .message-bubble.assistant .msg-body { background: #1e293b; color: #f8fafc; border-radius: 12px 12px 12px 0; border: 1px solid #334155; }
    .msg-body { padding: 12px 16px; font-size: 14px; line-height: 1.6; }
    .msg-content { white-space: pre-wrap; }

    .citations-box { margin-top: 12px; padding-top: 10px; border-top: 1px solid #334155; font-size: 11px; }
    .citations-title { font-weight: 700; color: #38bdf8; margin-bottom: 6px; }
    .citations-grid { display: flex; flex-wrap: wrap; gap: 6px; }
    .citation-pill { background: #0f172a; border: 1px solid #38bdf8; border-radius: 12px; padding: 3px 8px; cursor: pointer; display: flex; gap: 4px; align-items: center; transition: background 0.2s; }
    .citation-pill:hover { background: #0369a1; }
    .cite-doc { font-weight: 600; color: #f8fafc; }
    .cite-page { color: #fbbf24; }
    .cite-heading { color: #94a3b8; font-style: italic; }

    .chat-input-bar { padding: 16px 24px; background: #0f172a; border-top: 1px solid #1e293b; display: flex; gap: 12px; }
    .chat-input-bar textarea { flex: 1; background: #1e293b; border: 1px solid #334155; border-radius: 8px; padding: 10px 14px; font-size: 14px; resize: none; outline: none; }
    .chat-input-bar textarea:focus { border-color: #38bdf8; }

    .btn-primary { background: #0284c7; color: white; border: none; padding: 8px 16px; border-radius: 6px; font-weight: 600; }
    .btn-primary:hover { background: #0369a1; }
    .btn-secondary { background: #334155; color: #f8fafc; border: none; padding: 8px 16px; border-radius: 6px; }
    .btn-secondary:hover { background: #475569; }
    .btn-sm { padding: 4px 8px; font-size: 12px; border-radius: 4px; border: none; }
    .btn-accent { background: #38bdf8; color: #0f172a; font-weight: 700; }
    .btn-icon { background: none; border: none; color: #64748b; font-size: 12px; }
    .btn-icon:hover { color: #f87171; }

    .modal-backdrop { position: fixed; inset: 0; background: rgba(0,0,0,0.7); display: flex; align-items: center; justify-content: center; z-index: 100; }
    .modal-card { background: #1e293b; border: 1px solid #475569; border-radius: 12px; padding: 24px; width: 440px; display: flex; flex-direction: column; gap: 14px; }
    .input-field { background: #0f172a; border: 1px solid #334155; border-radius: 6px; padding: 8px 12px; color: white; width: 100%; }
    .modal-actions { display: flex; justify-content: flex-end; gap: 10px; margin-top: 8px; }
    .excerpt-quote { background: #0f172a; border-left: 3px solid #38bdf8; padding: 12px; font-style: italic; font-size: 13px; color: #cbd5e1; max-height: 200px; overflow-y: auto; }
  `]
})
export class AppComponent implements OnInit {
  apiUrl = environment.apiUrl;

  tenants = signal<Tenant[]>([]);
  selectedTenantId = signal<string>('');

  // Surfaces API failures instead of leaving the UI looking merely empty.
  apiError = signal<string>('');
  
  knowledgeBases = signal<KnowledgeBase[]>([]);
  selectedKbId = signal<string>('');
  
  documents = signal<DocumentItem[]>([]);
  isUploading = signal<boolean>(false);

  chatMessages = signal<ChatMessageUI[]>([]);
  userInput = '';
  isGenerating = signal<boolean>(false);

  showNewKbModal = false;
  newKbName = '';
  newKbDesc = '';

  activeCitationModal: Citation | null = null;

  constructor(private http: HttpClient) {}

  ngOnInit() {
    this.loadTenants();
  }

  getHeaders(): HttpHeaders {
    return new HttpHeaders({
      'X-Tenant-Id': this.selectedTenantId()
    });
  }

  loadTenants() {
    this.apiError.set('');
    this.http.get<Tenant[]>(`${this.apiUrl}/tenants`).subscribe({
      next: (list) => {
        this.tenants.set(list);
        if (list.length > 0 && !this.selectedTenantId()) {
          this.selectedTenantId.set(list[0].id);
          this.loadKnowledgeBases();
        }
      },
      error: (err) => {
        // Do NOT invent a tenant here. The previous fallback fabricated "Acme Corporation"
        // client-side, so a totally unreachable API looked like a working app with an empty
        // knowledge base - which is exactly what made this hard to diagnose.
        this.tenants.set([]);
        this.apiError.set(`Cannot reach the API at ${this.apiUrl} (${err.status || 'network error'}).`);
      }
    });
  }

  onTenantChange(tenantId: string) {
    this.selectedTenantId.set(tenantId);
    this.selectedKbId.set('');
    this.loadKnowledgeBases();
  }

  promptNewTenant() {
    const name = prompt('Enter Organization/Tenant Name:');
    if (!name) return;
    const slug = name.toLowerCase().replace(/[^a-z0-9]/g, '-');
    this.http.post<Tenant>(`${this.apiUrl}/tenants`, { name, slug }).subscribe({
      next: (t) => {
        this.tenants.update(list => [...list, t]);
        this.onTenantChange(t.id);
      }
    });
  }

  loadKnowledgeBases() {
    if (!this.selectedTenantId()) return;
    this.http.get<KnowledgeBase[]>(`${this.apiUrl}/knowledge-bases`, { headers: this.getHeaders() }).subscribe({
      next: (list) => {
        this.knowledgeBases.set(list);
        if (list.length > 0) {
          this.selectKb(list[0].id);
        } else {
          this.selectedKbId.set('');
          this.documents.set([]);
        }
      },
      error: (err) => {
        // Previously there was no error handler at all, so a failed request was
        // indistinguishable from "no knowledge bases exist".
        this.knowledgeBases.set([]);
        this.apiError.set(`Failed to load knowledge bases (${err.status || 'network error'}).`);
      }
    });
  }

  selectKb(kbId: string) {
    this.selectedKbId.set(kbId);
    this.loadDocuments();
  }

  getSelectedKbName(): string {
    const kb = this.knowledgeBases().find(k => k.id === this.selectedKbId());
    return kb ? kb.name : 'None selected';
  }

  createKnowledgeBase() {
    if (!this.newKbName.trim()) return;
    this.http.post<KnowledgeBase>(`${this.apiUrl}/knowledge-bases`, {
      name: this.newKbName,
      description: this.newKbDesc
    }, { headers: this.getHeaders() }).subscribe({
      next: (kb) => {
        this.knowledgeBases.update(kbs => [...kbs, kb]);
        this.selectKb(kb.id);
        this.showNewKbModal = false;
        this.newKbName = '';
        this.newKbDesc = '';
      }
    });
  }

  loadDocuments() {
    const kbId = this.selectedKbId();
    if (!kbId) return;

    this.http.get<DocumentItem[]>(`${this.apiUrl}/knowledge-bases/${kbId}/documents`, { headers: this.getHeaders() })
      .subscribe({
        next: (docs) => this.documents.set(docs)
      });
  }

  onFileSelected(event: Event) {
    const target = event.target as HTMLInputElement;
    if (!target.files || target.files.length === 0) return;

    const file = target.files[0];
    const formData = new FormData();
    formData.append('file', file);

    this.isUploading.set(true);
    this.http.post<DocumentItem>(
      `${this.apiUrl}/knowledge-bases/${this.selectedKbId()}/documents/upload`,
      formData,
      { headers: this.getHeaders() }
    ).subscribe({
      next: () => {
        this.isUploading.set(false);
        this.loadDocuments();
      },
      error: (err) => {
        this.isUploading.set(false);
        alert('Upload failed: ' + (err.error || err.message));
      }
    });
  }

  deleteDocument(docId: string) {
    if (!confirm('Are you sure you want to delete this document and its vector embeddings?')) return;
    this.http.delete(`${this.apiUrl}/documents/${docId}`, { headers: this.getHeaders() }).subscribe({
      next: () => this.loadDocuments()
    });
  }

  onEnterPress(event: Event) {
    const keyEvent = event as KeyboardEvent;
    if (!keyEvent.shiftKey) {
      keyEvent.preventDefault();
      this.sendMessage();
    }
  }

  async sendMessage() {
    const question = this.userInput.trim();
    if (!question || this.isGenerating()) return;

    this.userInput = '';
    this.chatMessages.update(msgs => [...msgs, { role: 'user', content: question }]);

    const assistantMsgIndex = this.chatMessages().length;
    this.chatMessages.update(msgs => [...msgs, { role: 'assistant', content: '', citations: [], isStreaming: true }]);
    this.isGenerating.set(true);

    try {
      const response = await fetch(`${this.apiUrl}/chat/stream`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Tenant-Id': this.selectedTenantId()
        },
        body: JSON.stringify({
          knowledgeBaseId: this.selectedKbId() || null,
          message: question
        })
      });

      if (!response.ok) {
        // Without this, a 500 with an empty body produced a blank assistant bubble
        // and no indication that anything had gone wrong.
        throw new Error(`API returned HTTP ${response.status} ${response.statusText}`);
      }
      if (!response.body) throw new Error('No response stream');

      const reader = response.body.getReader();
      const decoder = new TextDecoder();
      let buffer = '';

      while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        buffer += decoder.decode(value, { stream: true });
        const lines = buffer.split('\n\n');
        buffer = lines.pop() || '';

        for (const line of lines) {
          if (line.startsWith('data: ')) {
            const jsonStr = line.slice(6);
            try {
              const evt = JSON.parse(jsonStr);
              if (evt.Type === 'token') {
                this.chatMessages.update(msgs => {
                  const copy = [...msgs];
                  copy[assistantMsgIndex].content += evt.Content;
                  return copy;
                });
              } else if (evt.Type === 'citations') {
                this.chatMessages.update(msgs => {
                  const copy = [...msgs];
                  copy[assistantMsgIndex].citations = evt.Citations;
                  return copy;
                });
              } else if (evt.Type === 'error') {
                // The server now reports retrieval/model failures in-stream rather than
                // dropping the connection with a bare 500.
                this.chatMessages.update(msgs => {
                  const copy = [...msgs];
                  copy[assistantMsgIndex].content += `\n[Server error] ${evt.Content}`;
                  return copy;
                });
              }
            } catch {}
          }
        }
      }
    } catch (err: any) {
      this.chatMessages.update(msgs => {
        const copy = [...msgs];
        copy[assistantMsgIndex].content += '\n\n[Error retrieving response: ' + err.message + ']';
        return copy;
      });
    } finally {
      this.isGenerating.set(false);
      this.chatMessages.update(msgs => {
        const copy = [...msgs];
        copy[assistantMsgIndex].isStreaming = false;
        return copy;
      });
    }
  }

  previewCitation(c: Citation) {
    this.activeCitationModal = c;
  }

  clearChat() {
    this.chatMessages.set([]);
  }

  getStatusText(status: number): string {
    switch (status) {
      case 0: return 'Uploaded';
      case 1: return 'Queued';
      case 2: return 'Processing';
      case 3: return 'Indexed';
      case 4: return 'Failed';
      default: return 'Unknown';
    }
  }

  getStatusClass(status: number): string {
    switch (status) {
      case 1: return 'status-queued';
      case 2: return 'status-processing';
      case 3: return 'status-indexed';
      case 4: return 'status-failed';
      default: return '';
    }
  }

  formatBytes(bytes: number): string {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
  }
}
