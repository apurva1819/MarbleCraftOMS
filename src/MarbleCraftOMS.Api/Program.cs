using Azure.Identity;
using MarbleCraftOMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

// Key Vault — reads secrets at startup via Managed Identity; skipped locally (Uri is empty)
var keyVaultUri = builder.Configuration["KeyVault:Uri"];
if (!string.IsNullOrEmpty(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUri),
        new DefaultAzureCredential());
}

// EF Core — "Authentication=Active Directory Default" in the connection string means
// Managed Identity handles SQL auth in Azure; LocalDB Trusted_Connection handles dev
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Service Bus — DefaultAzureCredential resolves to Managed Identity in Azure,
// developer credentials (az login / VS / env vars) locally — no key ever needed
builder.Services.AddAzureClients(clients =>
{
    clients.AddServiceBusClient(builder.Configuration["ServiceBus:Namespace"]);
    clients.UseCredential(new DefaultAzureCredential());
});

// Entra ID JWT — validates bearer tokens issued by Azure AD for this app registration
builder.Services.AddAuthentication()
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization();
builder.Services.AddControllers();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
