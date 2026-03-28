namespace Subiekt.Connector.Sdk.Auth;

/// <summary>Stored OAuth token set.</summary>
public record TokenInfo(
    string AccessToken,
    string? RefreshToken,
    DateTime ExpiresAt
)
{
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt.AddMinutes(-2);
}

/// <summary>OAuth token response from InsERT.</summary>
internal record OAuthTokenResponse(
    string AccessToken,
    string? RefreshToken,
    int ExpiresIn,
    string TokenType,
    string? Scope
);

/// <summary>Pending PKCE OAuth state (stored while waiting for callback).</summary>
public record PkceState(
    string State,
    string CodeVerifier,
    string CodeChallenge,
    DateTime CreatedAt
)
{
    public bool IsExpired => DateTime.UtcNow - CreatedAt > TimeSpan.FromMinutes(5);
}
