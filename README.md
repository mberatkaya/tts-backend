# TTS Backend Foundation

This repository contains the backend foundation for a secure Text-to-SQL system built with ASP.NET Core Web API on .NET 8. The current goal is to keep the backend small, modular, and ready for the next implementation phase without adding authentication, frontend work, or real LLM integration yet.

## Architecture

The backend is split into four layers:

- `src/Api`
  - controller endpoint
  - request and response DTOs
  - API-specific DI registration
  - global exception middleware
- `src/Application`
  - orchestration service
  - interfaces
  - question validation
  - SQL builder abstractions
  - policy placeholders
- `src/Domain`
  - AI plan models
  - schema metadata models
  - validation and execution result models
- `src/Infrastructure`
  - fake AI query planner
  - fake schema metadata provider
  - SQL safety validator
  - SQL execution layer
  - result formatter
  - infrastructure registration

### Architecture Notes

- The AI planner only receives the user question and sanitized schema metadata.
- The backend is the only layer allowed to validate and execute SQL.
- SQL execution remains behind `ISqlQueryExecutor` so the database boundary stays explicit.
- The current validator is string-based by design, but it now has clearer extension points for a parser-based validator later.
- Schema metadata is sanitized before it reaches the planner, which keeps blocked or sensitive columns out of the AI context.

## Request Flow

`POST /api/text-to-sql/query`
-> `TextToSqlController`
-> `ITextToSqlService`
-> `ISchemaMetadataProvider`
-> `IAiQueryPlanner`
-> role-based SQL policy placeholder
-> `ISqlSafetyValidator`
-> `ISqlQueryExecutor`
-> `IResultFormatter`
-> response DTO

## Implemented Foundation

- One endpoint: `POST /api/text-to-sql/query`
- Clean DTOs for request, response, and API errors
- Required abstractions:
  - `ITextToSqlService`
  - `IAiQueryPlanner`
  - `ISchemaMetadataProvider`
  - `ISqlSafetyValidator`
  - `ISqlQueryExecutor`
  - `IResultFormatter`
- Fake AI planner for local development
- Fake schema metadata provider with:
  - configured metadata support
  - built-in sample schema fallback
  - blocked and sensitive column filtering
- SQL safety validator with:
  - `SELECT`-only enforcement
  - forbidden keyword checks
  - semicolon rejection
  - comment rejection
  - allowed-table validation
  - allowed-column validation
  - wildcard projection control
  - max-row enforcement through `TOP`
- Global exception handling middleware
- DI registration through extension methods in `Api`, `Application`, and `Infrastructure`
- Strongly typed options for:
  - `ConnectionStrings`
  - `TextToSql`
  - `AiProvider`
  - `QuerySafety`

## Configuration

Sample configuration lives in [src/Api/appsettings.json](./src/Api/appsettings.json).

Important sections:

- `ConnectionStrings`
  - placeholder SQL Server connection string for the future read-only login
- `TextToSql`
  - execution toggle
  - sample schema toggle
  - default schema
  - allowed tables and relationships
- `AiProvider`
  - fake provider settings plus placeholders for a future real provider
- `QuerySafety`
  - row limit
  - comment rejection
  - wildcard projection control
  - table whitelist switch
  - column whitelist switch
  - blocked columns

## Running

1. Restore packages:

```bash
dotnet restore Backend.sln
```

2. Run the API:

```bash
dotnet run --project src/Api
```

3. Send a request to `POST /api/text-to-sql/query`.

By default, query execution is disabled, so the API runs planning and validation but does not call SQL Server.

## Next-Phase TODOs

- replace the fake planner with a real LLM-backed planner
- move from regex-based validation to parser-based SQL validation
- generate SQL from structured plan objects instead of only consuming demo SQL strings
- connect schema metadata to controlled database introspection
- add authenticated caller context and real role-based row filtering
- add tests around validator edge cases and service orchestration
- harden operational concerns such as audit logging, rate limits, and observability
