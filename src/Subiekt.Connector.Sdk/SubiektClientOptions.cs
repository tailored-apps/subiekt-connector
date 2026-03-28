namespace Subiekt.Connector.Sdk;

/// <summary>Configuration for Subiekt 123 API connector.</summary>
public class SubiektClientOptions
{
    /// <summary>OAuth 2.0 Client ID from InsERT Developer Portal.</summary>
    public string ClientId { get; set; } = "";

    /// <summary>OAuth 2.0 Client Secret from InsERT Developer Portal.</summary>
    public string ClientSecret { get; set; } = "";

    /// <summary>Subscription key from InsERT Developer Portal.</summary>
    public string SubscriptionKey { get; set; } = "";

    /// <summary>Redirect URI registered in InsERT Developer Portal (must match exactly).</summary>
    public string RedirectUri { get; set; } = "";

    /// <summary>Subiekt 123 API base URL. Default: https://api.subiekt123.pl/1.1/</summary>
    public string ApiBaseUrl { get; set; } = "https://api.subiekt123.pl/1.1/";

    /// <summary>InsERT OAuth base URL. Default: https://kontoapi.insert.com.pl</summary>
    public string AuthBaseUrl { get; set; } = "https://kontoapi.insert.com.pl";
}
