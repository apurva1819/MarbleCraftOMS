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
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Asp.Versioning;
using MarbleCraftOMS.Api.Middleware;
using Microsoft.OpenApi;

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
// Skipped locally when Key Vault is not configured
var appInsightsConnStr = builder.Configuration["appinsights-connection-string"];
if (!string.IsNullOrEmpty(appInsightsConnStr))
{
    builder.Services.AddOpenTelemetry()
        .UseAzureMonitor(options => options.ConnectionString = appInsightsConnStr)
        .WithTracing(tracing => tracing
            .AddEntityFrameworkCoreInstrumentation());
}

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
builder.Services.AddScoped<IProductRepository, ProductRepository>();

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("fixed", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
    options.AddPolicy("fixed-write", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
    options.RejectionStatusCode = 429;
});

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
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MarbleCraftOMS API",
        Version = "v1",
        Description = "Order Management System for marble tile importing"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your Azure Entra ID JWT token"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
});

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddControllers();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "MarbleCraftOMS v1");
    options.RoutePrefix = "swagger";
});

app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<AuditMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.Run();
