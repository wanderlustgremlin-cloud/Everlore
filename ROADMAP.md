# Everlore Development Roadmap

Everlore is a multi-tenant BI platform that lets users connect to any data source, build ad-hoc reports, and leverage AI to query their data conversationally.

---

## Phase 1 — Platform Hardening

Foundation work that everything else builds on. Prevents rework as features layer in.

### 1.1 Pagination, Sorting & Filtering ✅
- [x] Offset pagination on all list endpoints (`PaginationQuery` with page/pageSize)
- [x] Sorting support (field + direction via `SortBy`/`SortDir` query params)
- [x] Consistent query parameter conventions across all controllers (generic `CrudController<T>`)
- [x] `PagedResult<T>` response model with totalCount, totalPages, hasNextPage, hasPreviousPage
- [x] Generic reflection-based filtering: exact match (string, bool, Guid, enum), date ranges (`From`/`To` suffixes), numeric types
- [x] Cursor-based pagination (`?after=<cursor>`) with `CursorPagedResult<T>`, Base64-encoded cursor, deterministic secondary sort by Id

### 1.2 Application Service Layer ✅
- [x] `Everlore.Application` project with MediatR commands/queries
- [x] Tenant management handlers with custom logic (7 handlers via MediatR)
- [x] Generic `CrudController<T>` for standard CRUD — bypasses MediatR, talks to `IRepository<T>` directly
- [x] FluentValidation with per-entity validators triggered via `FluentValidationFilter`
- [x] `ValidationBehavior` pipeline for MediatR path (tenant handlers)
- [x] `LoggingBehavior` pipeline for MediatR path
- [x] Common models: `Result<T>`, `PagedResult<T>`, `PaginationQuery`

**Design decision:** No DTOs — entities are the API contract. Navigation properties aren't loaded (no lazy loading, no `.Include()`), so they serialize as empty collections. Validators operate on entities directly (`AbstractValidator<Vendor>`) instead of command records. MediatR is reserved for handlers with genuinely custom logic (tenant management).

### 1.3 Global Error Handling ✅
- [x] RFC 7807 ProblemDetails for all error responses
- [x] `GlobalExceptionHandler` middleware with consistent error shape
- [x] Structured validation error formatting (FluentValidation → `ValidationException` → 422 ProblemDetails with grouped errors)
- [x] Not found handling (`NotFoundException` → 404 ProblemDetails)
- [x] Correlation IDs: `CorrelationIdMiddleware` reads/generates `X-Correlation-Id`, sets `TraceIdentifier`, wraps request in logging scope, includes in error ProblemDetails

### 1.4 Audit Trail ✅
- [x] `CreatedBy`/`UpdatedBy` fields on `BaseEntity` (nullable Guid — null for system operations)
- [x] `AuditSaveChangesInterceptor` sets audit fields from `ICurrentUser.UserId` on both DbContexts
- [x] `Repository.SetValues` preserves both `CreatedAt` and `CreatedBy` on update
- [x] `CrudController` no longer manually assigns timestamps — `SetTimestamps()` and interceptor handle everything

### 1.5 Tenant Onboarding API ✅
- [x] Admin endpoints to create/update tenants (SuperAdmin only)
- [x] Tenant user management: list, add, remove users (SuperAdmin + Admin)
- [x] Role-based authorization on tenant endpoints
- [x] SuperAdmin role seeded in dev environment
- [x] Programmatic tenant database provisioning: `PostgresTenantDatabaseProvisioner` auto-creates Postgres DB + runs migrations when `ConnectionString` is omitted from `CreateTenantCommand`
- [x] Tenant settings: key-value config per tenant (`TenantSetting` entity in catalog DB) with GET/PUT/DELETE endpoints, `WellKnownSettings` constants for AI provider, feature flags, branding
- [ ] Replace manual seeding with a self-service flow

### 1.6 Authentication ✅
- [x] ASP.NET Identity with JWT tokens
- [x] Login, register, and tenant-scoped token exchange
- [x] Identity roles (SuperAdmin) included in JWT claims
- [x] Tenant claim in JWT for scoped access
- [x] `ICurrentUser` abstraction for accessing authenticated user info
- [x] Dev user seeded: admin@everlore.dev / Admin123!

