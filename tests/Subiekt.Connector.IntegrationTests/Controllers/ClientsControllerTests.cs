using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Subiekt.Connector.Api;
using Subiekt.Connector.Api.Auth;
using Subiekt.Connector.Contracts;
using Subiekt.Connector.Api.Services;
using Xunit;

namespace Subiekt.Connector.IntegrationTests.Controllers;

public class ClientsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ClientsControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private WebApplicationFactory<Program> CreateFactory(Mock<ISubiektApiClient> mockApi)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace real client with mock
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ISubiektApiClient));
                if (descriptor != null) services.Remove(descriptor);
                services.AddSingleton(mockApi.Object);

                // Replace token store with one that has a valid token
                var tokenDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ITokenStore));
                if (tokenDescriptor != null) services.Remove(tokenDescriptor);
                var tokenStore = new InMemoryTokenStore();
                tokenStore.StoreTokens("test-token", null, DateTime.UtcNow.AddHours(1));
                services.AddSingleton<ITokenStore>(tokenStore);
            });
        });
    }

    [Fact]
    public async Task GetAll_Returns200WithClients()
    {
        var id = Guid.NewGuid();
        var mockApi = new Mock<ISubiektApiClient>();
        mockApi.Setup(a => a.GetClientsAsync(It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string[]?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ClientDto>
            {
                new ClientDto { Id = id, Kind = ClientKind.Company, Name = "Test Sp. z o.o." }
            });

        var client = CreateFactory(mockApi).CreateClient();
        var resp = await client.GetAsync("/clients?pageNumber=1&pageSize=10");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        body.Should().Contain("Test Sp. z o.o.");
    }

    [Fact]
    public async Task GetById_Returns200()
    {
        var id = Guid.NewGuid();
        var mockApi = new Mock<ISubiektApiClient>();
        mockApi.Setup(a => a.GetClientAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClientDto { Id = id, Kind = ClientKind.Company, Name = "Firma ABC" });

        var client = CreateFactory(mockApi).CreateClient();
        var resp = await client.GetAsync($"/clients/{id}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Delete_Returns204()
    {
        var id = Guid.NewGuid();
        var mockApi = new Mock<ISubiektApiClient>();
        mockApi.Setup(a => a.DeleteClientAsync(id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var client = CreateFactory(mockApi).CreateClient();
        var resp = await client.DeleteAsync($"/clients/{id}");

        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
