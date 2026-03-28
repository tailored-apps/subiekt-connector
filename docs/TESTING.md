# 🧪 Testing Guide

## Running Tests

### All tests (quickest)

```bash
dotnet test
```

### With full output

```bash
dotnet test --verbosity normal
```

### Only integration tests

```bash
dotnet test tests/Subiekt.Connector.IntegrationTests
```

### With test results file (for CI)

```bash
dotnet test --logger "trx;LogFileName=results.trx"
```

### With code coverage

```bash
dotnet test --collect:"XPlat Code Coverage"
# Report in: TestResults/<guid>/coverage.cobertura.xml
```

---

## Expected Output

```
Przebieg testu dla: Subiekt.Connector.IntegrationTests.dll (.NETCoreApp,Version=v10.0)

  ✅ PkceServiceTests.GeneratePkce_ReturnsValidCodeVerifierAndChallenge
  ✅ PkceServiceTests.GeneratePkce_EachCallProducesUniquePair
  ✅ PkceServiceTests.GenerateState_ReturnsUniqueValues
  ✅ PkceServiceTests.BuildAuthorizationUrl_ContainsRequiredParameters
  ✅ InMemoryTokenStoreTests.InitialState_HasNoToken
  ✅ InMemoryTokenStoreTests.StoreTokens_ThenGet_ReturnsStoredValues
  ✅ InMemoryTokenStoreTests.IsExpired_WhenTokenExpiresSoon_ReturnsTrue
  ✅ InMemoryTokenStoreTests.Clear_RemovesAllTokens
  ✅ InMemoryTokenStoreTests.StoreTokens_NullRefreshToken_PreservesExisting
  ✅ OAuthStateCacheTests.Set_ThenGet_ReturnsValue
  ✅ OAuthStateCacheTests.Get_UnknownState_ReturnsNull
  ✅ OAuthStateCacheTests.Remove_ThenGet_ReturnsNull
  ✅ ContractsTests.ClientDto_DeserializesFromCamelCaseJson
  ✅ ContractsTests.DocumentListDto_DeserializesCorrectly

Powodzenie! — niepowodzenie: 0, powodzenie: 14, pominięto: 0, łącznie: 14
```

---

## Test Structure

```
tests/Subiekt.Connector.IntegrationTests/
├── Auth/
│   ├── PkceServiceTests.cs           # PKCE generation, URL building
│   ├── InMemoryTokenStoreTests.cs    # Token storage, expiry
│   └── OAuthStateCacheTests.cs       # Pending OAuth state cache
├── Api/
│   └── ContractsTests.cs             # DTO JSON deserialization
└── usings.cs                         # Global usings (xUnit, FluentAssertions)
```

---

## Writing New Tests

Stack: **xUnit** + **FluentAssertions** + **Moq**

```csharp
public class MyServiceTests
{
    [Fact]
    public async Task MyMethod_WhenCondition_ReturnsExpected()
    {
        // Arrange
        var mock = new Mock<IDependency>();
        mock.Setup(x => x.GetAsync()).ReturnsAsync("value");
        var sut = new MyService(mock.Object);

        // Act
        var result = await sut.MyMethod();

        // Assert
        result.Should().Be("value");
    }
}
```

### FluentAssertions cheatsheet

```csharp
result.Should().Be("expected");
result.Should().NotBeNull();
result.Should().BeGreaterThan(0);
list.Should().HaveCount(3);
list.Should().Contain(x => x.Name == "test");
action.Should().Throw<InvalidOperationException>();
await asyncAction.Should().ThrowAsync<HttpRequestException>();
```

---

## Adding Integration Tests Against Live API

For tests against the real Subiekt 123 API, use environment variables (never commit credentials):

```csharp
public class LiveApiTests
{
    private readonly SubiektClient _client;

    public LiveApiTests()
    {
        var clientId = Environment.GetEnvironmentVariable("SUBIEKT_CLIENT_ID")
            ?? throw new SkipException("Live API credentials not set");

        _client = new SubiektClient(new SubiektClientOptions
        {
            ClientId = clientId,
            ClientSecret = Environment.GetEnvironmentVariable("SUBIEKT_CLIENT_SECRET")!,
            SubscriptionKey = Environment.GetEnvironmentVariable("SUBIEKT_SUBSCRIPTION_KEY")!,
            RedirectUri = "http://localhost/callback"
        });

        var token = Environment.GetEnvironmentVariable("SUBIEKT_ACCESS_TOKEN");
        if (token != null)
            _client.SetToken(new TokenInfo(token, null, DateTime.UtcNow.AddHours(1)));
    }

    [Fact]
    public async Task GetClients_ReturnsResults()
    {
        var clients = await _client.Clients.ListAsync(pageSize: 5);
        clients.Should().NotBeEmpty();
    }
}
```

Run with env vars:

```bash
SUBIEKT_CLIENT_ID=xxx \
SUBIEKT_CLIENT_SECRET=yyy \
SUBIEKT_SUBSCRIPTION_KEY=zzz \
SUBIEKT_ACCESS_TOKEN=eyJ... \
dotnet test --filter "FullyQualifiedName~LiveApiTests"
```
