# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Test Commands

```bash
dotnet build                                          # debug build (entire solution)
dotnet build --configuration Release                  # release build
dotnet test                                           # run all 14 tests
dotnet test --verbosity normal                        # with test names
dotnet test --filter "FullyQualifiedName~PkceService" # single test class
dotnet format                                         # auto-format code
dotnet format --verify-no-changes                     # check formatting (pre-commit hook uses this)
```

Run the WebAPI: `cd src/Subiekt.Connector.Api && dotnet run` (https://localhost:5001, Swagger at /swagger)
Run the demo: `cd demo/Subiekt.Demo && dotnet run` (https://localhost:5002)

## Architecture

.NET 10 solution with three layers connecting to the **Subiekt 123 API v1.1** (Polish ERP by InsERT):

```
Subiekt.Connector.Api (WebAPI, port 5001)
  └─ Subiekt.Connector.Sdk (standalone class library)
       └─ Subiekt.Connector.Contracts (auto-generated DTOs)
```

- **Contracts** (`src/Subiekt.Connector.Contracts/Contracts.cs`) — 56 auto-generated record types and enums from the OpenAPI spec. **Do not edit manually** — regenerate from `subiekt.json` spec.
- **SDK** (`src/Subiekt.Connector.Sdk/`) — `SubiektClient` facade exposing `Auth`, `Clients`, `Documents`, `Products` resources. Can be used standalone in any .NET project.
- **API** (`src/Subiekt.Connector.Api/`) — ASP.NET Core WebAPI wrapping the SDK. Controllers delegate to `SubiektApiClient` service which manages HTTP calls and auto-refreshes OAuth tokens.
- **Demo** (`demo/Subiekt.Demo/`) — Blazor Server app demonstrating the full OAuth login flow and resource listing.
- **Tests** (`tests/Subiekt.Connector.IntegrationTests/`) — xUnit + FluentAssertions + Moq. No live API calls in the default suite.

### OAuth 2.0 PKCE Flow

Auth goes through InsERT's identity provider (`kontoapi.insert.com.pl`). Key components:
- `PkceService` / `PkceHelper` — generates code_verifier/code_challenge pairs
- `IOAuthStateCache` — stores pending OAuth states with 5-min TTL (CSRF protection)
- `ITokenStore` / `InMemoryTokenStore` — holds tokens; `SubiektApiClient` auto-refreshes on expiry

Flow: `GET /auth/login` → InsERT login → callback with code → exchange for tokens → stored in `ITokenStore`

### API Patterns

- All IDs are `Guid` (v1.1 breaking change from v1.0 `int`)
- PUT operations require `If-Match: "<etag>"` header from the preceding GET
- All upstream requests need headers: `Authorization: Bearer`, `Ocp-Apim-Subscription-Key`, `x-api-version: 1.1`
- JSON serialization uses `System.Text.Json` with `PropertyNameCaseInsensitive = true` and `JsonStringEnumConverter`
- All I/O methods are async with `CancellationToken` support

## Git Hooks

Run `./setup-hooks.sh` once after clone. Hooks:
- **pre-commit**: `dotnet format --verify-no-changes` — fix with `dotnet format` before committing
- **pre-push**: `dotnet build --configuration Release` + `dotnet test`

## CI

GitHub Actions (`.github/workflows/build.yml`) runs on push to `main` and PRs: restore, build (Release), test with coverage, fail if line coverage < 5%.

## Configuration

OAuth credentials go in `appsettings.json` under the `"Subiekt"` section (`ClientId`, `ClientSecret`, `SubscriptionKey`, `RedirectUri`). These must never be committed — use `appsettings.Development.json` or user secrets.
