# Everlore

A multi-tenant Business Intelligence platform that lets users connect to any data source, build ad-hoc reports and dashboards, and leverage AI to query their data conversationally.

## What Is Everlore

Everlore is a self-hostable BI tool designed for companies that want Power BI-style capabilities without vendor lock-in. Users connect their own databases, APIs, or file uploads, then build reports visually or through natural language. Each tenant brings their own AI provider — OpenAI, Anthropic, self-hosted models — and Everlore learns their data over time to answer questions intelligently.

### Key Features (Planned)

- **Connect to any data source** — Postgres, SQL Server, MySQL, REST APIs, CSV uploads. Users configure connections directly, similar to Power BI's "Get Data" flow.
- **Ad-hoc report builder** — Drag-and-drop interface for building queries visually. Pick dimensions, measures, filters, and chart types with live preview.
- **Interactive dashboards** — Grid-based layouts with resizable widgets, cross-filtering, and click-to-drill.
- **AI-powered queries** — Natural language to report: "Show me revenue by customer for Q4" becomes a chart. Data-aware chat that understands your schema and business context.
- **Bring your own AI** — Tenant-configurable AI provider. API key services (OpenAI, Anthropic) or self-hosted models (Ollama, vLLM).
- **Multi-tenant** — Isolated data per tenant with shared infrastructure. Users authenticate once, then select which tenant to work in.
- **Connector ecosystem** — Managed data ingestion from external systems (QuickBooks, Xero) with scheduled sync and observability.

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Domain & API | .NET 10, ASP.NET Core, Entity Framework Core |
| Orchestration | .NET Aspire |
| Logging | Serilog (structured logging, OTLP export) |
| Observability | SigNoz (self-hosted) via OpenTelemetry |
| Database | PostgreSQL (catalog + per-tenant) |
| Frontend | React, Next.js, TypeScript |
| AI | Provider-agnostic (OpenAI, Anthropic, Ollama, vLLM) |
| Auth | ASP.NET Identity + JWT |

## Project Structure

```
Everlore.slnx
├── Everlore.Domain                    # Domain entities, base abstractions
├── Everlore.Application               # MediatR handlers, validators, common models
├── Everlore.Infrastructure            # EF configurations, generic repository, auth, tenancy
├── Everlore.Infrastructure.Postgres   # Npgsql provider, EF migrations
├── Everlore.QueryEngine               # External DB connections, SQL translation, schema discovery, GraphQL
├── Everlore.Api                       # ASP.NET Core controllers, filters, middleware, SignalR
├── Everlore.Connector.Seed            # Deterministic test data generator
├── Everlore.SyncService               # Background data sync worker
├── Everlore.MigrationService          # EF migrations + dev data seeding
├── Everlore.AppHost                   # .NET Aspire orchestrator (Postgres + Garnet + SigNoz)
└── Everlore.ServiceDefaults           # Shared Aspire configuration (Serilog, OpenTelemetry)
```

### Architecture

- **Catalog DB** — Users, tenants, configurations, data source definitions, report metadata. Managed by `CatalogDbContext` (inherits ASP.NET Identity).
- **Tenant DB** — Business data per tenant (Accounts Payable, Accounts Receivable, Inventory, Sales, Shipping). Managed by `EverloreDbContext`.
- **Tenant resolution** — JWT `tenant` claim (primary) with `X-Tenant-Id` header fallback (Development only). `TenantRequiredMiddleware` rejects any `/api/` request without a tenant context (auth and tenant endpoints are exempt).
- **Generic CRUD** — `CrudController<T>` provides all 5 CRUD operations against `IRepository<T>`. Entity controllers are one-liners that set the route. No DTOs — entities are the API contract.
- **Validation** — FluentValidation with per-entity validators triggered via a global action filter. MediatR's `ValidationBehavior` handles the tenant management path.
- **Tenant management** — Uses MediatR with custom handlers for operations that need cross-entity checks, catalog DB access, and role authorization.

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for Aspire-managed Postgres)

### Running Locally

1. Clone the repository:
   ```bash
   git clone https://github.com/wanderlustgremlin-cloud/Everlore.git
   cd Everlore
   ```

2. Set the JWT signing key in AppHost user secrets:
   ```bash
   cd Everlore.AppHost
   dotnet user-secrets set "Parameters:jwt-signing-key" "your-secret-key-at-least-32-characters-long!"
   ```

3. Run the Aspire AppHost:
   ```bash
   dotnet run --project Everlore.AppHost
   ```

   This starts Postgres, Garnet, SigNoz (ClickHouse + Collector + Query Service + Frontend), runs migrations, seeds a dev tenant with test data, and launches the API.

4. Open the Aspire dashboard (URL shown in terminal output) to see all services.

5. Open SigNoz at `http://localhost:3301` for logs, traces, and metrics.

6. The API is available with Swagger UI at the everlore-api endpoint.

### Dev Credentials

| | |
|---|---|
| Email | `admin@everlore.dev` |
| Password | `Admin123!` |

### Auth Flow

```
1. POST /api/auth/login         → JWT + tenant list
2. POST /api/auth/select-tenant → JWT with tenant claim
3. Use tenant-scoped JWT on all other endpoints
```

## Current Status

Phase 1 (Platform Hardening) is complete. Phase 2 (Reporting API) is in progress:

**Phase 1 — Complete:**
- Paginated & sortable list endpoints on all entities with generic filtering (date ranges, enums, bools, GUIDs)
- Cursor-based pagination as an alternative to offset pagination for large datasets
- Application layer with generic CRUD pattern and MediatR for tenant management
- Global error handling with RFC 7807 ProblemDetails and correlation IDs
- Audit trail: `CreatedBy`/`UpdatedBy` fields on all entities via EF interceptor
- Tenant onboarding API with auto-provisioning of tenant databases and key-value settings
- JWT authentication with role-based authorization
- Security hardening: tenant isolation guard, header spoofing prevention, rate limiting on auth, security headers, config-driven registration

**Phase 2 — In Progress:**
- DataSource and ReportDefinition entities in catalog DB with full CRUD API
- Connection string encryption via ASP.NET Data Protection API
- Connection test endpoint for verifying data source connectivity
- Schema discovery for Postgres, SQL Server, and MySQL
- Analytical query model with dialect-specific SQL translation
- GraphQL explore mode via HotChocolate with dynamic per-tenant schemas
- Real-time query progress via SignalR
- Garnet (Redis-compatible) cache for schema and query results
- Polly resilience (retry + circuit breaker) on external DB connections
- Serilog structured logging with OTLP export to Aspire Dashboard and SigNoz
- SigNoz observability stack (ClickHouse, OTel Collector, Query Service, Frontend) as Aspire containers

See [ROADMAP.md](ROADMAP.md) for the full development plan with detailed progress.

## Roadmap

1. **Platform Hardening** — Pagination, filtering, cursor pagination, correlation IDs, audit trail, tenant provisioning, tenant settings *(complete)*
2. **Reporting API & Ad-Hoc Query Engine** — Data sources, schema discovery, query model, SQL translation, GraphQL, SignalR *(in progress)*
3. **Frontend & Dashboard Builder** — React/Next.js report builder, dashboards, interactive filtering
4. **AI Integration** — Provider abstraction, natural language to query, data-aware chat
5. **Data Pipeline & Connectors** — Connector SDK, QuickBooks/Xero, scheduled sync
6. **Advanced Features** — Scheduled delivery, alerts, anomaly detection, forecasting

## License

Proprietary. All rights reserved.