### 1.7 Security Hardening ✅
- [x] `TenantRequiredMiddleware` — rejects `/api/` requests without tenant context (auth/tenants exempt); defense-in-depth dummy connection string replaces catalog DB fallback
- [x] `X-Tenant-Id` header fallback restricted to Development environment only (prevents header spoofing in production)
- [x] Rate limiting on auth endpoints — login: 10 req/min/IP, register: 3 req/15min/IP (built-in `Microsoft.AspNetCore.RateLimiting`)
- [x] `SecurityHeadersMiddleware` — X-Content-Type-Options, X-Frame-Options, CSP, Referrer-Policy, Permissions-Policy on all responses (CSP skipped for Swagger paths)
- [x] HSTS enabled in non-Development environments
- [x] CORS policy for frontend (config-driven origins, localhost:3000 in dev)
- [x] Config-driven registration mode — `Open`, `InviteOnly`, or `Disabled` via `Registration__Mode` env var

---

## Phase 2 — Reporting API & Ad-Hoc Query Engine ✅

The core product. Users connect to external databases, browse schemas, build queries, and save reports.

### 2.1 Data Source Abstraction ✅
- [x] DataSource entity in catalog DB: TenantId, Name, Type (PostgreSql/SqlServer/MySql), encrypted connection string
- [x] Connection string encryption via ASP.NET Data Protection API (auto key rotation)
- [x] Full CRUD API: DataSourcesController with MediatR handlers (create, update, delete, list, get)
- [x] Connection test endpoint: POST /datasources/{id}/test
- [x] Tenant-scoped: all operations enforce tenant isolation via ICurrentUser.TenantId

### 2.2 Schema Discovery ✅
- [x] Per-dialect schema introspectors (Postgres, SQL Server, MySQL) via Dapper against information_schema
- [x] Primary key detection via system catalogs (pg_constraint, sys.indexes, KEY_COLUMN_USAGE)
- [x] TypeNormalizer maps native types to normalized set: String, Integer, Decimal, DateTime, Boolean, Guid, Other
- [x] Schema cached in Garnet with 1-hour TTL; SchemaLastRefreshedAt tracked on DataSource entity
- [x] API: GET /datasources/{id}/schema (cached) and POST /datasources/{id}/schema/refresh (force)

### 2.3 Query Model + Execution Engine ✅
- [x] QueryDefinition: measures (Sum/Count/Avg/Min/Max/CountDistinct), dimensions (with date bucketing), filters, sorts, limit/offset
- [x] Dialect-specific SQL translators: Postgres (DATE_TRUNC, LIMIT/OFFSET), SQL Server (DATEPART/OFFSET FETCH), MySQL (DATE_FORMAT, LIMIT)
- [x] All filter values parameterized via DynamicParameters; column names validated against cached schema
- [x] Query results cached in Garnet (5-min TTL); row limit enforced (10k default); 60s query timeout
- [x] API: POST /queries/execute (ad-hoc) and POST /reports/{id}/execute (saved report)

### 2.4 Report Definitions ✅
- [x] ReportDefinition entity in catalog DB: DataSourceId, QueryDefinitionJson, VisualizationConfigJson, IsPublic, Version
- [x] Full CRUD API: ReportsController with MediatR handlers
- [x] Version auto-increment on update; tenant-scoped with DataSource FK

### 2.5 GraphQL Explore Mode ✅
- [x] HotChocolate GraphQL endpoint at /graphql with JWT authorization
- [x] `explore` query: field selection → SELECT with per-dialect quoting via Dapper
- [x] `dataSourceSchema` query: introspect tables/columns for a data source
- [x] Dynamic type mapping: NormalizedType → GraphQL scalars (String, Int, Float, DateTime, Boolean, UUID)

### 2.6 Real-Time Progress ✅
- [x] SignalR hub at /hubs/query with tenant-scoped groups
- [x] Strongly-typed client: QueryProgress, QueryCompleted, QueryFailed messages
- [x] JWT via query string for WebSocket connections
- [x] Redis backplane via StackExchange.Redis for multi-instance support

