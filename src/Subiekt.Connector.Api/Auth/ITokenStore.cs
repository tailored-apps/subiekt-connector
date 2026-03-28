namespace Subiekt.Connector.Api.Auth;

public interface ITokenStore
{
    void StoreTokens(string accessToken, string? refreshToken, DateTime expiresAt);
    string? GetAccessToken();
    string? GetRefreshToken();
    bool IsExpired();
    void Clear();
}

public class InMemoryTokenStore : ITokenStore
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
