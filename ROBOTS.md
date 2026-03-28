# ROBOTS.md — Agent Integration Guide

> This file is for AI agents, coding assistants, and automated tools integrating with the Subiekt 123 Connector.
> It follows the emerging `ROBOTS.md` convention (analogous to `HUMANS.md`/`AGENTS.md`).

---

## Repository Overview

**Repo:** `Daemon-Penguins/subiekt-connector`  
**Stack:** .NET 10, C#, Blazor Server, xUnit, OAuth 2.0 PKCE  
**API:** Subiekt 123 v1.1 by InsERT (Polish ERP)  
**Base URL:** `https://api.subiekt123.pl/1.1`  
**Auth URL:** `https://kontoapi.insert.com.pl`

---

## Project Map

```
src/Subiekt.Connector.Api/          → WebAPI (ASP.NET Core, port 5001)
src/Subiekt.Connector.Sdk/          → Class library SDK (no hosting required)
demo/Subiekt.Demo/                  → Blazor Server demo (port 5002)
tests/Subiekt.Connector.IntegrationTests/ → xUnit tests
.githooks/                          → pre-commit (format), pre-push (build+test)
docs/TESTING.md                     → full test guide
```

---

## Key Namespaces

| Namespace | Purpose |
|-----------|---------|
| `Subiekt.Connector.Api.Contracts` | All DTOs — auto-generated from OpenAPI v1.1 spec |
| `Subiekt.Connector.Api.Auth` | OAuth services: `IPkceService`, `ITokenStore`, `IOAuthStateCache` |
| `Subiekt.Connector.Api.Services` | `ISubiektApiClient` / `SubiektApiClient` (HttpClient wrapper) |
| `Subiekt.Connector.Sdk` | `SubiektClient` — main SDK entry point |
| `Subiekt.Connector.Sdk.Models` | SDK DTOs — auto-generated, same source as Contracts |
| `Subiekt.Connector.Sdk.Auth` | `PkceHelper`, `SubiektAuthClient`, `TokenInfo` |

---

## Contract Generation

All DTOs are **auto-generated from the official OpenAPI v1.1 spec** stored in `subiekt.json`.

- Generator script: Python, reads `components/schemas` from spec
- Output: `src/Subiekt.Connector.Api/Contracts/GeneratedContracts.cs` (56 types)
- Output: `src/Subiekt.Connector.Sdk/Models/Models.cs` (56 types, same source)
- All enums have `[JsonConverter(typeof(JsonStringEnumConverter))]`
- All records use `[property: JsonPropertyName("camelCaseName")]`
- Required params come before optional in record constructors
- IDs are `Guid` (not `int`) — v1.1 breaking change from v1.0

**To regenerate after spec update:**
```python
# Read subiekt.json, parse schemas, generate C# records + enums
# Required → Optional param ordering
# Unique name disambiguation for duplicate short names:
#   ClientDto → ClientDto (GetClientByExternalId)
#   ClientListItemDto (GetClientList)
#   DocumentListItemDto (GetDocumentList)
#   ProductListItemDto (GetProductList)
```

---

## OAuth 2.0 PKCE Flow

```
1. PkceHelper.Create()          → PkceState { State, CodeVerifier, CodeChallenge }
2. SubiektAuthClient.BuildAuthorizationUrl(challenge, state)
   → https://kontoapi.insert.com.pl/connect/authorize?...
3. User authenticates → InsERT redirects to redirect_uri?code=X&state=Y
4. Validate state from IOAuthStateCache (5-min TTL)
5. SubiektAuthClient.ExchangeCodeAsync(code, codeVerifier)
   → POST https://kontoapi.insert.com.pl/connect/token
   → returns TokenInfo { AccessToken, RefreshToken, ExpiresAt }
6. ITokenStore.StoreTokens(accessToken, refreshToken, expiresAt)
7. Auto-refresh: if IsExpired && RefreshToken != null → RefreshAsync()
```

**Endpoints:**
- `GET /auth/login` → initiates flow, redirects to InsERT
- `GET /auth/callback?code=&state=` → exchanges code for token
- `GET /hook/callback?code=&state=` → same as callback (use as redirect_uri)
- `POST /auth/refresh` → manual refresh
- `GET /auth/status` → `{ authorized, expired, hasRefreshToken }`
- `POST /auth/logout` → clear tokens

---

## API Resources

### Clients `/clients`