### 2.7 Infrastructure ✅
- [x] Everlore.QueryEngine project: isolates external DB concerns (Dapper, Npgsql, SqlClient, MySqlConnector)
- [x] Polly resilience: retry (3 attempts, exponential backoff), circuit breaker (50% failure ratio, 30s break), 30s timeout
- [x] Garnet (Redis-compatible) cache via Aspire with data volume
- [x] TenantRequiredMiddleware expanded to guard /graphql and /hubs paths
- [x] Serilog structured logging: Console sink + dual OTLP sinks (Aspire Dashboard + SigNoz)
- [x] SigNoz observability stack as Aspire container resources (ClickHouse, OTel Collector, Query Service, Frontend)

### 2.8 Export — not started
- CSV and Excel export from query results
- PDF export as a later addition

---

## Phase 3 — Frontend & Dashboard Builder

React/Next.js frontend that makes the query engine accessible to users.

### 3.1 Project Scaffolding
- Next.js app with TypeScript
- Authentication flow — login, tenant selection, JWT token management
- API client layer with typed request/response models
- Component library and design system foundations

### 3.2 Report Builder UI
- Drag-and-drop interface for building queries visually
- Field picker — browse connected data sources and their schemas
- Drag dimensions onto rows/columns, measures onto values, fields onto filters
- Chart type selector with live preview
- Raw data table view alongside visualizations
- Save, name, and share reports
- This is the flagship feature

### 3.3 Dashboard Framework
- Grid layout of widgets, each backed by a saved report
- Resizable and rearrangeable widget tiles
- Multi-dashboard support — users create and organize their own
- Sensible default dashboards per domain (AP Overview, AR Overview, Inventory, Sales)

### 3.4 Interactive Filtering
- Global date range picker that applies across all widgets on a dashboard
- Click-to-drill — click a bar in a chart to filter the dashboard by that value
- Cross-widget filtering — selecting a dimension value in one widget filters others

### 3.5 Visualization Types
- Line charts (trends over time)
- Bar/column charts (comparisons)
- Tables with sorting and pagination (detail views)
- KPI cards (single metric with trend indicator)
- Pie/donut charts (breakdowns)
- Scatter plots, area charts as the library matures

### 3.6 Auth & Profile UI
- Login page with email/password
- Tenant picker after authentication
- User profile management
- Tenant settings for admins (data sources, users, AI configuration)

---

## Phase 4 — AI Integration

Two tracks: bring-your-own AI provider, and an in-app AI that understands the tenant's data.

### 4.1 AI Provider Abstraction
- Tenant configures their preferred AI provider in settings
- Support API key services: OpenAI, Anthropic, Google, etc.
- Support self-hosted models: Ollama, vLLM, llama.cpp, etc.
- Provider interface with common contract: chat completion, embeddings
- Tenant brings their own credentials — Everlore doesn't intermediate billing

### 4.2 Natural Language to Query
- User types "Show me revenue by customer for Q4" in a chat or search bar
- AI translates natural language into the query model from Phase 2
- The execution engine runs it, results render as a chart or table inline
- The query model is the backbone — AI is a translation layer on top of it

### 4.3 Data-Aware Chat
- AI has context about the tenant's connected schemas, data relationships, and recent results
- Can answer analytical questions: "Why did revenue drop in March?", "Who are our top 5 vendors by spend?"
- Suggests follow-up queries based on what the user is exploring
- In-app chat interface alongside dashboards and reports

### 4.4 External CLI / Tool Support
- Users can bring whatever CLI tool they want and integrate it with Everlore
- API surface that external tools can call to execute queries, fetch schema, read results
- Webhook or event system so external tools can react to data changes

### 4.5 Learning & Memory
- AI builds context over time per tenant
- Remembers common queries, business-specific terminology, user preferences
- Tenant-scoped knowledge: "big customers" means >$100k annual, fiscal year starts in April, etc.
- Users can teach the AI definitions and it applies them going forward

---

## Phase 5 — Data Pipeline & Connectors

Real-world data ingestion. Deferred because the seed connector handles dev/test needs, and the query engine already supports connecting to external sources directly. Connectors add managed, scheduled syncing into the canonical data model.

### 5.1 Connector SDK
- Formalized contract for pulling data from external systems
- Support for incremental sync: last-modified cursors, pagination tokens, high-water marks
- Built-in error reporting, retry logic, and rate limiting
- Each connector is also exposed as a data source for the report builder

### 5.2 Real-World Connectors
- QuickBooks Online and Xero as first targets (AP/AR/Inventory domain fit)
- OAuth integration flows for each provider
- CSV/Excel bulk import connector for users migrating data

