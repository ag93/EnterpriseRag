# EnterpriseRag

An enterprise-style Retrieval-Augmented Generation (RAG) API built with C# and ASP.NET Core. The project combines document ingestion, vector search, access-controlled retrieval, grounded answers with citations, and index reconciliation.

**Repository:** https://github.com/ag93/EnterpriseRag

## Overview

EnterpriseRag lets authenticated users ask questions about documents they are authorized to access. It retrieves relevant document chunks, reranks candidates, generates an answer using a local language model, and validates citations against retrieved evidence.

The project emphasizes security, traceability, and operational correctness rather than just generating answers.

## Features

- PDF text extraction and structure-aware chunking.
- Local embeddings and chat completion through Ollama.
- Vector retrieval using Qdrant.
- SQLite as the authoritative store for documents, chunks, and access permissions.
- Group-based document authorization using Microsoft Entra ID.
- Retrieval filtering based on the current user's permitted documents.
- Defense-in-depth filtering of search results.
- Keyword-based reranking.
- Citation validation and evidence-support checking.
- Administrative index reconciliation.
- Integration tests for authentication, authorization, RAG behavior, and reconciliation.
- Local retrieval evaluation with baseline and reranked metrics.

## Technology stack

| Component | Technology |
|---|---|
| Language | C# |
| Framework | ASP.NET Core on .NET 10 |
| Authentication | Microsoft Entra ID / Microsoft.Identity.Web |
| Relational storage | SQLite |
| ORM and migrations | Entity Framework Core |
| Vector database | Qdrant |
| Embeddings | Ollama with `nomic-embed-text` |
| Chat generation | Ollama with `qwen3:8b` |
| PDF extraction | PdfPig |
| API documentation | Swagger / OpenAPI |
| Testing | .NET integration tests |

## Architecture

```text
Client
  |
  v
ASP.NET Core API
  |
  +--> Authentication and authorization
  |
  +--> User and group resolution
  |
  +--> Document access authorization
  |
  +--> RAG orchestration
          |
          +--> Query embedding (Ollama)
          |
          +--> Vector retrieval (Qdrant)
          |
          +--> Keyword reranking
          |
          +--> Authorized evidence selection
          |
          +--> Answer generation (Ollama)
          |
          +--> Citation and evidence checks
  |
  v
Answer with citations

Supporting stores:
  SQLite --> documents, chunks, access permissions
  Qdrant --> chunk vectors and retrieval payloads
```

SQLite is the authoritative source for document and permission metadata. Qdrant supports similarity search; it is not the source of truth for document authorization.

## Security model

### Authentication and authorization

The API uses Microsoft Entra ID for authentication in its normal configuration.

Authorization is separate from authentication and is enforced through ASP.NET Core authorization policies. The configured fallback policy requires authentication for endpoints without their own authorization metadata. The `AdminsOnly` policy protects administrative operations such as index reconciliation.

The API maps Entra ID group claims to application group names through configuration. Those application groups are used to determine which documents a user may access.

### Document-level access control

The RAG flow resolves the current user's permitted document IDs before retrieval. Those IDs are passed into the search request, and returned candidates are filtered again against the permitted IDs.

If a user has no permitted documents, the RAG service returns an access-related response without invoking document search or answer generation.

An empty permission list must not be treated as unrestricted access.

### Development and test authentication

Integration tests use a test authentication handler to simulate authenticated and unauthenticated users, including group membership, without requiring a live Entra ID sign-in.

A `LocalCurrentUser` implementation and `LocalIdentity` development configuration are also available for local development. Local identity is optional and is **not enabled by default**.

Do not comment out the authorization configuration to enable local identity. A local identity must supply an appropriate authenticated principal and group claims while leaving authorization policies enabled. Verify both the RAG and administrative authorization paths before relying on a local authentication setup.

Never enable a development identity in production or allow untrusted callers to choose their own identity or group membership.

## Prerequisites

Install and configure:

- .NET 10 SDK.
- Ollama.
- Qdrant.
- Git.

Pull the required Ollama models:

```bash
ollama pull nomic-embed-text
ollama pull qwen3:8b
```

Start Ollama and Qdrant before running live RAG operations or the integration evaluation that depends on those services.

The current local configuration expects:

| Service | Default address |
|---|---|
| Ollama HTTP API | `http://localhost:11434` |
| Qdrant gRPC | `localhost:6334` |
| API HTTP | `http://localhost:5019` |
| API HTTPS | `https://localhost:7166` |

These are development defaults. Update the configuration and launch profile if your environment uses different addresses or ports.

## Getting started

### 1. Clone the repository

```bash
git clone https://github.com/ag93/EnterpriseRag.git
cd EnterpriseRag
```

### 2. Restore dependencies

```bash
dotnet restore
```

### 3. Start dependencies

Ensure Ollama is running and the required models are available. Start Qdrant using your local installation or deployment.

The API's configured HTTP clients connect to Ollama at `http://localhost:11434`, and the Qdrant client connects to `localhost:6334`.

### 4. Configure the application

Review the API configuration files, including `appsettings.json`, `appsettings.Development.json`, and `Properties/launchSettings.json`.

Configure the Entra ID tenant, API application, audience, group mappings, and any client settings needed for your environment. Use your own values and do not commit secrets or environment-specific credentials.

The development configuration includes local identity settings, but these do not by themselves configure an alternate authentication scheme or bypass the authentication middleware.

### 5. Apply database migrations

From the repository root, run:

