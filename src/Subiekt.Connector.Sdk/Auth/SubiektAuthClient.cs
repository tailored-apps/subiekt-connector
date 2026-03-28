using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Subiekt.Connector.Sdk.Auth;

/// <summary>
/// Handles OAuth 2.0 PKCE flow for InsERT / Subiekt 123 API.
/// </summary>
public class SubiektAuthClient
{
    private readonly HttpClient _http;
    private readonly SubiektClientOptions _opts;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public SubiektAuthClient(HttpClient http, SubiektClientOptions opts)
    {
        _http = http;
        _opts = opts;
    }

    /// <summary>
    /// Creates a PKCE state and returns the authorization URL to redirect the user to.
    /// Store the returned <see cref="PkceState"/> (e.g. in session) for use in <see cref="ExchangeCodeAsync"/>.
    /// </summary>
    public (string AuthorizationUrl, PkceState State) BuildAuthorizationUrl()
    {
        var state = PkceHelper.Create();
        var qs = BuildQueryString(new()
        {
            ["response_type"] = "code",
            ["client_id"] = _opts.ClientId,
            ["state"] = state.State,
            ["scope"] = "openid profile email subiekt123 offline_access",
            ["redirect_uri"] = _opts.RedirectUri,
            ["code_challenge"] = state.CodeChallenge,
            ["code_challenge_method"] = "S256"
        });
        return ($"{_opts.AuthBaseUrl}/connect/authorize?{qs}", state);
    }

    /// <summary>
    /// Exchanges the authorization code (from OAuth callback) for an access token.
    /// </summary>
    public async Task<TokenInfo> ExchangeCodeAsync(string code, PkceState state, CancellationToken ct = default)
    {
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = _opts.ClientId,
            ["client_secret"] = _opts.ClientSecret,
            ["code"] = code,
            ["redirect_uri"] = _opts.RedirectUri,
            ["code_verifier"] = state.CodeVerifier
        };
        return await PostTokenAsync(form, ct);
    }

    /// <summary>
    /// Refreshes an access token using a refresh token.
    /// </summary>
    public async Task<TokenInfo> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = _opts.ClientId,
            ["client_secret"] = _opts.ClientSecret,
            ["refresh_token"] = refreshToken
        };
        return await PostTokenAsync(form, ct);
    }

    private async Task<TokenInfo> PostTokenAsync(Dictionary<string, string> form, CancellationToken ct)
    {
        var resp = await _http.PostAsync(
            $"{_opts.AuthBaseUrl}/connect/token",
            new FormUrlEncodedContent(form), ct);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"Token endpoint returned {(int)resp.StatusCode} {resp.ReasonPhrase}. Body: {body}");
        }

        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var accessToken = json.GetProperty("access_token").GetString()!;
        var expiresIn = json.GetProperty("expires_in").GetInt32();
        var refreshToken = json.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;

        return new TokenInfo(accessToken, refreshToken, DateTime.UtcNow.AddSeconds(expiresIn));
    }

    private static string BuildQueryString(Dictionary<string, string> qs) =>
        string.Join("&", qs.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
}
