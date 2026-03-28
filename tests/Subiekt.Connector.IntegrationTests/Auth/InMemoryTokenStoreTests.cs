using FluentAssertions;
using Subiekt.Connector.Api.Auth;
using Xunit;

namespace Subiekt.Connector.IntegrationTests.Auth;

public class InMemoryTokenStoreTests
{
    private readonly InMemoryTokenStore _sut = new();

    [Fact]
    public void InitialState_HasNoToken()
    {
        _sut.GetAccessToken().Should().BeNull();
        _sut.GetRefreshToken().Should().BeNull();
        _sut.IsExpired().Should().BeTrue();
    }

    [Fact]
    public void StoreTokens_ThenGet_ReturnsStoredValues()
    {
        var expiry = DateTime.UtcNow.AddHours(1);
        _sut.StoreTokens("access123", "refresh456", expiry);

        _sut.GetAccessToken().Should().Be("access123");
        _sut.GetRefreshToken().Should().Be("refresh456");
        _sut.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenTokenExpiresSoon_ReturnsTrue()
    {
        _sut.StoreTokens("token", null, DateTime.UtcNow.AddSeconds(60));
        _sut.IsExpired().Should().BeTrue(); // 2-min buffer
    }

    [Fact]
    public void Clear_RemovesAllTokens()
    {
        _sut.StoreTokens("access", "refresh", DateTime.UtcNow.AddHours(1));
        _sut.Clear();

        _sut.GetAccessToken().Should().BeNull();
        _sut.GetRefreshToken().Should().BeNull();
        _sut.IsExpired().Should().BeTrue();
    }

    [Fact]
    public void StoreTokens_NullRefreshToken_PreservesExisting()
    {
        _sut.StoreTokens("access1", "refresh1", DateTime.UtcNow.AddHours(1));
        _sut.StoreTokens("access2", null, DateTime.UtcNow.AddHours(2));

        _sut.GetRefreshToken().Should().Be("refresh1");
        _sut.GetAccessToken().Should().Be("access2");
    }
}
