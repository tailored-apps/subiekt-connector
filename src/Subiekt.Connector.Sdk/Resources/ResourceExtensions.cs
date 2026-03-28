using Subiekt.Connector.Contracts;

namespace Subiekt.Connector.Sdk;

public partial class ClientsResource
{
    public Task<(ClientDto Result, string? Etag)> GetWithEtagAsync(Guid id, CancellationToken ct = default)
        => _client.GetWithEtagAsync<ClientDto>($"clients/{id}", ct);
}

public partial class ProductsResource
{
    public Task<(ProductDto Result, string? Etag)> GetWithEtagAsync(Guid id, CancellationToken ct = default)
        => _client.GetWithEtagAsync<ProductDto>($"products/{id}", ct);
}
