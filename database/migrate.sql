CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261108204846_InitialCreate') THEN
    CREATE EXTENSION IF NOT EXISTS vector;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261108204846_InitialCreate') THEN
    CREATE TABLE tenants (
        "Id" uuid NOT NULL,
        "Name" text NOT NULL,
        "Slug" text NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_tenants" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261108204846_InitialCreate') THEN
    CREATE TABLE knowledge_bases (
        "Id" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "Name" text NOT NULL,
        "Description" text,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_knowledge_bases" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_knowledge_bases_tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES tenants ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261108204846_InitialCreate') THEN
    CREATE TABLE chat_sessions (
        "Id" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "KnowledgeBaseId" uuid,
        "Title" text NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_chat_sessions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_chat_sessions_knowledge_bases_KnowledgeBaseId" FOREIGN KEY ("KnowledgeBaseId") REFERENCES knowledge_bases ("Id"),
        CONSTRAINT "FK_chat_sessions_tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES tenants ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261108204846_InitialCreate') THEN
    CREATE TABLE documents (
        "Id" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "KnowledgeBaseId" uuid NOT NULL,
        "FileName" text NOT NULL,
        "ContentType" text NOT NULL,
        "FileSizeBytes" bigint NOT NULL,
        "ContentHash" text NOT NULL,
        "StoragePath" text NOT NULL,
        "Status" integer NOT NULL,
        "ChunkCount" integer NOT NULL,
        "ErrorMessage" text,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "IndexedAtUtc" timestamp with time zone,
        CONSTRAINT "PK_documents" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_documents_knowledge_bases_KnowledgeBaseId" FOREIGN KEY ("KnowledgeBaseId") REFERENCES knowledge_bases ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_documents_tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES tenants ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261108204846_InitialCreate') THEN
    CREATE TABLE chat_messages (
        "Id" uuid NOT NULL,
        "ChatSessionId" uuid NOT NULL,
        "Role" integer NOT NULL,
        "Content" text NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_chat_messages" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_chat_messages_chat_sessions_ChatSessionId" FOREIGN KEY ("ChatSessionId") REFERENCES chat_sessions ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261108204846_InitialCreate') THEN
    CREATE TABLE document_chunks (
        "Id" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "KnowledgeBaseId" uuid NOT NULL,
        "DocumentId" uuid NOT NULL,
        "ChunkIndex" integer NOT NULL,
        "Content" text NOT NULL,
        "PageNumber" integer,
        "HeadingPath" text,
        "MetadataJson" text NOT NULL,
        "Embedding" vector(768),
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_document_chunks" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_document_chunks_documents_DocumentId" FOREIGN KEY ("DocumentId") REFERENCES documents ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_document_chunks_knowledge_bases_KnowledgeBaseId" FOREIGN KEY ("KnowledgeBaseId") REFERENCES knowledge_bases ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_document_chunks_tenants_TenantId" FOREIGN KEY ("TenantId") REFERENCES tenants ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261108204846_InitialCreate') THEN
    CREATE TABLE ingestion_jobs (
        "Id" uuid NOT NULL,
        "TenantId" uuid NOT NULL,
        "DocumentId" uuid NOT NULL,
        "Status" integer NOT NULL,
        "Attempts" integer NOT NULL,
        "ErrorMessage" text,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "CompletedAtUtc" timestamp with time zone,
        CONSTRAINT "PK_ingestion_jobs" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ingestion_jobs_documents_DocumentId" FOREIGN KEY ("DocumentId") REFERENCES documents ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261108204846_InitialCreate') THEN
    CREATE TABLE chat_citations (
        "Id" uuid NOT NULL,
        "ChatMessageId" uuid NOT NULL,
        "DocumentId" uuid NOT NULL,
        "ChunkId" uuid NOT NULL,
        "DocumentName" text NOT NULL,
        "PageNumber" integer,
        "HeadingPath" text,
        "Excerpt" text NOT NULL,
        CONSTRAINT "PK_chat_citations" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_chat_citations_chat_messages_ChatMessageId" FOREIGN KEY ("ChatMessageId") REFERENCES chat_messages ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_chat_citations_document_chunks_ChunkId" FOREIGN KEY ("ChunkId") REFERENCES document_chunks ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_chat_citations_documents_DocumentId" FOREIGN KEY ("DocumentId") REFERENCES documents ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261108204846_InitialCreate') THEN
    CREATE INDEX "IX_chat_citations_ChatMessageId" ON chat_citations ("ChatMessageId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261108204846_InitialCreate') THEN
    CREATE INDEX "IX_chat_citations_ChunkId" ON chat_citations ("ChunkId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261108204846_InitialCreate') THEN
    CREATE INDEX "IX_chat_citations_DocumentId" ON chat_citations ("DocumentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261108204846_InitialCreate') THEN
    CREATE INDEX "IX_chat_messages_ChatSessionId" ON chat_messages ("ChatSessionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261108204846_InitialCreate') THEN
    CREATE INDEX "IX_chat_sessions_KnowledgeBaseId" ON chat_sessions ("KnowledgeBaseId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261108204846_InitialCreate') THEN
    CREATE INDEX "IX_chat_sessions_TenantId" ON chat_sessions ("TenantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261108204846_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_document_chunks_DocumentId_ChunkIndex" ON document_chunks ("DocumentId", "ChunkIndex");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261108204846_InitialCreate') THEN
    CREATE INDEX "IX_document_chunks_Embedding" ON document_chunks USING hnsw ("Embedding" vector_cosine_ops);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261108204846_InitialCreate') THEN
    CREATE INDEX "IX_document_chunks_KnowledgeBaseId" ON document_chunks ("KnowledgeBaseId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261108204846_InitialCreate') THEN
    CREATE INDEX "IX_document_chunks_TenantId_KnowledgeBaseId" ON document_chunks ("TenantId", "KnowledgeBaseId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261108204846_InitialCreate') THEN
    CREATE INDEX "IX_documents_KnowledgeBaseId" ON documents ("KnowledgeBaseId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261108204846_InitialCreate') THEN
    CREATE INDEX "IX_documents_TenantId_ContentHash" ON documents ("TenantId", "ContentHash");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261108204846_InitialCreate') THEN
    CREATE INDEX "IX_documents_TenantId_KnowledgeBaseId" ON documents ("TenantId", "KnowledgeBaseId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261108204846_InitialCreate') THEN
    CREATE INDEX "IX_ingestion_jobs_DocumentId" ON ingestion_jobs ("DocumentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261108204846_InitialCreate') THEN
    CREATE INDEX "IX_ingestion_jobs_TenantId_DocumentId" ON ingestion_jobs ("TenantId", "DocumentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261108204846_InitialCreate') THEN
    CREATE INDEX "IX_knowledge_bases_TenantId_Name" ON knowledge_bases ("TenantId", "Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261108204846_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_tenants_Slug" ON tenants ("Slug");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261108204846_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261108204846_InitialCreate', '9.0.2');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261109123408_AddRowLevelSecurity') THEN
    ALTER TABLE knowledge_bases ENABLE ROW LEVEL SECURITY;

    DROP POLICY IF EXISTS tenant_isolation_knowledge_bases ON knowledge_bases;

    CREATE POLICY tenant_isolation_knowledge_bases ON knowledge_bases
        FOR ALL
        USING (
            NULLIF(current_setting('app.tenant_id', true), '') IS NULL
            OR "TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
        )
        WITH CHECK (
            NULLIF(current_setting('app.tenant_id', true), '') IS NULL
            OR "TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
        );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261109123408_AddRowLevelSecurity') THEN
    ALTER TABLE documents ENABLE ROW LEVEL SECURITY;

    DROP POLICY IF EXISTS tenant_isolation_documents ON documents;

    CREATE POLICY tenant_isolation_documents ON documents
        FOR ALL
        USING (
            NULLIF(current_setting('app.tenant_id', true), '') IS NULL
            OR "TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
        )
        WITH CHECK (
            NULLIF(current_setting('app.tenant_id', true), '') IS NULL
            OR "TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
        );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261109123408_AddRowLevelSecurity') THEN
    ALTER TABLE document_chunks ENABLE ROW LEVEL SECURITY;

    DROP POLICY IF EXISTS tenant_isolation_document_chunks ON document_chunks;

    CREATE POLICY tenant_isolation_document_chunks ON document_chunks
        FOR ALL
        USING (
            NULLIF(current_setting('app.tenant_id', true), '') IS NULL
            OR "TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
        )
        WITH CHECK (
            NULLIF(current_setting('app.tenant_id', true), '') IS NULL
            OR "TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
        );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261109123408_AddRowLevelSecurity') THEN
    ALTER TABLE ingestion_jobs ENABLE ROW LEVEL SECURITY;

    DROP POLICY IF EXISTS tenant_isolation_ingestion_jobs ON ingestion_jobs;

    CREATE POLICY tenant_isolation_ingestion_jobs ON ingestion_jobs
        FOR ALL
        USING (
            NULLIF(current_setting('app.tenant_id', true), '') IS NULL
            OR "TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
        )
        WITH CHECK (
            NULLIF(current_setting('app.tenant_id', true), '') IS NULL
            OR "TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
        );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261109123408_AddRowLevelSecurity') THEN
    ALTER TABLE chat_sessions ENABLE ROW LEVEL SECURITY;

    DROP POLICY IF EXISTS tenant_isolation_chat_sessions ON chat_sessions;

    CREATE POLICY tenant_isolation_chat_sessions ON chat_sessions
        FOR ALL
        USING (
            NULLIF(current_setting('app.tenant_id', true), '') IS NULL
            OR "TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
        )
        WITH CHECK (
            NULLIF(current_setting('app.tenant_id', true), '') IS NULL
            OR "TenantId" = NULLIF(current_setting('app.tenant_id', true), '')::uuid
        );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261109123408_AddRowLevelSecurity') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261109123408_AddRowLevelSecurity', '9.0.2');
    END IF;
END $EF$;
COMMIT;

