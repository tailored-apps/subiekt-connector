namespace Subiekt.Connector.Sdk.Auth;

/// <summary>
/// Simple in-memory OAuth PKCE state cache for demo/development use.
/// Not suitable for production multi-user scenarios.
/// </summary>
public class InMemoryOAuthStateCache
{
    private readonly Dictionary<string, PkceState> _cache = new();
    private readonly object _lock = new();

    public void Set(string state, PkceState value)
    {
        lock (_lock) _cache[state] = value;
    }

    public PkceState? Get(string state)
    {
        lock (_lock) return _cache.TryGetValue(state, out var v) ? v : null;
    }

    public void Remove(string state)
    {
        lock (_lock) _cache.Remove(state);
    }
}
