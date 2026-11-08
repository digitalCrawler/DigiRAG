-- ====================================================================
-- DigiRAG: PostgreSQL 18 + pgvector Initial Schema & Security Setup
-- ====================================================================

-- 1. Enable pgvector extension
CREATE EXTENSION IF NOT EXISTS vector;
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- 2. Tenants Table
CREATE TABLE IF NOT EXISTS tenants (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(255) NOT NULL,
    slug VARCHAR(100) NOT NULL UNIQUE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 3. Knowledge Bases
CREATE TABLE IF NOT EXISTS knowledge_bases (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    created_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS ix_knowledge_bases_tenant_name ON knowledge_bases (tenant_id, name);

-- 4. Documents Table
CREATE TABLE IF NOT EXISTS documents (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    knowledge_base_id UUID NOT NULL REFERENCES knowledge_bases(id) ON DELETE CASCADE,
    file_name VARCHAR(500) NOT NULL,
    content_type VARCHAR(100) NOT NULL,
    file_size_bytes BIGINT NOT NULL DEFAULT 0,
    content_hash VARCHAR(64) NOT NULL,
    storage_path VARCHAR(1000) NOT NULL,
    status INTEGER NOT NULL DEFAULT 0, -- 0: Uploaded, 1: Queued, 2: Processing, 3: Indexed, 4: Failed
    chunk_count INTEGER NOT NULL DEFAULT 0,
    error_message TEXT,
    created_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    indexed_at_utc TIMESTAMPTZ
);
CREATE INDEX IF NOT EXISTS ix_documents_tenant_kb ON documents (tenant_id, knowledge_base_id);
CREATE INDEX IF NOT EXISTS ix_documents_tenant_hash ON documents (tenant_id, content_hash);

-- 5. Document Chunks (Dense Vector + Full-Text TSVector + Metadata)
CREATE TABLE IF NOT EXISTS document_chunks (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    knowledge_base_id UUID NOT NULL REFERENCES knowledge_bases(id) ON DELETE CASCADE,
    document_id UUID NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
    chunk_index INTEGER NOT NULL,
    content TEXT NOT NULL,
    page_number INTEGER,
    heading_path VARCHAR(500),
    metadata_json JSONB NOT NULL DEFAULT '{}'::jsonb,
    embedding VECTOR(768), -- default 768 dimensions (Nomic / Gemini)
    search_vector TSVECTOR GENERATED ALWAYS AS (to_tsvector('english', coalesce(content, ''))) STORED,
    created_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_chunks_doc_index UNIQUE (document_id, chunk_index)
);

-- Fast HNSW Cosine Index for Approximate Nearest Neighbor retrieval
CREATE INDEX IF NOT EXISTS ix_chunks_embedding_hnsw 
    ON document_chunks USING hnsw (embedding vector_cosine_ops);

-- GIN Index for Lexical / Keyword Full-Text Search
CREATE INDEX IF NOT EXISTS ix_chunks_search_vector 
    ON document_chunks USING gin (search_vector);

CREATE INDEX IF NOT EXISTS ix_chunks_tenant_kb 
    ON document_chunks (tenant_id, knowledge_base_id);

CREATE INDEX IF NOT EXISTS ix_chunks_metadata 
    ON document_chunks USING gin (metadata_json);

-- 6. Ingestion Jobs
CREATE TABLE IF NOT EXISTS ingestion_jobs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    document_id UUID NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
    status INTEGER NOT NULL DEFAULT 1,
    attempts INTEGER NOT NULL DEFAULT 0,
    error_message TEXT,
    created_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    completed_at_utc TIMESTAMPTZ
);
CREATE INDEX IF NOT EXISTS ix_jobs_tenant_doc ON ingestion_jobs (tenant_id, document_id);

-- 7. Chat Sessions & History
CREATE TABLE IF NOT EXISTS chat_sessions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
    knowledge_base_id UUID REFERENCES knowledge_bases(id) ON DELETE SET NULL,
    title VARCHAR(255) NOT NULL DEFAULT 'New Conversation',
    created_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS ix_chat_sessions_tenant ON chat_sessions (tenant_id);

CREATE TABLE IF NOT EXISTS chat_messages (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    chat_session_id UUID NOT NULL REFERENCES chat_sessions(id) ON DELETE CASCADE,
    role INTEGER NOT NULL, -- 0: User, 1: Assistant, 2: System
    content TEXT NOT NULL,
    created_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS ix_chat_messages_session ON chat_messages (chat_session_id);

CREATE TABLE IF NOT EXISTS chat_citations (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    chat_message_id UUID NOT NULL REFERENCES chat_messages(id) ON DELETE CASCADE,
    document_id UUID NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
    chunk_id UUID NOT NULL REFERENCES document_chunks(id) ON DELETE CASCADE,
    document_name VARCHAR(500) NOT NULL,
    page_number INTEGER,
    heading_path VARCHAR(500),
    excerpt TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS ix_citations_message ON chat_citations (chat_message_id);

-- ====================================================================
-- Multi-Tenant Row-Level Security (RLS) Defence-in-Depth
-- ====================================================================
ALTER TABLE document_chunks ENABLE ROW LEVEL SECURITY;
ALTER TABLE document_chunks FORCE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS tenant_isolation_chunks ON document_chunks;
CREATE POLICY tenant_isolation_chunks ON document_chunks
    FOR ALL
    USING (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid)
    WITH CHECK (tenant_id = NULLIF(current_setting('app.tenant_id', true), '')::uuid);
