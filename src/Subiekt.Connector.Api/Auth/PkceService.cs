using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Subiekt.Connector.Api.Auth.Models;
using Subiekt.Connector.Api.Configuration;

namespace Subiekt.Connector.Api.Auth;

public class PkceService : IPkceService
{
    private const string AuthBaseUrl = "https://kontoapi.insert.com.pl";
    private const string Scopes = "openid profile email subiekt123 offline_access";

    private readonly SubiektOptions _opts;
    private readonly HttpClient _http;

    public PkceService(IOptions<SubiektOptions> opts, IHttpClientFactory factory)
    {
        _opts = opts.Value;
        _http = factory.CreateClient("auth");
    }

    public (string CodeVerifier, string CodeChallenge) GeneratePkce()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        var verifier = Convert.ToBase64String(bytes)
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(verifier));
        var challenge = Convert.ToBase64String(hash)
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

        return (verifier, challenge);
    }

    public string GenerateState()
    {
        var bytes = new byte[16];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public string BuildAuthorizationUrl(string codeChallenge, string state)
    {
        var qs = new Dictionary<string, string>
        {
            ["response_type"] = "code",
            ["client_id"] = _opts.ClientId,
            ["state"] = state,
            ["scope"] = Scopes,
            ["redirect_uri"] = _opts.RedirectUri,
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256"
        };

        var query = string.Join("&", qs.Select(kv =>
            $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

        return $"{AuthBaseUrl}/connect/authorize?{query}";
    }

    public async Task<TokenResponse> ExchangeCodeAsync(string code, string codeVerifier)
    {
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = _opts.ClientId,
            ["client_secret"] = _opts.ClientSecret,
            ["code"] = code,
            ["redirect_uri"] = _opts.RedirectUri,
            ["code_verifier"] = codeVerifier
        };

        return await PostTokenAsync(form);
    }

    public async Task<TokenResponse> RefreshTokenAsync(string refreshToken)
    {
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = _opts.ClientId,
            ["client_secret"] = _opts.ClientSecret,
            ["refresh_token"] = refreshToken
        };

        return await PostTokenAsync(form);
    }

    private async Task<TokenResponse> PostTokenAsync(Dictionary<string, string> form)
    {
        var response = await _http.PostAsync(
            $"{AuthBaseUrl}/connect/token",
            new FormUrlEncodedContent(form));

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        return new TokenResponse(
            json.GetProperty("access_token").GetString()!,
            json.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null,
            json.GetProperty("expires_in").GetInt32(),
            json.GetProperty("token_type").GetString()!,
            json.TryGetProperty("id_token", out var it) ? it.GetString() : null,
            json.TryGetProperty("scope", out var sc) ? sc.GetString() : null
        );
    }
}
