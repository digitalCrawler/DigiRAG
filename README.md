# DigiRAG - Enterprise Multi-Tenant RAG Platform

DigiRAG is a high-performance, enterprise-grade Retrieval-Augmented Generation (RAG) platform built with **.NET 9 (C#)**, **PostgreSQL 18 (`pgvector`)**, **Hangfire**, and **Angular**.

---

## 🏗️ Architecture Overview

```
c:\Personal\RAG\
├── DigiRAG.slnx                 # Modern .NET solution file
├── database/
│   └── migrate.sql              # Generated: dotnet ef migrations script --idempotent
├── src/
│   ├── DigiRAG.Domain/          # Pure Domain Entities, Enums & Value Objects
│   │   ├── Entities/            # Tenant, KnowledgeBase, Document, DocumentChunk, ChatSession, Citations
│   │   └── Enums/               # DocumentStatus, MessageRole, AiProviderType
│   ├── DigiRAG.Application/     # CQRS, AI contracts, Extraction & Retrieval Interfaces
│   │   ├── AI/                  # AiSettings, IAiModelFactory, Microsoft.Extensions.AI
│   │   ├── Ingestion/           # ExtractedDocument, ITextChunker, IIngestionService
│   │   └── Retrieval/           # IRetrievalService, IRerankerService, IChatService
│   ├── DigiRAG.Infrastructure/  # Concrete Providers, Database, Storage, and AI Adapters
│   │   ├── AI/                  # ModelProviderFactory (Local Ollama, Gemini, OpenAI, Azure OpenAI)
│   │   ├── Extractors/          # PdfPig (PDF), OpenXML (Word DOCX), Text & Markdown extractors
│   │   ├── Chunking/            # Structure-aware heading & paragraph chunking with overlaps
│   │   ├── Ingestion/           # Durable ingestion pipeline with batch vector embeddings
│   │   ├── Retrieval/           # Hybrid search (pgvector Cosine <=> + TSVector) & RRF fusion
│   │   ├── Storage/             # File storage service (Local disk & S3 compatible)
│   │   └── Persistence/         # ApplicationDbContext, pgvector HNSW indexing, RLS, DbInitializer
│   ├── DigiRAG.Worker/          # Hangfire Ingestion Processing Service
│   ├── DigiRAG.Api/             # ASP.NET Core REST API, SSE Streaming & Hangfire Dashboard
│   └── DigiRAG.Client/          # Standalone Angular 19 Client (KB Manager, Document Uploads, Grounded Chat)
└── tests/
    └── DigiRAG.UnitTests/       # Unit tests for structure-aware chunking and reranking
```

---

## ⚡ Pluggable AI Configuration (Local / OpenAI / Azure / Gemini)

In `src/DigiRAG.Api/appsettings.json`, you can switch between AI providers by simply toggling `"ActiveProvider"`:

```json
{
  "AiSettings": {
    "ActiveProvider": "Gemini", // "Local" | "Gemini" | "OpenAI" | "AzureOpenAI"
    "Local": {
      "Endpoint": "http://localhost:11434",
      "ChatModel": "llama3.2",
      "EmbeddingModel": "nomic-embed-text",
      "EmbeddingDimensions": 768
    },
    "Gemini": {
      "ApiKey": "YOUR_GEMINI_API_KEY",
      "ChatModel": "gemini-3.8-flash",
      "EmbeddingModel": "gemini-embedding-2",
      "EmbeddingDimensions": 768
    },
    "OpenAI": {
      "ApiKey": "YOUR_OPENAI_API_KEY",
      "ChatModel": "gpt-4o-mini",
      "EmbeddingModel": "text-embedding-3-small",
      "EmbeddingDimensions": 1536
    },
    "AzureOpenAI": {
      "Endpoint": "https://your-resource.openai.azure.com/",
      "ApiKey": "YOUR_AZURE_KEY",
      "ChatDeploymentName": "gpt-4o-mini",
      "EmbeddingDeploymentName": "text-embedding-3-small",
      "EmbeddingDimensions": 1536
    }
  }
}
```

> **Embedding dimensions must match the database.** `DocumentChunk.Embedding` is a
> `vector(768)` column, so `EmbeddingDimensions` has to stay at `768` for whichever
> provider is active. `gemini-embedding-2` natively returns 3072 dimensions, so the
> configured value is sent as an explicit output size (Gemini L2-normalizes the
> truncated vector automatically). Switching to a provider with a different width
> (e.g. OpenAI at 1536) requires a migration that widens the column **and**
> re-embedding every existing chunk, since vectors from different models are not
> comparable.

> **Gemini API key.** Keep `ApiKey` out of `appsettings.json` and supply it via the
> `AiSettings__Gemini__ApiKey` environment variable. Get a key from
> [Google AI Studio](https://aistudio.google.com/apikey). If the key is missing,
> the first chat or ingestion request fails with an explicit configuration error.

---

## 🚀 Getting Started

### 1. PostgreSQL Database & pgvector
PostgreSQL 18 is located at `C:\Program Files\PostgreSQL\18`.
Ensure the Windows service is running in `services.msc`:
- Service Name: `postgresql-x64-18`
- Port: `5432`
- Verify password in `appsettings.json`: `"DefaultConnection": "Host=localhost;Port=5432;Database=digirag_db;Username=postgres;Password=your_password;"`

*On first startup, the API automatically runs `DbInitializer` which creates the database, enables `vector`, creates tables, and seeds a starter Tenant and Knowledge Base!*

### 2. Run Local Model (Ollama) — optional
Only needed if you set `ActiveProvider` back to `Local`; the default is Gemini:
```bash
ollama run llama3.2
ollama pull nomic-embed-text
```

### 3. Run the Web API
```bash
dotnet run --project src/DigiRAG.Api
```
- **Swagger Documentation:** `http://localhost:5000/swagger`
- **Hangfire Jobs Dashboard:** `http://localhost:5000/hangfire`

### 4. Run the Background Worker (Optional / Standalone)
```bash
dotnet run --project src/DigiRAG.Worker
```

### 5. Run the Angular Frontend
```bash
cd src/DigiRAG.Client
npm install
npm start
```
Open your browser at `http://localhost:4200` to manage Knowledge Bases, upload documents, and test grounded streaming chat with verified citations.

---

## 🔒 Security & Multi-Tenancy (Row-Level Security)
1. Every API request receives `X-Tenant-Id` header.
2. The `TenantResolutionMiddleware` sets the scoped context and calls:
   ```sql
   SELECT set_config('app.tenant_id', @tenant_id, false);
   ```
3. PostgreSQL Row-Level Security (RLS) ensures that even in raw SQL queries, data across organizations cannot leak.

---

## ☁️ Cloud Deployment (Railway + Vercel)

### Deploying Backend & Database to Railway
1. **Create Project on Railway**:
   - Add **PostgreSQL** service (with `pgvector` enabled, e.g. using `pgvector/pgvector:pg16` Docker image).
2. **Deploy DigiRAG Backend**:
   - Link your GitHub repository.
   - Set the root directory to repo root; Railway will read [railway.json](file:///c:/Personal/RAG/railway.json) and build [Dockerfile](file:///c:/Personal/RAG/src/DigiRAG.Api/Dockerfile).
3. **Configure Environment Variables** on Railway:
   - Copy values from [.env.example](file:///c:/Personal/RAG/.env.example):
     ```env
     ConnectionStrings__DefaultConnection=${{Postgres.DATABASE_URL}}
     AiSettings__ActiveProvider=Gemini
     AiSettings__Gemini__ApiKey=YOUR_GEMINI_KEY
     Cors__AllowedOrigins=https://your-frontend.vercel.app
     ```
   - *On boot, EF Core automatically applies all migrations (`CREATE EXTENSION IF NOT EXISTS vector`, creates tables, HNSW indexes, and seeds starter data).*

### Deploying Frontend to Vercel
1. In Vercel, click **Add New Project** and select your repository.
2. Set **Root Directory** to `src/DigiRAG.Client`.
3. Set **Framework Preset** to `Angular`.
4. Update `src/environments/environment.prod.ts` with your Railway API domain:
   ```ts
   export const environment = {
     production: true,
     apiUrl: 'https://digirag-api.up.railway.app/api/v1'
   };
   ```
5. Deploy! Vercel uses [vercel.json](file:///c:/Personal/RAG/src/DigiRAG.Client/vercel.json) to handle single-page application routing.
