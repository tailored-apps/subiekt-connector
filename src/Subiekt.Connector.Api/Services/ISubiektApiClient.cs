using Subiekt.Connector.Contracts;

namespace Subiekt.Connector.Api.Services;

public interface ISubiektApiClient
{
    // Clients
    Task<List<ClientDto>> GetClientsAsync(int pageNumber = 1, int pageSize = 25, string[]? filters = null, string? orderBy = null, CancellationToken ct = default);
    Task<ClientDto> GetClientAsync(Guid id, CancellationToken ct = default);
    Task<CreateClientResultDto> CreateClientAsync(CreateClientDto dto, CancellationToken ct = default);
    Task<UpdateClientResultDto> UpdateClientAsync(Guid id, UpdateClientDto dto, string etag, CancellationToken ct = default);
    Task DeleteClientAsync(Guid id, CancellationToken ct = default);

    // Documents
    Task<List<DocumentListDto>> GetDocumentsAsync(int pageNumber = 1, int pageSize = 25, string[]? filters = null, string? orderBy = null, CancellationToken ct = default);
    Task<DocumentDto> GetDocumentAsync(Guid id, CancellationToken ct = default);
    Task<CreateDocumentResultDto> CreateDocumentAsync(CreateDocumentDto dto, CancellationToken ct = default);
    Task<UpdateDocumentResultDto> UpdateDocumentAsync(Guid id, UpdateDocumentDto dto, string etag, CancellationToken ct = default);
    Task DeleteDocumentAsync(Guid id, CancellationToken ct = default);
    Task<byte[]> PrintDocumentAsync(Guid id, DocumentPrintingSettingsDto settings, CancellationToken ct = default);

    // Products
    Task<List<ProductListItemDto>> GetProductsAsync(int pageNumber = 1, int pageSize = 25, string[]? filters = null, string? orderBy = null, CancellationToken ct = default);
    Task<ProductDto> GetProductAsync(Guid id, CancellationToken ct = default);
    Task<CreateProductResultDto> CreateProductAsync(CreateProductDto dto, CancellationToken ct = default);
    Task<UpdateProductResultDto> UpdateProductAsync(Guid id, UpdateProductDto dto, string etag, CancellationToken ct = default);
    Task DeleteProductAsync(Guid id, CancellationToken ct = default);
}
