using Microsoft.AspNetCore.HttpOverrides;
using Subiekt.Connector.Sdk;
using Subiekt.Connector.Sdk.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// SDK
builder.Services.Configure<SubiektClientOptions>(builder.Configuration.GetSection("Subiekt"));
builder.Services.AddSingleton<InMemoryTokenStore>();
builder.Services.AddSingleton<InMemoryOAuthStateCache>();
builder.Services.AddHttpClient("auth");
builder.Services.AddScoped<SubiektClient>(sp =>
{
    var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SubiektClientOptions>>().Value;
    var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("SubiektSdk");
    var sdk = new SubiektClient(opts) { Log = msg => logger.LogInformation("{SdkLog}", msg) };
    var store = sp.GetRequiredService<InMemoryTokenStore>();
    var accessToken = store.GetAccessToken();
    if (accessToken is not null && !store.IsExpired())
        sdk.SetToken(new TokenInfo(accessToken, store.GetRefreshToken(), store.GetExpiresAt()));
    return sdk;
});

// Forwarded headers (reverse proxy)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();
app.UseForwardedHeaders();
app.UseStaticFiles();
app.UseRouting();
app.UseAntiforgery();
app.MapRazorComponents<Subiekt.Demo.App>()
    .AddInteractiveServerRenderMode();
app.Run();
