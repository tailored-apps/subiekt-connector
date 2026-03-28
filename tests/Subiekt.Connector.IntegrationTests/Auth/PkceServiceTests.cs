using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Subiekt.Connector.Api.Auth;
using Subiekt.Connector.Api.Configuration;
using Xunit;

namespace Subiekt.Connector.IntegrationTests.Auth;

public class PkceServiceTests
{
    private readonly PkceService _sut;

    public PkceServiceTests()
    {
        var opts = Options.Create(new SubiektOptions
        {
            ClientId = "test-client",
            ClientSecret = "test-secret",
            RedirectUri = "https://localhost/hook/callback",
            SubscriptionKey = "test-key"
        });
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("auth")).Returns(new HttpClient());
        _sut = new PkceService(opts, factory.Object);
    }

    [Fact]
    public void GeneratePkce_ReturnsValidCodeVerifierAndChallenge()
    {
        var (verifier, challenge) = _sut.GeneratePkce();

        verifier.Should().NotBeNullOrEmpty();
        verifier.Should().MatchRegex(@"^[A-Za-z0-9\-_]+$");
        verifier.Length.Should().BeGreaterThanOrEqualTo(43);

        // Verify challenge = BASE64URL(SHA256(verifier))
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(verifier));
        var expectedChallenge = Convert.ToBase64String(hash)
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

        challenge.Should().Be(expectedChallenge);
    }

    [Fact]
    public void GeneratePkce_EachCallProducesUniquePair()
    {
        var (v1, c1) = _sut.GeneratePkce();
        var (v2, c2) = _sut.GeneratePkce();

        v1.Should().NotBe(v2);
        c1.Should().NotBe(c2);
    }

    [Fact]
    public void GenerateState_ReturnsUniqueValues()
    {
        var s1 = _sut.GenerateState();
        var s2 = _sut.GenerateState();

        s1.Should().NotBeNullOrEmpty();
        s1.Should().NotBe(s2);
    }

    [Fact]
    public void BuildAuthorizationUrl_ContainsRequiredParameters()
    {
        var url = _sut.BuildAuthorizationUrl("challenge123", "state456");

        url.Should().Contain("response_type=code");
        url.Should().Contain("client_id=test-client");
        url.Should().Contain("code_challenge=challenge123");
        url.Should().Contain("code_challenge_method=S256");
        url.Should().Contain("state=state456");
        url.Should().Contain("subiekt123");
        url.Should().StartWith("https://kontoapi.insert.com.pl/connect/authorize");
    }
}
