using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Subiekt.Connector.Sdk.Auth;
using Subiekt.Connector.Contracts;

namespace Subiekt.Connector.Sdk;

/// <summary>
/// Main entry point for Subiekt 123 API v1.1.
/// Handles authentication, token refresh, and all resource operations.
///
/// <example>
/// var sdk = new SubiektClient(options, httpClient);
/// var (url, state) = sdk.Auth.BuildAuthorizationUrl();
/// // redirect user to url, store state
/// // on callback:
/// var token = await sdk.Auth.ExchangeCodeAsync(code, state);
/// sdk.SetToken(token);
/// var clients = await sdk.Clients.ListAsync();
/// </example>
/// </summary>
public class SubiektClient
{
    private readonly HttpClient _http;
    private readonly SubiektClientOptions _opts;

    private TokenInfo? _token;

    public SubiektAuthClient Auth { get; }
    public ClientsResource Clients { get; }
    public DocumentsResource Documents { get; }
    public ProductsResource Products { get; }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public SubiektClient(SubiektClientOptions opts, HttpClient? http = null)
    {
        _opts = opts;
        _http = http ?? new HttpClient { BaseAddress = new Uri(opts.ApiBaseUrl) };

        var authHttp = new HttpClient();
        Auth = new SubiektAuthClient(authHttp, opts);
        Clients = new ClientsResource(this);
        Documents = new DocumentsResource(this);
        Products = new ProductsResource(this);
    }

    /// <summary>Sets the current token (call after ExchangeCodeAsync or RefreshAsync).</summary>
    public void SetToken(TokenInfo token) => _token = token;

    /// <summary>Returns current token info.</summary>
    public TokenInfo? GetToken() => _token;

    /// <summary>Returns true if token is set and not expired.</summary>
    public bool IsAuthorized => _token is not null && !_token.IsExpired;

    /// <summary>Optional log callback for diagnostics. Set to receive HTTP request/response details.</summary>
    public Action<string>? Log { get; set; }

    internal async Task<T> GetAsync<T>(string path, CancellationToken ct = default)
    {
        var (result, _) = await GetWithEtagAsync<T>(path, ct);
        return result;
    }

    internal async Task<(T Result, string? Etag)> GetWithEtagAsync<T>(string path, CancellationToken ct = default)
    {
        await EnsureTokenAsync(ct);
        Log?.Invoke($"GET {_http.BaseAddress}{path}");
        var resp = await _http.GetAsync(path, ct);
        var raw = await resp.Content.ReadAsStringAsync(ct);
        Log?.Invoke($"  → {(int)resp.StatusCode} {resp.ReasonPhrase} ({raw.Length} bytes)");
        if (!resp.IsSuccessStatusCode)
        {
            Log?.Invoke($"  Body: {raw[..Math.Min(500, raw.Length)]}");
            resp.EnsureSuccessStatusCode();
        }
        var etag = resp.Headers.ETag?.Tag?.Trim('"');
        Log?.Invoke($"  ETag: [{etag}]");
        return (JsonSerializer.Deserialize<T>(raw, JsonOpts)!, etag);
    }

    /// <summary>
    /// Smart list GET — handles both array [...] and paged { items: [...], totalItems: N } responses.
    /// </summary>
    internal async Task<PagedResult<T>> GetListAsync<T>(string path, CancellationToken ct = default)
    {
        await EnsureTokenAsync(ct);
        Log?.Invoke($"GET {_http.BaseAddress}{path}");
        LogHeaders();
        var resp = await _http.GetAsync(path, ct);
        var raw = await resp.Content.ReadAsStringAsync(ct);
        Log?.Invoke($"  → {(int)resp.StatusCode} {resp.ReasonPhrase} ({raw.Length} bytes)");
        Log?.Invoke($"  Body: {raw[..Math.Min(1000, raw.Length)]}");
        if (!resp.IsSuccessStatusCode)
            resp.EnsureSuccessStatusCode();

        if (raw.TrimStart().StartsWith('['))
        {
            var list = JsonSerializer.Deserialize<List<T>>(raw, JsonOpts) ?? [];
            Log?.Invoke($"  Deserialized array: {list.Count} items");
            return new PagedResult<T>(list, list.Count);
        }

        var paged = JsonSerializer.Deserialize<PagedResult<T>>(raw, JsonOpts)
            ?? new PagedResult<T>([], 0);
        Log?.Invoke($"  Deserialized paged: {paged.Items?.Count ?? 0}/{paged.TotalItemsCount}");
        return paged;
    }

