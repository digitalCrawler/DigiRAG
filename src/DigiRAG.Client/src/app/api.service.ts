import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';
import { Citation, DocumentItem, KnowledgeBase, Tenant } from './models';

export interface ChatStreamHandlers {
  onCitations: (citations: Citation[]) => void;
  onToken: (text: string) => void;
  onError: (message: string) => void;
}

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  readonly apiUrl = environment.apiUrl;

  private headers(tenantId: string): HttpHeaders {
    return new HttpHeaders({ 'X-Tenant-Id': tenantId });
  }

  getTenants(): Observable<Tenant[]> {
    return this.http.get<Tenant[]>(`${this.apiUrl}/tenants`);
  }

  createTenant(name: string, slug: string): Observable<Tenant> {
    return this.http.post<Tenant>(`${this.apiUrl}/tenants`, { name, slug });
  }

  getKnowledgeBases(tenantId: string): Observable<KnowledgeBase[]> {
    return this.http.get<KnowledgeBase[]>(`${this.apiUrl}/knowledge-bases`, {
      headers: this.headers(tenantId)
    });
  }

  createKnowledgeBase(tenantId: string, name: string, description: string): Observable<KnowledgeBase> {
    return this.http.post<KnowledgeBase>(
      `${this.apiUrl}/knowledge-bases`,
      { name, description },
      { headers: this.headers(tenantId) }
    );
  }

  getDocuments(tenantId: string, kbId: string): Observable<DocumentItem[]> {
    return this.http.get<DocumentItem[]>(`${this.apiUrl}/knowledge-bases/${kbId}/documents`, {
      headers: this.headers(tenantId)
    });
  }

  uploadDocument(tenantId: string, kbId: string, file: File): Observable<DocumentItem> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<DocumentItem>(
      `${this.apiUrl}/knowledge-bases/${kbId}/documents/upload`,
      formData,
      { headers: this.headers(tenantId) }
    );
  }

  deleteDocument(tenantId: string, documentId: string): Observable<unknown> {
    return this.http.delete(`${this.apiUrl}/documents/${documentId}`, {
      headers: this.headers(tenantId)
    });
  }

  /**
   * The API returns Server-Sent Events, which HttpClient cannot read incrementally,
   * so this uses fetch and parses the stream by hand.
   */
  async streamChat(
    tenantId: string,
    knowledgeBaseId: string | null,
    message: string,
    handlers: ChatStreamHandlers
  ): Promise<void> {
    const response = await fetch(`${this.apiUrl}/chat/stream`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-Tenant-Id': tenantId
      },
      body: JSON.stringify({ knowledgeBaseId: knowledgeBaseId || null, message })
    });

    // A 500 with an empty body would otherwise produce a blank answer with no
    // indication that anything went wrong.
    if (!response.ok) {
      throw new Error(`API returned HTTP ${response.status} ${response.statusText}`);
    }
    if (!response.body) {
      throw new Error('No response stream');
    }

    const reader = response.body.getReader();
    const decoder = new TextDecoder();
    let buffer = '';

    while (true) {
      const { done, value } = await reader.read();
      if (done) break;

      buffer += decoder.decode(value, { stream: true });
      const frames = buffer.split('\n\n');
      buffer = frames.pop() || '';

      for (const frame of frames) {
        if (!frame.startsWith('data: ')) continue;
        try {
          const evt = JSON.parse(frame.slice(6));
          if (evt.Type === 'token') {
            handlers.onToken(evt.Content ?? '');
          } else if (evt.Type === 'citations') {
            handlers.onCitations(evt.Citations ?? []);
          } else if (evt.Type === 'error') {
            // The server reports retrieval and model failures in-stream rather than
            // dropping the connection with a bare 500.
            handlers.onError(evt.Content ?? 'Unknown server error');
          }
        } catch {
          // Ignore malformed frames rather than aborting the whole stream.
        }
      }
    }
  }

  /**
   * Upload and storage failures come back as a structured body, so err.error is an
   * object; plain string concatenation would render "[object Object]".
   */
  static describeError(err: any): string {
    const body = err?.error;
    if (typeof body === 'string' && body.trim()) return body;
    const detail = body?.detail || body?.error || err?.message || 'Unknown error';
    return body?.hint ? `${detail}\n\n${body.hint}` : detail;
  }
}
