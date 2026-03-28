using Microsoft.AspNetCore.Mvc;
using Subiekt.Connector.Api.Auth;
using Subiekt.Connector.Api.Auth.Models;

namespace Subiekt.Connector.Api.Controllers;

/// <summary>OAuth 2.0 PKCE authorization flow for Subiekt 123 API</summary>
[ApiController]
[Route("auth")]
[Tags("Authorization")]
public class AuthController : ControllerBase
{
    private readonly IPkceService _pkce;
    private readonly ITokenStore _tokens;
    private readonly IOAuthStateCache _stateCache;

    public AuthController(IPkceService pkce, ITokenStore tokens, IOAuthStateCache stateCache)
    {
        _pkce = pkce;
        _tokens = tokens;
        _stateCache = stateCache;
    }

    /// <summary>Initiate OAuth 2.0 PKCE login flow — redirects to InsERT login page</summary>
    [HttpGet("login")]
    [ProducesResponseType(302)]
    public IActionResult Login()
    {
        var (verifier, challenge) = _pkce.GeneratePkce();
        var state = _pkce.GenerateState();

        _stateCache.Set(state, new OAuthState(state, verifier, DateTime.UtcNow));

        var url = _pkce.BuildAuthorizationUrl(challenge, state);
        return Redirect(url);
    }

    /// <summary>OAuth callback — exchanges code for token (called by InsERT after login)</summary>
    [HttpGet("callback")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string state)
    {
        var pending = _stateCache.Get(state);
        if (pending is null)
            return BadRequest("Invalid or expired state parameter");

        if (DateTime.UtcNow - pending.CreatedAt > TimeSpan.FromMinutes(5))
        {
            _stateCache.Remove(state);
            return BadRequest("State expired — restart login flow");
        }

        var token = await _pkce.ExchangeCodeAsync(code, pending.CodeVerifier);
        _stateCache.Remove(state);

        _tokens.StoreTokens(
            token.AccessToken,
            token.RefreshToken,
            DateTime.UtcNow.AddSeconds(token.ExpiresIn));

        return Ok(new { message = "Authorization successful", expiresIn = token.ExpiresIn });
    }

    /// <summary>Manually refresh access token using stored refresh token</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = _tokens.GetRefreshToken();
        if (refreshToken is null)
            return BadRequest("No refresh token stored — complete login flow first");

        var token = await _pkce.RefreshTokenAsync(refreshToken);
        _tokens.StoreTokens(
            token.AccessToken,
            token.RefreshToken,
            DateTime.UtcNow.AddSeconds(token.ExpiresIn));

        return Ok(new { message = "Token refreshed", expiresIn = token.ExpiresIn });
    }

    /// <summary>Check current token status</summary>
    [HttpGet("status")]
    [ProducesResponseType(200)]
    public IActionResult Status()
    {
        var hasToken = _tokens.GetAccessToken() is not null;
        return Ok(new
        {
            authorized = hasToken,
            expired = _tokens.IsExpired(),
            hasRefreshToken = _tokens.GetRefreshToken() is not null
        });
    }

    /// <summary>Clear stored tokens (logout)</summary>
    [HttpPost("logout")]
    [ProducesResponseType(200)]
    public IActionResult Logout()
    {
        _tokens.Clear();
        return Ok(new { message = "Logged out" });
    }
}