    private void LogHeaders()
    {
        if (Log is null) return;
        foreach (var h in _http.DefaultRequestHeaders)
            Log($"  Header: {h.Key}: {string.Join(", ", h.Value)}");
    }

    internal async Task<T> PostAsync<T>(string path, object body, CancellationToken ct = default)
    {
        await EnsureTokenAsync(ct);
        Log?.Invoke($"POST {_http.BaseAddress}{path}");
        var jsonBody = JsonSerializer.Serialize(body, body.GetType(), JsonOpts);
        Log?.Invoke($"  Request: {jsonBody[..Math.Min(1000, jsonBody.Length)]}");
        var content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json");
        var resp = await _http.PostAsync(path, content, ct);
        var raw = await resp.Content.ReadAsStringAsync(ct);
        Log?.Invoke($"  → {(int)resp.StatusCode} {resp.ReasonPhrase} ({raw.Length} bytes)");
        if (!resp.IsSuccessStatusCode)
        {
            Log?.Invoke($"  Body: {raw[..Math.Min(500, raw.Length)]}");
            throw new HttpRequestException($"POST {path} → {(int)resp.StatusCode}: {raw[..Math.Min(500, raw.Length)]}");
        }
        return JsonSerializer.Deserialize<T>(raw, JsonOpts)!;
    }

    internal async Task<T> PutAsync<T>(string path, object body, string etag, CancellationToken ct = default)
    {
        await EnsureTokenAsync(ct);
        Log?.Invoke($"PUT {_http.BaseAddress}{path} (ETag: {etag})");
        var jsonBody = JsonSerializer.Serialize(body, body.GetType(), JsonOpts);
        Log?.Invoke($"  Request: {jsonBody[..Math.Min(1000, jsonBody.Length)]}");
        var req = new HttpRequestMessage(HttpMethod.Put, path)
        {
            Content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json")
        };
        req.Headers.TryAddWithoutValidation("If-Match", etag);
        var resp = await _http.SendAsync(req, ct);
        var raw = await resp.Content.ReadAsStringAsync(ct);
        Log?.Invoke($"  → {(int)resp.StatusCode} {resp.ReasonPhrase} ({raw.Length} bytes)");
        if (!resp.IsSuccessStatusCode)
        {
            Log?.Invoke($"  Body: {raw[..Math.Min(1000, raw.Length)]}");
            throw new HttpRequestException($"PUT {path} → {(int)resp.StatusCode}: {raw[..Math.Min(500, raw.Length)]}");
        }
        return JsonSerializer.Deserialize<T>(raw, JsonOpts)!;
    }

    internal async Task DeleteAsync(string path, CancellationToken ct = default)
    {
        await EnsureTokenAsync(ct);
        (await _http.DeleteAsync(path, ct)).EnsureSuccessStatusCode();
    }

    internal async Task PatchAsync(string path, CancellationToken ct = default)
    {
        await EnsureTokenAsync(ct);
        var resp = await _http.PatchAsync(path, null, ct);
        resp.EnsureSuccessStatusCode();
    }

    internal async Task<byte[]> PostBytesAsync(string path, object body, CancellationToken ct = default)
    {
        await EnsureTokenAsync(ct);
        var resp = await _http.PostAsJsonAsync(path, body, JsonOpts, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsByteArrayAsync(ct);
    }

    private async Task EnsureTokenAsync(CancellationToken ct)
    {
        if (_token is null)
            throw new InvalidOperationException("Not authorized. Call SetToken() after completing OAuth flow.");

        if (_token.IsExpired && _token.RefreshToken is not null)
        {
            _token = await Auth.RefreshAsync(_token.RefreshToken, ct);
        }

        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token.AccessToken);
        _http.DefaultRequestHeaders.Remove("Ocp-Apim-Subscription-Key");
        _http.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _opts.SubscriptionKey);
        _http.DefaultRequestHeaders.Remove("x-api-version");
        _http.DefaultRequestHeaders.Add("x-api-version", "1.1");
    }

    internal static string BuildListQuery(int pageNumber, int pageSize, string[]? filters, string[]? orderBy)
    {
        var parts = new List<string> { $"pageNumber={pageNumber}", $"pageSize={pageSize}" };
        if (filters != null)
            foreach (var f in filters) parts.Add($"filters={Uri.EscapeDataString(f)}");
        if (orderBy != null)
            foreach (var o in orderBy) parts.Add($"orderBy={Uri.EscapeDataString(o)}");
        return "?" + string.Join("&", parts);
    }
}
