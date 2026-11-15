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

/** status: 0=Uploaded, 1=Queued, 2=Processing, 3=Indexed, 4=Failed */
export interface DocumentItem {
  id: string;
  fileName: string;
  fileSizeBytes: number;
  status: number;
  chunkCount: number;
  errorMessage?: string;
  createdAtUtc: string;
}

export interface ChatSession {
  id: string;
  title: string;
  createdAtUtc: string;
  knowledgeBaseId?: string;
}

/** MessageRole on the server: 0 = User, 1 = Assistant, 2 = System. */
export interface StoredMessage {
  id: string;
  chatSessionId: string;
  role: number;
  content: string;
  createdAtUtc: string;
  citations?: Citation[];
}

export interface Citation {
  documentId: string;
  chunkId: string;
  documentName: string;
  pageNumber?: number;
  headingPath?: string;
  excerpt: string;
}

/**
 * Generative UI: the model chooses the component for each part of its answer and
 * emits one of these per line, rather than returning prose we then have to guess at.
 */
export type UiBlock =
  | { type: 'heading'; text: string }
  | { type: 'paragraph'; text: string }
  | { type: 'bullets'; items: string[] }
  | { type: 'steps'; items: string[] }
  | { type: 'facts'; items: { label: string; value: string }[] }
  | { type: 'table'; headers: string[]; rows: string[][] }
  | { type: 'callout'; tone?: 'info' | 'warn' | 'success'; text: string }
  | { type: 'code'; language?: string; text: string };

export interface ChatMessageUI {
  role: 'user' | 'assistant';
  content: string;
  blocks?: UiBlock[];
  citations?: Citation[];
  isStreaming?: boolean;
  /** Set when the stream reported a failure, rendered separately from the answer text. */
  errorText?: string;
  /** Thumbs rating already submitted for this reply. */
  rating?: 'up' | 'down';
  /** Server-side message id, present only for replies loaded from history. */
  messageId?: string;
}

export const DOCUMENT_STATUS: Record<number, { label: string; className: string }> = {
  0: { label: 'Uploaded', className: 'status-uploaded' },
  1: { label: 'Queued', className: 'status-queued' },
  2: { label: 'Processing', className: 'status-processing' },
  3: { label: 'Indexed', className: 'status-indexed' },
  4: { label: 'Failed', className: 'status-failed' }
};

export function statusLabel(status: number): string {
  return DOCUMENT_STATUS[status]?.label ?? 'Unknown';
}

export function statusClass(status: number): string {
  return DOCUMENT_STATUS[status]?.className ?? '';
}

/**
 * Restores UI blocks from a stored answer. Assistant replies are persisted as the
 * raw newline-delimited JSON the model produced, so a reloaded conversation has to
 * be parsed back into blocks or it would display as JSON source.
 */
export function parseBlocks(content: string): UiBlock[] {
  const blocks: UiBlock[] = [];
  for (const line of (content || '').split('\n')) {
    const trimmed = line.trim();
    if (!trimmed) continue;
    try {
      const parsed = JSON.parse(trimmed);
      if (parsed && typeof parsed.type === 'string') {
        blocks.push(parsed as UiBlock);
        continue;
      }
    } catch {
      // Older answers were stored as plain prose.
    }
    blocks.push({ type: 'paragraph', text: trimmed });
  }
  return blocks;
}

export function formatBytes(bytes: number): string {
  if (!bytes) return '0 B';
  const k = 1024;
  const sizes = ['B', 'KB', 'MB', 'GB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
}