### 5.3 Scheduled Sync
- Recurring sync jobs configurable per connector per tenant
- Sync intervals: manual, hourly, daily, custom cron
- Replace one-shot SyncService with a durable job system

### 5.4 Sync Observability
- Track last sync time, record counts, errors per connector per tenant
- Sync history log with success/failure details
- Expose via API and surface in the frontend admin UI
- Data freshness indicators on dashboards

### 5.5 Pre-Computed Metrics
- KPIs that auto-calculate on sync completion for the canonical model
- Days Sales Outstanding, Days Payable Outstanding, Inventory Turnover
- Gross Margin, Revenue by period, Aging buckets
- Time-series snapshots: capture KPI values at regular intervals for trend analysis

---

## Phase 6 — Advanced Features

Differentiators that move Everlore from a reporting tool to a full BI platform.

### 6.1 Scheduled Report Delivery
- Email a PDF or CSV of a report on a configurable schedule (daily, weekly, monthly)
- Recipients list per scheduled report
- Useful for executives and stakeholders who don't log into the app

### 6.2 Threshold Alerts
- Rule-based notifications: "Alert me when DSO exceeds 45 days"
- Configurable per metric, per tenant, per user
- Delivery via email, in-app notification, or webhook

### 6.3 Anomaly Detection
- Flag metrics that deviate significantly from their historical trend
- Statistical approach: z-score against rolling average (no ML required)
- Surface anomalies in dashboards and via alerts

### 6.4 Forecasting
- Simple trend extrapolation (linear regression) for revenue, cash flow, inventory levels
- Visual overlay on time-series charts: actual vs. forecast
- High perceived value for relatively low implementation cost

### 6.5 Embeddable Widgets
- Let tenants embed a chart or KPI card in their own tools
- Iframe or JavaScript snippet with authentication token
- Configurable styling to match the host application

---

## Architecture Notes

### The Query Engine Is the Backbone
The query model and execution engine from Phase 2 are the single most important architectural component. Everything builds on top of them:
- The **frontend** sends query models to the API
- The **AI** translates natural language into query models
- **Connectors** are just another data source the engine can query
- **Exports** serialize query results
- **Dashboards** are collections of saved query models with layout metadata

### Technology Stack
| Layer | Technology |
|-------|-----------|
| Domain & API | .NET 10, ASP.NET Core, Entity Framework Core |
| Query Engine | Dapper, HotChocolate (GraphQL), Polly |
| Logging | Serilog (structured logging, OTLP export) |
| Observability | SigNoz (self-hosted), OpenTelemetry, Aspire Dashboard |
| Cache | Garnet (Redis-compatible) via Aspire |
| Real-time | SignalR with Redis backplane |
| Orchestration | .NET Aspire |
| Database | PostgreSQL (catalog + per-tenant), SQL Server, MySQL (external sources) |
| Frontend | React, Next.js, TypeScript |
| AI | Provider-agnostic (OpenAI, Anthropic, Ollama, vLLM) |
| Auth | ASP.NET Identity + JWT |

### Multi-Tenancy Model
- **Catalog DB**: users, tenants, configurations, data source definitions, report metadata
- **Tenant DB**: business data (synced from connectors or queried live from connected sources)
- Users authenticate before tenant is known; pick tenant to get a scoped JWT

### Project Structure
| Project | Purpose |
|---------|---------|
| `Everlore.Domain` | Pure domain entities, no infrastructure deps |
| `Everlore.Application` | MediatR handlers (tenancy), validators, common models |
| `Everlore.Infrastructure` | EF configs, repos, auth, tenancy (provider-agnostic) |
| `Everlore.Infrastructure.Postgres` | Npgsql, migrations, Postgres-specific DI |
| `Everlore.QueryEngine` | External DB connections, SQL translation, schema discovery, GraphQL resolvers |
| `Everlore.Api` | ASP.NET Core controllers, filters, middleware, SignalR hub |
| `Everlore.Connector.Seed` | Deterministic test data generator |
| `Everlore.SyncService` | Background sync worker |
| `Everlore.MigrationService` | EF migrations + dev data seeding |
| `Everlore.AppHost` | Aspire orchestrator (Postgres + Garnet + SigNoz) |
| `Everlore.ServiceDefaults` | Shared Aspire config (Serilog, OpenTelemetry) |
