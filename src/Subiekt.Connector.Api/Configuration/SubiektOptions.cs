namespace Subiekt.Connector.Api.Configuration;

public class SubiektOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string SubscriptionKey { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;

    public const string AuthorizationEndpoint = "https://kontoapi.insert.com.pl/connect/authorize";
    public const string TokenEndpoint = "https://kontoapi.insert.com.pl/connect/token";
    public const string Scope = "openid profile email subiekt123 offline_access";
}
