# 🐧 Subiekt 123 Connector

.NET 10 connector for **Subiekt 123 API v1.1** (InsERT).  
Includes: standalone SDK, WebAPI connector, Blazor demo app, integration tests, and Git hooks.

---

## Quick Start

```bash
git clone https://github.com/Daemon-Penguins/subiekt-connector
cd subiekt-connector
./setup-hooks.sh   # install git hooks (one-time)
dotnet restore
dotnet build
```

---

## Projects

| Project | Type | Description |
|---------|------|-------------|
| `src/Subiekt.Connector.Api` | WebAPI | REST connector — exposes Subiekt 123 API via local HTTP endpoints |
| `src/Subiekt.Connector.Sdk` | Class Library | Standalone SDK — use directly in any .NET project |
| `demo/Subiekt.Demo` | Blazor Server | Demo app with OAuth login and client/document/product listing |
| `tests/Subiekt.Connector.IntegrationTests` | xUnit | Integration tests — auth, token store, PKCE, contract deserialization |

---

## Running Tests

```bash
# All tests
dotnet test

# With verbose output
dotnet test --verbosity normal

# Specific project
dotnet test tests/Subiekt.Connector.IntegrationTests

# With coverage (requires coverlet)
dotnet test --collect:"XPlat Code Coverage"
```

Expected result: **14/14 tests passed**

```
Powodzenie! — niepowodzenie: 0, powodzenie: 14, pominięto: 0, łącznie: 14
```

### Test categories

| Suite | Tests | What it covers |
|-------|-------|----------------|
| `PkceServiceTests` | 4 | PKCE code_verifier/challenge generation, state uniqueness, auth URL builder |
| `InMemoryTokenStoreTests` | 5 | Token storage, expiry check, refresh token preservation, clear |
| `OAuthStateCacheTests` | 3 | Pending OAuth state set/get/remove |
| `ContractsTests` | 2 | JSON deserialization of `ClientDto` and `DocumentListDto` from real API responses |

---

## Configuration

### WebAPI / Demo app (`appsettings.json`)

```json
{
  "Subiekt": {
    "ClientId": "twój-client-id",
    "ClientSecret": "twój-client-secret",
    "SubscriptionKey": "klucz-subskrypcji-z-portalu",
    "RedirectUri": "https://localhost:5001/hook/callback"
  }
}
```

### SDK (in code)

```csharp
var sdk = new SubiektClient(new SubiektClientOptions
{
    ClientId     = "twój-client-id",
    ClientSecret = "twój-client-secret",
    SubscriptionKey = "klucz-subskrypcji",
    RedirectUri  = "https://twoja-aplikacja.pl/callback"
});
```

### How to get credentials

