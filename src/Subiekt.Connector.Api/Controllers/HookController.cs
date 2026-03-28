using Microsoft.AspNetCore.Mvc;
using Subiekt.Connector.Api.Auth;

namespace Subiekt.Connector.Api.Controllers;

/// <summary>Webhook receiver — receives OAuth redirect from InsERT and exchanges code automatically</summary>
[ApiController]
[Route("hook")]
[Tags("Webhook")]
public class HookController : ControllerBase
{
    private readonly IPkceService _pkce;
    private readonly ITokenStore _tokens;
    private readonly IOAuthStateCache _stateCache;
    private readonly ILogger<HookController> _logger;

    public HookController(
        IPkceService pkce,
        ITokenStore tokens,
        IOAuthStateCache stateCache,
        ILogger<HookController> logger)
    {
        _pkce = pkce;
        _tokens = tokens;
        _stateCache = stateCache;
        _logger = logger;
    }

    /// <summary>
    /// OAuth redirect hook — use this URL as redirect_uri in InsERT developer portal.
    /// On success redirects back to /auth with success flag.
    /// Example redirect_uri: https://yourhost/hook/callback
    /// </summary>
    [HttpGet("callback")]
    [ProducesResponseType(302)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> OAuthCallback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error)
    {
        if (error is not null)
        {
            _logger.LogWarning("OAuth error from InsERT: {Error}", error);
            return Redirect($"/auth?error={Uri.EscapeDataString(error)}");
        }

        if (code is null || state is null)
            return Redirect("/auth?error=missing_params");

        var pending = _stateCache.Get(state);
        if (pending is null)
        {
            _logger.LogWarning("Unknown state: {State}", state);
            return Redirect("/auth?error=invalid_state");
        }

        if (DateTime.UtcNow - pending.CreatedAt > TimeSpan.FromMinutes(5))
        {
            _stateCache.Remove(state);
            return Redirect("/auth?error=state_expired");
        }

        try
        {
            var token = await _pkce.ExchangeCodeAsync(code, pending.CodeVerifier);
            _stateCache.Remove(state);

            _tokens.StoreTokens(
                token.AccessToken,
                token.RefreshToken,
                DateTime.UtcNow.AddSeconds(token.ExpiresIn));

            _logger.LogInformation("OAuth token obtained, expires in {ExpiresIn}s", token.ExpiresIn);

            return Redirect("/auth?success=true");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to exchange code for token");
            return Redirect($"/auth?error={Uri.EscapeDataString("token_exchange_failed")}");
        }
    }
}
