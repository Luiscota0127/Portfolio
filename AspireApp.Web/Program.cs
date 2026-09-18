using AspireApp.Web;
using AspireApp.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
builder.AddRedisOutputCache("cache");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Note: token persistence implemented via JS localStorage from ApiAuthService

builder.Services.AddHttpClient<WeatherApiClient>(client =>
    {
        // This URL uses "https+http://" to indicate HTTPS is preferred over HTTP.
        // Learn more about service discovery scheme resolution at https://aka.ms/dotnet/sdschemes.
        client.BaseAddress = new("https+http://apiservice");
    });

// API HttpClients for AspireApp.ApiService
builder.Services.AddTransient<AspireApp.Web.Services.RefreshTokenHandler>();
builder.Services.AddHttpClient("ApiClientNoAuth", client =>
{
    client.BaseAddress = new("https+http://apiservice");
});

builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new("https+http://apiservice");
})
    .AddHttpMessageHandler<AspireApp.Web.Services.RefreshTokenHandler>();

// Auth/API helper service (scoped per user session)
builder.Services.AddScoped<AspireApp.Web.Services.ApiAuthService>();
// CollaborationService registration must remain to support real-time collaboration features.
builder.Services.AddScoped<AspireApp.Web.Services.CollaborationService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
