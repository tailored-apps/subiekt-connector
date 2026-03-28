using Subiekt.Connector.Api.Auth.Models;

namespace Subiekt.Connector.Api.Auth;

public interface IPkceService
{
    (string CodeVerifier, string CodeChallenge) GeneratePkce();
    string GenerateState();
    string BuildAuthorizationUrl(string codeChallenge, string state);
    Task<TokenResponse> ExchangeCodeAsync(string code, string codeVerifier);
    Task<TokenResponse> RefreshTokenAsync(string refreshToken);
}
