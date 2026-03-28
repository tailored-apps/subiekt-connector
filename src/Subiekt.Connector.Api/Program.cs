using Subiekt.Connector.Api.Auth;
using Subiekt.Connector.Api.Configuration;
using Subiekt.Connector.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.Configure<SubiektOptions>(builder.Configuration.GetSection("Subiekt"));
builder.Services.AddSingleton<ITokenStore, InMemoryTokenStore>();
builder.Services.AddSingleton<IOAuthStateCache, InMemoryOAuthStateCache>();
builder.Services.AddScoped<IPkceService, PkceService>();
builder.Services.AddHttpClient("auth");
builder.Services.AddHttpClient<ISubiektApiClient, SubiektApiClient>(c =>
    c.BaseAddress = new Uri("https://api.subiekt123.pl/1.1/"));

var app = builder.Build();
app.MapOpenApi();
app.MapControllers();
app.Run();
