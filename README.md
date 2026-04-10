# TTS Backend Foundation

This repository contains the first backend foundation for a secure Text-to-SQL system built with ASP.NET Core Web API on .NET 8. The goal of this phase is to establish the architecture, contracts, and security boundaries for future work, not to ship a full production implementation yet.

## Architecture

The solution uses a layered architecture with clear boundaries:

- `src/TTS.Api`
  - HTTP entry point
  - controllers
  - request and response DTOs
  - global exception middleware
  - configuration host setup
- `src/TTS.Application`
  - use-case orchestration
  - service interfaces
  - validators
  - SQL builder abstraction
  - role-based policy placeholder
  - options and DI registration
- `src/TTS.Domain`
  - schema metadata models
  - AI query plan contract
  - validation and execution result models
  - text-to-sql response model
- `src/TTS.Infrastructure`
  - fake AI query planner
  - configuration-backed schema metadata provider
  - SQL safety validator
  - SQL execution layer
  - result formatter
  - infrastructure options and DI registration

## Request Flow

The current request flow is:

`POST /api/text-to-sql/query`
-> controller receives `question`
-> `ITextToSqlService`
-> `ISchemaMetadataProvider` returns sanitized allowed schema metadata
-> `IAiQueryPlanner` receives only the question and allowed schema metadata
-> role-based SQL policy placeholder
-> `ISqlSafetyValidator` validates and normalizes SQL
-> `ISqlQueryExecutor` executes SQL only when execution is enabled
-> `IResultFormatter` prepares the result payload
-> response DTO returned to the client

Important boundary:

- The AI planner never receives a SQL connection or direct database access.
- The backend is the only layer allowed to validate and execute SQL.
- SQL execution is disabled by default in configuration for this foundation phase.

## Implemented In This Phase

- Layered solution with `Api`, `Application`, `Domain`, and `Infrastructure` projects
- One controller endpoint: `POST /api/text-to-sql/query`
- Request DTO:

```json
{
  "question": "string"
}
```

- Response DTO:

```json
{
  "success": true,
  "question": "string",
  "generatedSql": "string",
  "rows": [],
  "warnings": [],
  "confidence": 0.0
}
```

- Required abstractions:
  - `ITextToSqlService`
  - `IAiQueryPlanner`
  - `ISchemaMetadataProvider`
  - `ISqlSafetyValidator`
  - `ISqlQueryExecutor`
  - `IResultFormatter`
- Strict AI output contract through `AiQueryPlan`
- Sanitized schema metadata models:
  - `AllowedSchemaMetadata`
  - `TableMetadata`
  - `ColumnMetadata`
  - `RelationshipMetadata`
- Security foundation:
  - only `SELECT` allowed
  - blocks `UPDATE`, `DELETE`, `INSERT`, `DROP`, `ALTER`, `TRUNCATE`, `EXEC`, `MERGE`, `INTO`
  - rejects semicolon-separated statements
  - rejects comments
  - enforces a maximum row limit
  - prepares table whitelist checks
  - prepares sensitive column blocking
  - includes a role-based filtering placeholder
- SQL execution skeleton using `Microsoft.Data.SqlClient`
- Fake AI planner for safe local development
- Global exception middleware with consistent API error responses
- Strongly typed configuration for:
  - `ConnectionStrings`
  - `TextToSql`
  - `AiProvider`
  - `QuerySafety`

## Running

1. Restore packages:

```bash
dotnet restore
```

2. Run the API:

```bash
dotnet run --project src/TTS.Api
```

3. Send a sample request with the included `.http` file or a client such as `curl`.

By default, the fake planner will return a demo SQL query and the executor will skip the database call because `TextToSql:EnableQueryExecution` is set to `false`.

## What Is Intentionally Left For The Next Phase

- real LLM provider integration
- parser-based SQL validation
- richer SQL generation from structured plans instead of demo SQL strings
- authenticated caller identity and role-aware filters
- auditing and query history
- prompt engineering and guardrail tuning
- schema discovery from a real database
- production-ready read-only database credentials and operational hardening
