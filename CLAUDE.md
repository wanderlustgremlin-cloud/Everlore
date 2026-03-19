# CLAUDE.md

Project-level instructions for AI assistants working on Everlore.

## What Is Everlore

Multi-tenant BI SaaS platform — users connect to any data source (Postgres, SQL Server, MySQL, REST APIs, CSV), build ad-hoc reports, and query data conversationally via AI. Built on .NET 10 / Aspire / Postgres with React/Next.js frontend.

## Architecture Rules

- **No DTOs** — entities are the API contract. Navigation properties aren't loaded so they serialize as empty collections.
- **Generic CRUD** — `CrudController<T>` handles all 5 operations against `IRepository<T>`; entity controllers are one-liners.
- **MediatR only for custom logic** — tenant management (7 handlers) uses MediatR; standard CRUD bypasses it entirely.
- **Validators on entities** — `AbstractValidator<Vendor>` not `AbstractValidator<CreateVendorCommand>`; triggered via `FluentValidationFilter`.
- **Multi-tenant isolation** — catalog DB (users, tenants, config) + per-tenant DB (business data). JWT auth with tenant claim.
- **AI provider abstraction** — support API key services (OpenAI, Anthropic) AND self-hosted (Ollama, vLLM).
- **Hybrid hosting** — tenants choose SaasHosted (central DB) or SelfHosted (gateway agent on their network).

## Project Structure

```
Everlore.Domain                    # Pure domain entities, no infra deps
Everlore.Application               # MediatR tenant handlers, validators, common models
Everlore.Infrastructure            # EF configs, generic Repository<T>, auth, tenancy
Everlore.Infrastructure.Postgres   # Npgsql, migrations, DI wiring
Everlore.QueryEngine               # External DB connections (Dapper), SQL translation, schema discovery
Everlore.Api                       # Controllers, GraphQL, SignalR hubs, middleware
Everlore.Gateway.Contracts         # Shared message types for gateway communication
Everlore.Gateway                   # On-premise gateway agent (outbound SignalR)
Everlore.Connector.Seed            # Deterministic test data generator
Everlore.SyncService               # Background sync worker
Everlore.MigrationService          # EF migrations + dev data seeding
Everlore.AppHost                   # Aspire orchestrator
Everlore.ServiceDefaults           # Shared Aspire config (Serilog, OTLP, health checks)
```

## Development Conventions

- Dev user: `admin@everlore.dev` / `Admin123!`
- Seed connector exists to defer real connector complexity — dev environment uses deterministic data.
- On every commit: update `README.md`, `ROADMAP.md`, and memory files to reflect changes.

## Key Infrastructure Patterns

- **TenantRequiredMiddleware** — rejects API/GraphQL/SignalR requests without tenant context (auth/tenants/gateway exempt).
- **Cursor pagination** — `?after=<cursor>` for cursor mode; `Cursor.Encode/Decode` uses Base64-JSON; secondary sort by Id.
- **Correlation IDs** — `X-Correlation-Id` header, set on `TraceIdentifier`, included in all ProblemDetails responses.
- **Audit trail** — `CreatedBy`/`UpdatedBy` on `BaseEntity` via `AuditSaveChangesInterceptor`.
- **Rate limiting** — `auth` policy (10/min), `register` policy (3/15min).
- **Garnet cache** — schema cache (1h TTL), query result cache (5min TTL).
- **QueryEngine resilience** — Polly retry 3x, circuit breaker, 30s timeout.
- **Gateway routing** — decorator pattern (`GatewayQueryExecutionService`, `GatewaySchemaService`, `GatewayRepository<T>`) checks `HostingMode` and routes SelfHosted through SignalR.
- **Serilog** — Console + dual OTLP sinks (Aspire Dashboard + SigNoz).

## Tech Notes

- OpenApi v2 (Microsoft.OpenApi 2.4.1): types in `Microsoft.OpenApi` namespace, not `Microsoft.OpenApi.Models`.
- `AddSecurityRequirement` in Swashbuckle 10.x takes a `Func<OpenApiDocument, OpenApiSecurityRequirement>` delegate.