```
GET    /clients?pageNumber=1&pageSize=25&filters=name~text&orderBy=name:asc
GET    /clients/{guid}
POST   /clients                      body: CreateClientDto
PUT    /clients/{guid}               body: UpdateClientDto, headers: If-Match: "etag"
DELETE /clients/{guid}
```

Filter fields: `name~`, `kind=`, `group=`, `nip=`

### Documents `/documents`

```
GET    /documents?pageNumber=1&pageSize=25&filters=issueDate_From=2024-01-01
GET    /documents/{guid}
POST   /documents                    body: CreateDocumentDto
PUT    /documents/{guid}             body: UpdateDocumentDto, headers: If-Match: "etag"
DELETE /documents/{guid}
POST   /documents/{guid}/printing    body: DocumentPrintingSettingsDto → returns PDF bytes
```

Filter fields: `documentNumber=`, `client~`, `issueDate_From=`, `issueDate_To=`

### Products `/products`

```
GET    /products?pageNumber=1&pageSize=25&filters=kind=Good
GET    /products/{guid}
POST   /products                     body: CreateProductDto
PUT    /products/{guid}              body: UpdateProductDto, headers: If-Match: "etag"
DELETE /products/{guid}              (deactivates, does not permanently delete)
```

Filter fields: `name~`, `kind=`, `group=`

---

## Headers (required on all API requests)

```
x-api-version: 1.1
Ocp-Apim-Subscription-Key: <subscription_key>
Authorization: Bearer <access_token>
```

For PUT: additionally `If-Match: "<etag>"` (value from previous GET response header `ETag`)

---

## Key Types

```csharp
// Enums (all serialized as strings)
ClientKind        { Company, Person }
DocumentKind      { Invoice, Receipt, Proforma }
PaymentMethod     { Cash, CashOnDelivery, BankTransfer, CreditCard, ElectronicPayment, Setoff }
PaymentState      { Paid, Unpaid, Overdue, NotSubjectToPayment }
ProductKind       { Good, Service }
TinKind           { Nip, Vatin, Pesel }
CalculationMethod { Net, Gross }
SplitPayment      { False, True, ManualTrue }
InvoiceMode       { Traditional }

// Result types
CreateClientResultDto   { ClientIdDto Id }       // Id.Value = Guid
CreateDocumentResultDto { DocumentIdDto Id }
CreateProductResultDto  { ProductIdDto Id }
```

---

## SDK Entry Point

```csharp
var sdk = new SubiektClient(options);
// sdk.Auth    → SubiektAuthClient
// sdk.Clients → ClientsResource  (ListAsync, GetAsync, CreateAsync, UpdateAsync, DeleteAsync)
// sdk.Documents → DocumentsResource  (+ PrintAsync)
// sdk.Products  → ProductsResource
// sdk.SetToken(tokenInfo)
// sdk.GetToken()
// sdk.IsAuthorized
```

---

## Test Commands

```bash
dotnet test                                         # all tests
dotnet test --verbosity normal                      # with names
dotnet test tests/Subiekt.Connector.IntegrationTests  # specific project
dotnet test --filter "FullyQualifiedName~PkceService" # specific class
```

14 tests, all should pass. No live API calls in default suite.

---

## Build Commands

```bash
dotnet restore
dotnet build
dotnet build --configuration Release
dotnet format                    # auto-format all code
dotnet format --verify-no-changes  # check formatting (used in pre-commit hook)
```

---

## Known Design Decisions

| Decision | Reason |
|----------|--------|
| Contracts auto-generated from OpenAPI | Zero drift between spec and code |
| `InMemoryTokenStore` as default | Simplicity — swap for DB/Redis in production |
| `IOAuthStateCache` with 5-min TTL | Prevents CSRF; pending states expire automatically |
| Removed Docker HEALTHCHECK | nginx:alpine wget unreliable; Traefik v3 ignores unhealthy containers |
| Internal Docker network required | Traefik v3 ignores containers on external-only networks |
| All IDs as `Guid` | API v1.1 breaking change from v1.0 (was `int`) |
| `pre-commit` uses `--verify-no-changes` | Fails fast instead of silently reformatting |

---

## Do NOT

- Do not store tokens in source code or `appsettings.json` committed to git
- Do not use `InMemoryTokenStore` in multi-instance/load-balanced deployments
- Do not call `/auth/login` from server-to-server flows — it requires user interaction
- Do not modify `GeneratedContracts.cs` or `Models/Models.cs` manually — regenerate from spec
- Do not add `HEALTHCHECK` to Dockerfile for nginx:alpine (wget fails silently)
