namespace Subiekt.Connector.Sdk.Auth;

/// <summary>
/// Simple in-memory token store for demo/development use.
/// Not suitable for production multi-user scenarios.
/// </summary>
public class InMemoryTokenStore
{
    private string? _accessToken;
    private string? _refreshToken;
    private DateTime _expiresAt = DateTime.MinValue;
    private readonly object _lock = new();

    public void StoreTokens(string accessToken, string? refreshToken, DateTime expiresAt)
    {
        lock (_lock)
        {
            _accessToken = accessToken;
            if (refreshToken != null) _refreshToken = refreshToken;
            _expiresAt = expiresAt;
        }
    }

    public string? GetAccessToken()
    {
        lock (_lock) return _accessToken;
    }

    public string? GetRefreshToken()
    {
        lock (_lock) return _refreshToken;
    }

    public DateTime GetExpiresAt()
    {
        lock (_lock) return _expiresAt;
    }

    public bool IsExpired()
    {
        lock (_lock)
        {
            if (_expiresAt == DateTime.MinValue) return true;
            return DateTime.UtcNow >= _expiresAt.AddMinutes(-2);
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _accessToken = null;
            _refreshToken = null;
            _expiresAt = DateTime.MinValue;
        }
    }
}
