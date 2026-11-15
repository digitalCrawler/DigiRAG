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
  /** Set when the stream reported a failure, rendered separately from the answer text. */
  errorText?: string;
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

export function formatBytes(bytes: number): string {
  if (!bytes) return '0 B';
  const k = 1024;
  const sizes = ['B', 'KB', 'MB', 'GB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
}