1. Wejdź na [developers.insert.com.pl](https://developers.insert.com.pl)
2. Zarejestruj aplikację → otrzymasz `ClientId` i `ClientSecret`
3. Ustaw `redirect_uri` na adres Twojej aplikacji (np. `/hook/callback`)
4. Subskrybuj API Subiekt 123 → otrzymasz `SubscriptionKey`

---

## Running the WebAPI

```bash
cd src/Subiekt.Connector.Api
dotnet run
```

Swagger UI: `https://localhost:5001/swagger`

### OAuth flow

1. `GET /auth/login` → redirects to InsERT login page
2. User logs in and authorizes
3. InsERT redirects to `/hook/callback?code=...&state=...`
4. Token stored automatically
5. `GET /auth/status` → check if authorized
6. `POST /auth/refresh` → manual token refresh
7. `POST /auth/logout` → clear token

---

## Running the Blazor Demo

```bash
cd demo/Subiekt.Demo
dotnet run
```

Open `https://localhost:5002` → go to **Auth** → click **Zaloguj się przez InsERT**

---

## Using the SDK

Install as a project reference:

```xml
<ProjectReference Include="../src/Subiekt.Connector.Sdk/Subiekt.Connector.Sdk.csproj" />
```

### OAuth 2.0 PKCE flow

```csharp
var sdk = new SubiektClient(options);

// Step 1: generate auth URL
var (url, state) = sdk.Auth.BuildAuthorizationUrl();
// Store state (e.g. in session) — needed for step 2
// Redirect user to url

// Step 2: on callback (code + state from query string)
var token = await sdk.Auth.ExchangeCodeAsync(code, state);
sdk.SetToken(token);
```

### Clients

```csharp
// List (paged, with filters)
var clients = await sdk.Clients.ListAsync(
    pageNumber: 1,
    pageSize: 25,
    filters: new[] { "name~Kowalski" }   // name contains "Kowalski"
);

// Get by ID
var client = await sdk.Clients.GetAsync(clientId);

// Create
var result = await sdk.Clients.CreateAsync(new CreateClientDto(
    Name: "Testowa Firma Sp. z o.o.",
    Kind: ClientKind.Company,
    Tin: "1234567890",
    TinKind: TinKind.Nip
));

// Update (requires ETag from previous GET response header)
await sdk.Clients.UpdateAsync(id, updateDto, etag);

// Delete
await sdk.Clients.DeleteAsync(id);
```

### Documents

```csharp
// List with date filter
var docs = await sdk.Documents.ListAsync(
    filters: new[] { "issueDate_From=2024-01-01", "issueDate_To=2024-12-31" }
);

// Get full document
var doc = await sdk.Documents.GetAsync(documentId);

// Print (returns PDF bytes)
byte[] pdf = await sdk.Documents.PrintAsync(documentId, ecoMode: false);
File.WriteAllBytes("faktura.pdf", pdf);
```

### Products

```csharp
// List goods only
var products = await sdk.Products.ListAsync(
    filters: new[] { "kind=Good" },
    orderBy: "name:asc"
);

var product = await sdk.Products.GetAsync(productId);
```

### Supported filter operators

| Operator | Meaning | Example |
|----------|---------|---------|
| `=` | Exact match | `kind=Company` |
| `~` | Contains | `name~Kowalski` |

### Clients filter fields: `name`, `kind`, `group`, `nip`
### Documents filter fields: `documentNumber`, `client`, `issueDate_From`, `issueDate_To`
### Products filter fields: `name`, `kind`, `group`

---

## Git Hooks

After cloning, install hooks once:

```bash
./setup-hooks.sh
```

| Hook | Trigger | Action |
|------|---------|--------|
| `pre-commit` | `git commit` | `dotnet format --verify-no-changes` — blocks commit if code is not formatted |
| `pre-push` | `git push` | `dotnet build` + `dotnet test` — blocks push on failure |

Format code before committing:

```bash
dotnet format
git add .
git commit -m "..."
```

Skip hooks if needed:

```bash
git commit --no-verify
git push --no-verify
```

---

## CI/CD

GitHub Actions runs on every push to `main`:

- `dotnet restore`
- `dotnet build --configuration Release`
- `dotnet test --configuration Release`
- Uploads test results as `.trx` artifacts

See [Actions](https://github.com/Daemon-Penguins/subiekt-connector/actions).

---

## API Reference

Subiekt 123 API v1.1 — [developers.insert.com.pl](https://developers.insert.com.pl)

Base URL: `https://api.subiekt123.pl/1.1`  
Auth: OAuth 2.0 PKCE via `https://kontoapi.insert.com.pl`

> **Note:** All IDs in v1.1 are `Guid` (not `int` as in v1.0).  
> Contracts are auto-generated from the official OpenAPI spec.

---

## Project Structure

```
subiekt-connector/
├── src/
│   ├── Subiekt.Connector.Api/          # WebAPI connector
│   │   ├── Auth/                       # OAuth services (PKCE, TokenStore, StateCache)
│   │   ├── Contracts/                  # Auto-generated DTOs from OpenAPI v1.1
│   │   ├── Controllers/                # Auth, Hook, Clients, Documents, Products
│   │   └── Services/                   # SubiektApiClient (HttpClient wrapper)
│   └── Subiekt.Connector.Sdk/          # Standalone SDK
│       ├── Auth/                       # PkceHelper, SubiektAuthClient, TokenInfo
│       ├── Models/                     # Auto-generated models from OpenAPI v1.1
│       └── Resources/                  # ClientsResource, DocumentsResource, ProductsResource
├── demo/
│   └── Subiekt.Demo/                   # Blazor Server demo
├── tests/
│   └── Subiekt.Connector.IntegrationTests/  # xUnit tests
├── .githooks/                          # Git hooks (pre-commit, pre-push)
├── setup-hooks.sh                      # One-time hook installer
└── Subiekt.Connector.sln
```
