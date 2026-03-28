using Subiekt.Connector.Api.Auth.Models;

namespace Subiekt.Connector.Api.Auth;

public interface IOAuthStateCache
{
    void Set(string state, OAuthState value);
    OAuthState? Get(string state);
    void Remove(string state);
}

public class InMemoryOAuthStateCache : IOAuthStateCache
{
    private readonly Dictionary<string, OAuthState> _cache = new();
    private readonly object _lock = new();

    public void Set(string state, OAuthState value)
    {
        lock (_lock) _cache[state] = value;
    }

    public OAuthState? Get(string state)
    {
        lock (_lock) return _cache.TryGetValue(state, out var v) ? v : null;
    }

    public void Remove(string state)
    {
        lock (_lock) _cache.Remove(state);
    }
}
