using FluentAssertions;
using Subiekt.Connector.Api.Auth;
using Subiekt.Connector.Api.Auth.Models;
using Xunit;

namespace Subiekt.Connector.IntegrationTests.Auth;

public class OAuthStateCacheTests
{
    private readonly InMemoryOAuthStateCache _sut = new();

    [Fact]
    public void Set_ThenGet_ReturnsValue()
    {
        var state = new OAuthState("state1", "verifier1", DateTime.UtcNow);
        _sut.Set("state1", state);

        _sut.Get("state1").Should().Be(state);
    }

    [Fact]
    public void Get_UnknownState_ReturnsNull()
    {
        _sut.Get("nonexistent").Should().BeNull();
    }

    [Fact]
    public void Remove_ThenGet_ReturnsNull()
    {
        _sut.Set("s", new OAuthState("s", "v", DateTime.UtcNow));
        _sut.Remove("s");
        _sut.Get("s").Should().BeNull();
    }
}
