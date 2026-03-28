using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Subiekt.Connector.Api.Auth;
using Subiekt.Connector.Api.Configuration;
using Subiekt.Connector.Contracts;
using Subiekt.Connector.Api.Services;
using Xunit;

namespace Subiekt.Connector.IntegrationTests.Api;

public class SubiektApiClientTests
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    private SubiektApiClient CreateSut(HttpMessageHandler handler)
    {
        var opts = Options.Create(new SubiektOptions
        {
            ClientId = "test",
            ClientSecret = "secret",
            SubscriptionKey = "sub-key",
            RedirectUri = "https://localhost/callback"
        });

        var tokenStore = new InMemoryTokenStore();
        tokenStore.StoreTokens("test-access-token", "test-refresh-token", DateTime.UtcNow.AddHours(1));

        var pkceFactory = new Mock<IHttpClientFactory>();
        pkceFactory.Setup(f => f.CreateClient("auth")).Returns(new HttpClient());
        var pkce = new PkceService(opts, pkceFactory.Object);

        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.subiekt123.pl/1.1/") };
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<SubiektApiClient>.Instance;
        return new SubiektApiClient(http, opts, tokenStore, pkce, logger);
    }

    private static HttpMessageHandler MockHandler<T>(T responseBody, HttpStatusCode status = HttpStatusCode.OK)
    {
        var mock = new Mock<HttpMessageHandler>();
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(status)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(responseBody, JsonOpts),
                    System.Text.Encoding.UTF8,
                    "application/json")
            });
        return mock.Object;
    }

    // ── Clients ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetClientsAsync_ReturnsClientList()
    {
        // Arrange
        var id = Guid.NewGuid();
        var json = $"[{{\"id\":\"{id}\",\"name\":\"Test Firma\",\"kind\":\"Company\",\"tinKind\":\"Nip\",\"favourite\":false}}]";
        var mock = new Mock<HttpMessageHandler>();
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });

        var sut = CreateSut(mock.Object);

        // Act
        var result = await sut.GetClientsAsync(1, 25, null, null, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Test Firma");
        result[0].Kind.Should().Be(ClientKind.Company);
    }

    [Fact]
    public async Task GetClientsAsync_WithFilters_BuildsCorrectQuery()
    {
        HttpRequestMessage? capturedRequest = null;
        var mock = new Mock<HttpMessageHandler>();
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]", System.Text.Encoding.UTF8, "application/json")
            });

        var sut = CreateSut(mock.Object);
        await sut.GetClientsAsync(2, 10, new[] { "name~Test", "kind=Company" }, null, CancellationToken.None);

        capturedRequest!.RequestUri!.Query.Should().Contain("pageNumber=2");
        capturedRequest.RequestUri.Query.Should().Contain("pageSize=10");
        capturedRequest.RequestUri.Query.Should().Contain("filters=");
    }

    [Fact]
    public async Task GetClientAsync_ReturnsClient()
    {
        var id = Guid.NewGuid();
        var json = $"{{\"id\":\"{id}\",\"name\":\"Test\",\"kind\":\"Company\",\"tinKind\":\"Nip\",\"favourite\":false}}";
        var mock = new Mock<HttpMessageHandler>();
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });

        var sut = CreateSut(mock.Object);
        var result = await sut.GetClientAsync(id, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(id);
    }

    [Fact]
    public async Task GetDocumentsAsync_ReturnsDocumentList()
    {
        var id = Guid.NewGuid();
        var json = $"[{{\"id\":\"{id}\",\"documentNumber\":\"FV/2024/001\",\"kind\":\"Invoice\",\"issueDate\":\"2024-01-15T00:00:00\",\"dueDate\":\"2024-01-29T00:00:00\",\"totalNet\":1000,\"totalNetPln\":1000,\"totalGross\":1230,\"totalGrossPln\":1230,\"calculationMethod\":\"Net\"}}]";
        var mock = new Mock<HttpMessageHandler>();
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });

        var sut = CreateSut(mock.Object);
        var result = await sut.GetDocumentsAsync(1, 25, null, null, CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].DocumentNumber.Should().Be("FV/2024/001");
        result[0].Kind.Should().Be(DocumentKind.Invoice);
    }

    [Fact]
    public async Task GetProductsAsync_ReturnsProductList()
    {
        var id = Guid.NewGuid();
        var json = $"[{{\"id\":\"{id}\",\"name\":\"Produkt 1\",\"kind\":\"Good\",\"favourite\":false,\"subjectToSplitPayment\":false,\"grossPrice\":100,\"netPrice\":81}}]";
        var mock = new Mock<HttpMessageHandler>();
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
            });

        var sut = CreateSut(mock.Object);
        var result = await sut.GetProductsAsync(1, 25, null, null, CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Produkt 1");
    }

    [Fact]
    public async Task DeleteClientAsync_CallsDeleteEndpoint()
    {
        var id = Guid.NewGuid();
        HttpRequestMessage? captured = null;
        var mock = new Mock<HttpMessageHandler>();
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => captured = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NoContent));

        var sut = CreateSut(mock.Object);
        await sut.DeleteClientAsync(id, CancellationToken.None);

        captured!.Method.Should().Be(HttpMethod.Delete);
        captured.RequestUri!.PathAndQuery.Should().Contain(id.ToString());
    }

    [Fact]
    public async Task SetAuthAsync_SetsRequiredHeaders()
    {
        HttpRequestMessage? captured = null;
        var mock = new Mock<HttpMessageHandler>();
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => captured = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]", System.Text.Encoding.UTF8, "application/json")
            });

        var sut = CreateSut(mock.Object);
        await sut.GetClientsAsync(1, 25, null, null, CancellationToken.None);

        captured!.Headers.Authorization!.Scheme.Should().Be("Bearer");
        captured.Headers.Authorization.Parameter.Should().Be("test-access-token");
        captured.Headers.GetValues("Ocp-Apim-Subscription-Key").Should().Contain("sub-key");
        captured.Headers.GetValues("x-api-version").Should().Contain("1.1");
    }
}
