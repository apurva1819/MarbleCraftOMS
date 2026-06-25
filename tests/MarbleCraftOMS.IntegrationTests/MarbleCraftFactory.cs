using MarbleCraftOMS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace MarbleCraftOMS.IntegrationTests;

public class MarbleCraftFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Keep the connection open so the in-memory SQLite DB persists across requests
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public async Task InitializeAsync() => await _connection.OpenAsync();

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _connection.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["KeyVault:Uri"]     = "",
                ["ServiceBus:Namespace"] = "localhost",
                // Valid-looking GUIDs so AddMicrosoftIdentityWebApi registers without throwing
                ["AzureAd:TenantId"]  = "00000000-0000-0000-0000-000000000001",
                ["AzureAd:ClientId"]  = "00000000-0000-0000-0000-000000000002",
                ["AzureAd:Audience"]  = "api://test",
                ["Jwt:Key"]           = "MarbleCraft-Test-Secret-Key-Min32Chars!!",
                ["Jwt:Issuer"]        = "MarbleCraftOMS",
                ["Jwt:Audience"]      = "MarbleCraftOMS.Api",
                ["Jwt:ExpiryMinutes"] = "60",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Swap SQL Server for SQLite backed by the open connection above.
            // Must remove BOTH the options object AND the SqlServer options-configuration
            // registration, otherwise EF sees two providers and throws at context init.
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(_connection));

            // Remove background services — the Channel reader loops would block teardown
            services.RemoveAll<IHostedService>();

            // Raise rate-limit ceilings so tests don't exhaust the 10/min write window
            services.RemoveAll(typeof(IConfigureOptions<RateLimiterOptions>));
            services.AddRateLimiter(opts =>
            {
                opts.AddFixedWindowLimiter("fixed",       o => { o.PermitLimit = 10_000; o.Window = TimeSpan.FromMinutes(1); });
                opts.AddFixedWindowLimiter("fixed-write", o => { o.PermitLimit = 10_000; o.Window = TimeSpan.FromMinutes(1); });
            });
        });
    }
}
