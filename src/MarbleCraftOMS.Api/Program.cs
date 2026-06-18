using Azure.Identity;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using OpenTelemetry.Trace;
using MarbleCraftOMS.Core.Constants;
using MarbleCraftOMS.Core.Interfaces;
using MarbleCraftOMS.Infrastructure.Persistence;
using MarbleCraftOMS.Infrastructure.Persistence.Repositories;
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

// OpenTelemetry → App Insights
// Connection string stored in Key Vault as "appinsights-connection-string" — never hardcoded
builder.Services.AddOpenTelemetry()
    .UseAzureMonitor(options =>
    {
        options.ConnectionString = builder.Configuration["appinsights-connection-string"];
    })
    .WithTracing(tracing => tracing
        .AddEntityFrameworkCoreInstrumentation());

// Correlation ID — OpenTelemetry logging exporter stamps trace/span ID on every log entry
builder.Logging.AddOpenTelemetry(o =>
{
    o.IncludeFormattedMessage = true;
    o.IncludeScopes = true;
});

// EF Core — "Authentication=Active Directory Default" in the connection string means
// Managed Identity handles SQL auth in Azure; LocalDB Trusted_Connection handles dev
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();

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

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly",
        policy => policy.RequireRole(Roles.Admin));

    options.AddPolicy("WarehouseAccess",
        policy => policy.RequireRole(Roles.Admin, Roles.WarehouseStaff));

    options.AddPolicy("SalesAccess",
        policy => policy.RequireRole(Roles.Admin, Roles.SalesAgent));

    options.AddPolicy("DistributorAccess",
        policy => policy.RequireRole(Roles.Distributor));

    options.AddPolicy("InternalOnly",
        policy => policy.RequireRole(Roles.Admin, Roles.WarehouseStaff, Roles.SalesAgent));
});
builder.Services.AddAuthorization();
builder.Services.AddControllers();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
