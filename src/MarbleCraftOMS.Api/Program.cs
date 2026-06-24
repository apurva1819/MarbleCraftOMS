using System.Text;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using OpenTelemetry.Trace;
using MarbleCraftOMS.Api.Data;
using MarbleCraftOMS.Core.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MarbleCraftOMS.Application.Catalogue;
using MarbleCraftOMS.Application.Inventory;
using MarbleCraftOMS.Core.Interfaces;
using MarbleCraftOMS.Infrastructure.Persistence;
using MarbleCraftOMS.Infrastructure.Persistence.DapperQueries;
using MarbleCraftOMS.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Asp.Versioning;
using MarbleCraftOMS.Api.Middleware;
using MarbleCraftOMS.Api.Services;
using MarbleCraftOMS.Application.Auth;
using MarbleCraftOMS.Application.Suppliers;
using MarbleCraftOMS.Infrastructure.Services;
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

builder.Services.AddMemoryCache();
builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductBrowseQuery, ProductBrowseQuery>();
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IStockSummaryQuery, StockSummaryQuery>();

builder.Services.AddScoped(typeof(ICache<>), typeof(MemoryCacheAdapter<>));
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();

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

// Policy scheme: routes to "Local" when the token's issuer matches the local JWT issuer,
// otherwise falls back to "Bearer" (Entra ID). This provides a DefaultChallengeScheme so
// plain [Authorize] works without specifying a scheme explicitly.
var localIssuer = builder.Configuration["Jwt:Issuer"] ?? "MarbleCraftOMS";
var authBuilder = builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "Smart";
        options.DefaultChallengeScheme = "Smart";
    })
    .AddPolicyScheme("Smart", "JWT scheme selector", o =>
    {
        o.ForwardDefaultSelector = ctx =>
        {
            var bearer = ctx.Request.Headers.Authorization.FirstOrDefault();
            if (bearer?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true)
            {
                try
                {
                    var raw = bearer["Bearer ".Length..].Trim();
                    var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler()
                        .ReadJwtToken(raw);
                    if (jwt.Issuer == localIssuer) return "Local";
                }
                catch { /* malformed token — let Entra ID scheme reject it */ }
            }
            return Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
        };
    });

// Entra ID JWT — validates bearer tokens issued by Azure AD for this app registration
authBuilder.AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

// Local JWT — validates tokens issued by POST /login (dev/demo flow)
authBuilder.AddJwtBearer("Local", options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

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

// Apply pending migrations and seed initial users on first run
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DbInitializer.SeedAsync(db);
}

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
