using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using Tracker.Client;
using Tracker.Client.Services;
using Tracker.Shared.Services;
using Blazored.LocalStorage;
using Blazored.Toast;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HTTP client with base address
var baseAddress = builder.Configuration["BaseAddress"] ?? builder.HostEnvironment.BaseAddress;
if (baseAddress.EndsWith("/"))
{
    baseAddress = baseAddress[..^1]; // Remove trailing slash
}

// Register HTTP client with authentication
builder.Services.AddScoped(sp =>
{
    var httpClient = new HttpClient { BaseAddress = new Uri(baseAddress) };
    
    // Get the auth state
    var authState = sp.GetRequiredService<AuthenticationStateProvider>();
    
    // Add request interceptor for auth token
    httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    
    return httpClient;
});

// Register Blazored services
builder.Services.AddBlazoredLocalStorage();

// Register Blazored.Toast
builder.Services.AddBlazoredToast();

// Register application services
builder.Services.AddScoped<IIncidentService, IncidentService>();
builder.Services.AddScoped<IIndividualService, IndividualService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<ITimelineService, TimelineService>();

// Register ToastService with manual resolution to avoid circular dependency
builder.Services.AddScoped<IToastService, ToastService>();

// Register log service
builder.Services.AddScoped<Tracker.Client.Services.ILogService, LogService>();

// Register auth services
builder.Services.AddOptions();
builder.Services.AddAuthorizationCore();

// First register the CustomAuthStateProvider
builder.Services.AddScoped<CustomAuthStateProvider>();
// Then register it as the implementation of AuthenticationStateProvider
builder.Services.AddScoped<AuthenticationStateProvider>(provider => 
    provider.GetRequiredService<CustomAuthStateProvider>());

// Register AuthService
builder.Services.AddScoped<IAuthService, AuthService>();

// Configure the token refresh event after all services are registered
builder.Services.AddScoped(provider =>
{
    var authService = provider.GetRequiredService<IAuthService>();
    var authStateProvider = provider.GetRequiredService<CustomAuthStateProvider>();
    
    // Set up the token refresh event
    authStateProvider.TokenRefreshRequested += async () =>
    {
        await authService.RefreshToken();
    };
    
    return authStateProvider;
});

await builder.Build().RunAsync();
