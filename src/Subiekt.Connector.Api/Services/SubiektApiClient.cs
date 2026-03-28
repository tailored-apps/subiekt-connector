using Microsoft.Extensions.Options;
using Subiekt.Connector.Api.Auth;
using Subiekt.Connector.Api.Configuration;
using Subiekt.Connector.Contracts;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Subiekt.Connector.Api.Services;

public class SubiektApiClient : ISubiektApiClient
{
    private readonly HttpClient _http;
    private readonly SubiektOptions _options;
    private readonly ITokenStore _tokens;
    private readonly IPkceService _pkce;
    private readonly ILogger<SubiektApiClient> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public SubiektApiClient(
        HttpClient http,
        IOptions<SubiektOptions> options,
        ITokenStore tokens,
        IPkceService pkce,
        ILogger<SubiektApiClient> logger)
    {
        _http = http;
        _options = options.Value;
        _tokens = tokens;
        _pkce = pkce;
        _logger = logger;
    }

    private async Task SetAuthAsync(CancellationToken ct)
    {
        if (_tokens.IsExpired() && _tokens.GetRefreshToken() is not null)
        {
            _logger.LogInformation("Token expired, refreshing...");
            var refreshed = await _pkce.RefreshTokenAsync(_tokens.GetRefreshToken()!);
            _tokens.StoreTokens(refreshed.AccessToken, refreshed.RefreshToken, DateTime.UtcNow.AddSeconds(refreshed.ExpiresIn));
        }

        var token = _tokens.GetAccessToken()
            ?? throw new InvalidOperationException("Not authorized. Complete OAuth flow at /auth/login first.");

        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _http.DefaultRequestHeaders.Remove("Ocp-Apim-Subscription-Key");
        _http.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _options.SubscriptionKey);
        _http.DefaultRequestHeaders.Remove("x-api-version");
        _http.DefaultRequestHeaders.Add("x-api-version", "1.1");
    }

    private string BuildListQuery(int pageNumber, int pageSize, string[]? filters, string? orderBy)
    {
        var parts = new List<string> { $"pageNumber={pageNumber}", $"pageSize={pageSize}" };
        if (filters != null)
            foreach (var f in filters) parts.Add($"filters={Uri.EscapeDataString(f)}");
        if (!string.IsNullOrWhiteSpace(orderBy))
            parts.Add($"orderBy={Uri.EscapeDataString(orderBy)}");
        return "?" + string.Join("&", parts);
    }

    /// <summary>
    /// Smart GET — handles both array responses and paged { items: [], totalItems: N } responses.
    /// Logs raw JSON in debug mode.
    /// </summary>
    private async Task<List<T>> GetListAsync<T>(string url, CancellationToken ct)
    {
        var resp = await _http.GetAsync(url, ct);

        var raw = await resp.Content.ReadAsStringAsync(ct);
        _logger.LogDebug("GET {Url} → {Status}\n{Body}", url, (int)resp.StatusCode, raw);

        resp.EnsureSuccessStatusCode();

        // Jeśli odpowiedź zaczyna się od '[' — to tablica
        if (raw.TrimStart().StartsWith('['))
        {
            return JsonSerializer.Deserialize<List<T>>(raw, JsonOpts) ?? [];
        }

        // Jeśli obiekt — spróbuj paginację { items: [...] }
        var paged = JsonSerializer.Deserialize<PagedResult<T>>(raw, JsonOpts);
        if (paged?.Items is not null)
            return paged.Items;

        // Fallback — może być pojedynczy obiekt w tablicy
        _logger.LogWarning("Unexpected response structure for {Url}: {Body}", url, raw[..Math.Min(200, raw.Length)]);
        return [];
    }

    // ── Clients ──────────────────────────────────────────────────────────────

    public async Task<List<ClientDto>> GetClientsAsync(int pageNumber, int pageSize, string[]? filters, string? orderBy, CancellationToken ct)
    {
        await SetAuthAsync(ct);
        return await GetListAsync<ClientDto>($"clients{BuildListQuery(pageNumber, pageSize, filters, orderBy)}", ct);
    }

    public async Task<ClientDto> GetClientAsync(Guid id, CancellationToken ct)
    {
        await SetAuthAsync(ct);
        var resp = await _http.GetAsync($"clients/{id}", ct);
        var raw = await resp.Content.ReadAsStringAsync(ct);
        _logger.LogDebug("GET clients/{Id} → {Status}\n{Body}", id, (int)resp.StatusCode, raw);
        resp.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<ClientDto>(raw, JsonOpts)!;
    }

    public async Task<CreateClientResultDto> CreateClientAsync(CreateClientDto dto, CancellationToken ct)
    {
        await SetAuthAsync(ct);
        var resp = await _http.PostAsJsonAsync("clients", dto, JsonOpts, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<CreateClientResultDto>(JsonOpts, ct))!;
    }

    public async Task<UpdateClientResultDto> UpdateClientAsync(Guid id, UpdateClientDto dto, string etag, CancellationToken ct)
    {
        await SetAuthAsync(ct);
        var req = new HttpRequestMessage(HttpMethod.Put, $"clients/{id}") { Content = JsonContent.Create(dto, options: JsonOpts) };
        req.Headers.IfMatch.Add(new EntityTagHeaderValue($"\"{etag}\""));
        var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<UpdateClientResultDto>(JsonOpts, ct))!;
    }

    public async Task DeleteClientAsync(Guid id, CancellationToken ct)
    {
        await SetAuthAsync(ct);
        (await _http.DeleteAsync($"clients/{id}", ct)).EnsureSuccessStatusCode();
    }

    // ── Documents ─────────────────────────────────────────────────────────────

    public async Task<List<DocumentListDto>> GetDocumentsAsync(int pageNumber, int pageSize, string[]? filters, string? orderBy, CancellationToken ct)
    {
        await SetAuthAsync(ct);
        return await GetListAsync<DocumentListDto>($"documents{BuildListQuery(pageNumber, pageSize, filters, orderBy)}", ct);
    }

    public async Task<DocumentDto> GetDocumentAsync(Guid id, CancellationToken ct)
    {
        await SetAuthAsync(ct);
        var resp = await _http.GetAsync($"documents/{id}", ct);
        var raw = await resp.Content.ReadAsStringAsync(ct);
        _logger.LogDebug("GET documents/{Id} → {Status}\n{Body}", id, (int)resp.StatusCode, raw[..Math.Min(500, raw.Length)]);
        resp.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<DocumentDto>(raw, JsonOpts)!;
    }

    public async Task<CreateDocumentResultDto> CreateDocumentAsync(CreateDocumentDto dto, CancellationToken ct)
    {
        await SetAuthAsync(ct);
        var resp = await _http.PostAsJsonAsync("documents", dto, JsonOpts, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<CreateDocumentResultDto>(JsonOpts, ct))!;
    }

    public async Task<UpdateDocumentResultDto> UpdateDocumentAsync(Guid id, UpdateDocumentDto dto, string etag, CancellationToken ct)
    {
        await SetAuthAsync(ct);
        var req = new HttpRequestMessage(HttpMethod.Put, $"documents/{id}") { Content = JsonContent.Create(dto, options: JsonOpts) };
        req.Headers.IfMatch.Add(new EntityTagHeaderValue($"\"{etag}\""));
        var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<UpdateDocumentResultDto>(JsonOpts, ct))!;
    }

    public async Task DeleteDocumentAsync(Guid id, CancellationToken ct)
    {
        await SetAuthAsync(ct);
        (await _http.DeleteAsync($"documents/{id}", ct)).EnsureSuccessStatusCode();
    }

    public async Task<byte[]> PrintDocumentAsync(Guid id, DocumentPrintingSettingsDto settings, CancellationToken ct)
    {
        await SetAuthAsync(ct);
        var resp = await _http.PostAsJsonAsync($"documents/{id}/printing", settings, JsonOpts, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsByteArrayAsync(ct);
    }

    // ── Products ──────────────────────────────────────────────────────────────

    public async Task<List<ProductListItemDto>> GetProductsAsync(int pageNumber, int pageSize, string[]? filters, string? orderBy, CancellationToken ct)
    {
        await SetAuthAsync(ct);
        return await GetListAsync<ProductListItemDto>($"products{BuildListQuery(pageNumber, pageSize, filters, orderBy)}", ct);
    }

    public async Task<ProductDto> GetProductAsync(Guid id, CancellationToken ct)
    {
        await SetAuthAsync(ct);
        var resp = await _http.GetAsync($"products/{id}", ct);
        var raw = await resp.Content.ReadAsStringAsync(ct);
        _logger.LogDebug("GET products/{Id} → {Status}\n{Body}", id, (int)resp.StatusCode, raw[..Math.Min(500, raw.Length)]);
        resp.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<ProductDto>(raw, JsonOpts)!;
    }

    public async Task<CreateProductResultDto> CreateProductAsync(CreateProductDto dto, CancellationToken ct)
    {
        await SetAuthAsync(ct);
        var resp = await _http.PostAsJsonAsync("products", dto, JsonOpts, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<CreateProductResultDto>(JsonOpts, ct))!;
    }

    public async Task<UpdateProductResultDto> UpdateProductAsync(Guid id, UpdateProductDto dto, string etag, CancellationToken ct)
    {
        await SetAuthAsync(ct);
        var req = new HttpRequestMessage(HttpMethod.Put, $"products/{id}") { Content = JsonContent.Create(dto, options: JsonOpts) };
        req.Headers.IfMatch.Add(new EntityTagHeaderValue($"\"{etag}\""));
        var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<UpdateProductResultDto>(JsonOpts, ct))!;
    }

    public async Task DeleteProductAsync(Guid id, CancellationToken ct)
    {
        await SetAuthAsync(ct);
        (await _http.DeleteAsync($"products/{id}", ct)).EnsureSuccessStatusCode();
    }
}
