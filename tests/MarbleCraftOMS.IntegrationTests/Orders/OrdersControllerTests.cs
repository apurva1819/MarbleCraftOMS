using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MarbleCraftOMS.Core.Entities;
using MarbleCraftOMS.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace MarbleCraftOMS.IntegrationTests.Orders;

public class OrdersControllerTests : IClassFixture<MarbleCraftFactory>, IAsyncLifetime
{
    private readonly MarbleCraftFactory _factory;
    private HttpClient _salesClient = null!;
    private int _productId;
    private int _stockLotId;

    public OrdersControllerTests(MarbleCraftFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        _salesClient = _factory.CreateClient();
        await SetBearerAsync(_salesClient, "salesagent", "Sales@123");
        (_productId, _stockLotId) = await SeedInventoryAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ── Auth guard ─────────────────────────────────────────────────────────

    [Fact]
    public async Task PlaceOrder_WithoutToken_Returns401()
    {
        var anon = _factory.CreateClient();
        var response = await anon.PostAsJsonAsync("/api/v1/orders", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Validation ─────────────────────────────────────────────────────────

    [Fact]
    public async Task PlaceOrder_WithNoLines_Returns400()
    {
        var response = await _salesClient.PostAsJsonAsync("/api/v1/orders", new
        {
            customerId = 1,
            lines = Array.Empty<object>()
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PlaceOrder_WhenLotNotFound_Returns404()
    {
        var response = await _salesClient.PostAsJsonAsync("/api/v1/orders", new
        {
            customerId = 1,
            lines = new[] { new { productId = _productId, stockLotId = 999999, quantity = 1, unitPrice = 25.00 } }
        });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Happy paths ────────────────────────────────────────────────────────

    [Fact]
    public async Task PlaceOrder_WithValidData_Returns201WithOrderNumber()
    {
        var response = await _salesClient.PostAsJsonAsync("/api/v1/orders", new
        {
            customerId = 1,
            notes = "Integration test order",
            lines = new[] { new { productId = _productId, stockLotId = _stockLotId, quantity = 5, unitPrice = 25.00 } }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.StartsWith("ORD-", body.GetProperty("orderNumber").GetString());
        Assert.True(body.GetProperty("orderId").GetInt32() > 0);
    }

    [Fact]
    public async Task GetAll_Returns200WithArray()
    {
        var response = await _salesClient.GetAsync("/api/v1/orders");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, body.ValueKind);
    }

    [Fact]
    public async Task GetById_WhenExists_Returns200WithLines()
    {
        var orderId = await PlaceTestOrderAsync(quantity: 3);

        var response = await _salesClient.GetAsync($"/api/v1/orders/{orderId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(orderId, body.GetProperty("id").GetInt32());
        Assert.Equal(1, body.GetProperty("lines").GetArrayLength());
    }

    [Fact]
    public async Task GetById_WhenNotFound_Returns404()
    {
        var response = await _salesClient.GetAsync("/api/v1/orders/999999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── State transitions ──────────────────────────────────────────────────

    [Fact]
    public async Task Confirm_PendingOrder_Returns204()
    {
        var orderId = await PlaceTestOrderAsync(quantity: 4);

        var response = await _salesClient.PatchAsync($"/api/v1/orders/{orderId}/confirm", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Confirm_AlreadyConfirmed_Returns409()
    {
        var orderId = await PlaceTestOrderAsync(quantity: 4);
        await _salesClient.PatchAsync($"/api/v1/orders/{orderId}/confirm", null);

        var response = await _salesClient.PatchAsync($"/api/v1/orders/{orderId}/confirm", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Dispatch_ConfirmedOrder_Returns204()
    {
        var orderId = await PlaceTestOrderAsync(quantity: 4);
        await _salesClient.PatchAsync($"/api/v1/orders/{orderId}/confirm", null);

        var response = await _salesClient.PatchAsync($"/api/v1/orders/{orderId}/dispatch", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_PendingOrder_Returns204()
    {
        var orderId = await PlaceTestOrderAsync(quantity: 4);

        var response = await _salesClient.DeleteAsync($"/api/v1/orders/{orderId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_DispatchedOrder_Returns409()
    {
        var orderId = await PlaceTestOrderAsync(quantity: 4);
        await _salesClient.PatchAsync($"/api/v1/orders/{orderId}/confirm", null);
        await _salesClient.PatchAsync($"/api/v1/orders/{orderId}/dispatch", null);

        var response = await _salesClient.DeleteAsync($"/api/v1/orders/{orderId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private async Task<int> PlaceTestOrderAsync(int quantity)
    {
        var response = await _salesClient.PostAsJsonAsync("/api/v1/orders", new
        {
            customerId = 1,
            lines = new[] { new { productId = _productId, stockLotId = _stockLotId, quantity, unitPrice = 25.00 } }
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("orderId").GetInt32();
    }

    private async Task<(int productId, int stockLotId)> SeedInventoryAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var supplier = new Supplier { CompanyName = "IT Marble", ContactName = "Mario", Country = "Italy" };
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync();

        var product = new Product
        {
            Name = "White Marble", Material = "Marble", Format = "Slab",
            Surface = "Polished", Color = "White", Size = "60x60",
            CountryOfOrigin = "Italy", PricePerUnit = 25m, SupplierId = supplier.Id
        };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var lot = new StockLot
        {
            LotNumber = $"LOT-{Guid.NewGuid().ToString("N")[..8]}",
            ProductId = product.Id, SupplierId = supplier.Id,
            QuantityOnHand = 1000, QuantityCommitted = 0,
            UnitCostPerSqm = 20m, ReceivedDate = DateTime.UtcNow.AddMonths(-1)
        };
        db.StockLots.Add(lot);
        await db.SaveChangesAsync();

        return (product.Id, lot.Id);
    }

    private async Task SetBearerAsync(HttpClient client, string username, string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/login", new { username, password });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var token = body.GetProperty("token").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
