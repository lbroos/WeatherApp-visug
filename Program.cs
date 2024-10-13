using Azure.Identity;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.FeatureManagement;
using WeatherApp;
using WeatherApp.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var isDevelopment = builder.Environment.IsDevelopment();
var environment = builder.Environment.EnvironmentName.ToLower();

string connectionString = builder.Configuration.GetConnectionString("AppConfig") ?? throw new ArgumentNullException("AppConfigConnectionString");

builder.Configuration.AddAzureAppConfiguration(
    options =>
    {
        //if (isDevelopment)
        //{
        //    options.Connect(connectionString);
        //}
        //else
        //{
            options.Connect(new Uri(connectionString), new DefaultAzureCredential());
        //}

        options.ConfigureKeyVault(keyVaultOptions =>
        {
            //// Only needed if you are not logged into Visual Studio and/or your account has not been setup to access the Key Vault you are trying to connect.
            //if (isDevelopment)
            //{
            //    var cred = new ClientSecretCredential(
            //        builder.Configuration.GetSection("DevCredential:TenantId").Value,
            //        builder.Configuration.GetSection("DevCredential:ClientId").Value,
            //        builder.Configuration.GetSection("DevCredential:ClientSecret").Value);

            //    keyVaultOptions.SetCredential(cred);
            //} else {

            keyVaultOptions
                .SetCredential(new DefaultAzureCredential())
                .SetSecretRefreshInterval(TimeSpan.FromMinutes(1));
            //}
        });

        options
            .Select(KeyFilter.Any, LabelFilter.Null)
            .Select(KeyFilter.Any, environment ?? LabelFilter.Null)
            .ConfigureRefresh(refreshOptions =>
                refreshOptions
                    .Register("WeatherApp:Sentinel", isDevelopment ? LabelFilter.Null : environment, refreshAll: true)
                    .SetRefreshInterval(TimeSpan.FromSeconds(15)))
            .UseFeatureFlags(featureFlagOptions => 
                featureFlagOptions.SetRefreshInterval(TimeSpan.FromSeconds(15)));
    });

builder.Services.AddAzureAppConfiguration();
builder.Services.AddFeatureManagement();

builder.Services.Configure<WeatherSettings>(builder.Configuration.GetSection("WeatherApp:WeatherSettings"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseAzureAppConfiguration();

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