```bash
dotnet ef database update \
  --project src/EnterpriseRag.Infrastructure \
  --startup-project src/EnterpriseRag.Api
```

This applies the EF Core migrations to the configured SQLite database.

The default database path is relative to the API content root:

```text
src/EnterpriseRag.Api/data/enterprise-rag.db
```

Ensure the directory exists and is writable. The API does not automatically apply migrations at startup in the current configuration.

### 6. Run the API

```bash
dotnet run --project src/EnterpriseRag.Api
```

Using the configured launch profile, the API is available at:

- API: http://localhost:5019
- Swagger UI: http://localhost:5019/swagger
- HTTPS API: https://localhost:7166

The actual addresses depend on the selected launch profile.

The `/health` endpoint provides a basic liveness check. It should not be interpreted as proof that SQLite, Ollama, or Qdrant are healthy and reachable.

## Document ingestion and indexing

The ingestion pipeline extracts text from PDF files and splits it into chunks using structure-aware chunking.

Current chunking defaults:

- Maximum chunk size: 1,000 characters.
- Overlap: 150 characters.

Chunk metadata includes identifiers and source information such as document ID, file name, and page number. Embeddings are generated using `nomic-embed-text` and stored in Qdrant with retrieval payloads.

SQLite stores the authoritative document and chunk records, along with access permissions.

Uploading the same file again currently creates a new document and new chunk IDs rather than updating or deduplicating the existing document. Document replacement and deduplication are potential future improvements.

## RAG request flow

A typical RAG request follows these steps:

1. Authenticate the caller.
2. Resolve the caller's identity and application groups.
3. Determine which documents the caller may access.
4. Return an access-related response if no documents are permitted.
5. Embed the query.
6. Retrieve candidate chunks from Qdrant, filtered by permitted document IDs.
7. Apply keyword-based reranking.
8. Generate an answer from the selected evidence.
9. Validate citations and check whether the answer is supported by retrieved evidence.
10. Return the answer and source citations.

Retrieval and citation checks improve traceability, but they do not guarantee that every generated statement is correct. Answers should be evaluated against the underlying documents and expected behavior.

## Index reconciliation

The administrative reconciliation endpoint is:

```http
POST /api/admin/reconcile-index
```

It is protected by the `AdminsOnly` authorization policy.

Reconciliation compares the relational records with the vector index and detects inconsistencies, including missing indexed chunks and orphaned vectors.

The current implementation can reindex documents whose chunks are missing and remove orphaned vectors after successful reindexing. Reconciliation reports inconsistencies detected before repairs.

Reindexing a document embeds all of its chunks again. Reconciliation is designed to be repeatable, and the integration tests cover idempotence and safeguards against deleting orphaned vectors when reindexing fails.

Only authorized administrators should invoke this endpoint.

## Running tests

Run the complete test suite from the repository root:

```bash
dotnet test
```

The integration tests cover areas including:

- Authentication requirements.
- Admin versus non-admin access.
- RAG behavior when a user has no permitted documents.
- Document authorization and retrieval filtering.
- Index reconciliation, idempotence, and failure handling.

The integration test factory configures test authentication and group mappings. Tests that exercise live retrieval evaluation require the relevant local services and models to be running.

A successful test run verifies the behavior covered by the tests; it does not establish that every production deployment scenario is secure or reliable.

## Retrieval evaluation

The repository includes a local retrieval evaluation suite that compares baseline retrieval with keyword reranking.

Recorded results on the current evaluation dataset:

| Metric | Baseline | Reranked |
|---|---:|---:|
| Mean Reciprocal Rank (MRR) | 0.864 | 0.939 |
| Evidence Recall@1 | 65.9% | 75.0% |
| Evidence Recall@3 | 84.1% | 97.7% |
| Evidence Recall@5 | 84.1% | 100% |

Additional recorded results:

- Answerable questions: 11/11 correct against the expected evidence fragments.
- Unanswerable questions: 3/3 handled as expected.
- Citations checked: 15.
- Citations supported: 15.
- Citations rejected: 0.

These figures describe a small, local evaluation dataset and the tested configuration. They are not a guarantee of performance on other document collections, queries, models, or production workloads.

## Project structure

```text
EnterpriseRag/
├── src/
│   ├── EnterpriseRag.Api/
│   └── EnterpriseRag.Infrastructure/
├── tests/
│   └── EnterpriseRag.IntegrationTests/
└── README.md
```

The API project contains the ASP.NET Core application and API configuration. The infrastructure project contains persistence and infrastructure implementations, including the EF Core context and migrations. The integration test project exercises API and application behavior.

## Configuration and operational notes

- Keep tenant-specific settings and credentials out of source control.
- Use development-only settings for local identity and test authentication.
- Keep authorization policies enabled in every environment.
- Back up the SQLite database if it contains data that must be retained.
- Ensure the Qdrant collection's vector size and distance metric match the embedding configuration.
- The current embedding setup expects 768-dimensional vectors.
- The Qdrant collection used by the current implementation is `document_chunks`.
- The API's basic health endpoint does not perform comprehensive dependency checks.
- Local model performance and retrieval quality depend on the hardware, document set, and model versions.

## Current limitations and future improvements

Potential next steps include:

- Document deduplication and replacement on re-upload.
- More comprehensive dependency health checks.
- Additional retrieval and answer evaluation datasets.
- Expanded observability, structured logging, and latency metrics.
- More robust handling of ingestion failures and retries.
- Production-oriented deployment configuration and secrets management.
- A dedicated, secure local-development authentication scheme that retains the normal authorization policies.
