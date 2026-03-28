namespace Subiekt.Connector.Api.Auth.Models;

public record TokenResponse(
    string AccessToken,
    string? RefreshToken,
    int ExpiresIn,
    string TokenType,
    string? IdToken,
    string? Scope
);

public record OAuthState(
    string State,
    string CodeVerifier,
    DateTime CreatedAt
);
