using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Subiekt.Connector.Api;
using Subiekt.Connector.Api.Auth;
using Subiekt.Connector.Api.Auth.Models;
using Xunit;

namespace Subiekt.Connector.IntegrationTests.Controllers;

public class AuthControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private WebApplicationFactory<Program> CreateFactory(
        Mock<IPkceService>? mockPkce = null,
        ITokenStore? tokenStore = null,
        IOAuthStateCache? stateCache = null)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                if (mockPkce != null)
                {
                    var d = services.SingleOrDefault(s => s.ServiceType == typeof(IPkceService));
                    if (d != null) services.Remove(d);
                    services.AddScoped(_ => mockPkce.Object);
                }
                if (tokenStore != null)
                {
                    var d = services.SingleOrDefault(s => s.ServiceType == typeof(ITokenStore));
                    if (d != null) services.Remove(d);
                    services.AddSingleton(tokenStore);
                }
                if (stateCache != null)
                {
                    var d = services.SingleOrDefault(s => s.ServiceType == typeof(IOAuthStateCache));
                    if (d != null) services.Remove(d);
                    services.AddSingleton(stateCache);
                }
            });
        });
    }

    [Fact]
    public async Task Status_WhenNoToken_ReturnsNotAuthorized()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/auth/status");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        body.Should().Contain("\"authorized\":false");
    }

    [Fact]
    public async Task Status_WhenTokenStored_ReturnsAuthorized()
    {
        var store = new InMemoryTokenStore();
        store.StoreTokens("access", "refresh", DateTime.UtcNow.AddHours(1));

        var client = CreateFactory(tokenStore: store).CreateClient();
        var resp = await client.GetAsync("/auth/status");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        body.Should().Contain("\"authorized\":true");
    }

    [Fact]
    public async Task Logout_ClearsToken_Returns200()
    {
        var store = new InMemoryTokenStore();
        store.StoreTokens("access", "refresh", DateTime.UtcNow.AddHours(1));

        var client = CreateFactory(tokenStore: store).CreateClient();
        var resp = await client.PostAsync("/auth/logout", null);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        store.GetAccessToken().Should().BeNull();
    }

    [Fact]
    public async Task Login_RedirectsToInsert()
    {
        var mockPkce = new Mock<IPkceService>();
        mockPkce.Setup(p => p.GeneratePkce())
            .Returns(("verifier", "challenge"));
        mockPkce.Setup(p => p.GenerateState())
            .Returns("test-state");
        mockPkce.Setup(p => p.BuildAuthorizationUrl("challenge", "test-state"))
            .Returns("https://kontoapi.insert.com.pl/connect/authorize?test=1");

        var client = CreateFactory(mockPkce: mockPkce)
            .CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var resp = await client.GetAsync("/auth/login");

        resp.StatusCode.Should().Be(HttpStatusCode.Redirect);
        resp.Headers.Location!.ToString().Should().Contain("kontoapi.insert.com.pl");
    }

    [Fact]
    public async Task Callback_InvalidState_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/auth/callback?code=abc123&state=nonexistent");

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Callback_ValidState_ExchangesCodeAndReturns200()
    {
        var cache = new InMemoryOAuthStateCache();
        var state = new OAuthState("valid-state", "verifier123", DateTime.UtcNow);
        cache.Set("valid-state", state);

        var store = new InMemoryTokenStore();

        var mockPkce = new Mock<IPkceService>();
        mockPkce.Setup(p => p.GeneratePkce()).Returns(("v", "c"));
        mockPkce.Setup(p => p.GenerateState()).Returns("s");
        mockPkce.Setup(p => p.BuildAuthorizationUrl(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("https://example.com");
        mockPkce.Setup(p => p.ExchangeCodeAsync("test-code", "verifier123"))
            .ReturnsAsync(new Subiekt.Connector.Api.Auth.Models.TokenResponse(
                "access-token", "refresh-token", 3600, "Bearer", null, null));

        var client = CreateFactory(mockPkce, store, cache)
            .CreateClient();

        var resp = await client.GetAsync("/auth/callback?code=test-code&state=valid-state");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        store.GetAccessToken().Should().Be("access-token");
    }

    [Fact]
    public async Task Refresh_NoRefreshToken_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsync("/auth/refresh", null);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
