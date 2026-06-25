using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MarbleCraftOMS.Core.Entities;
using MarbleCraftOMS.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace MarbleCraftOMS.IntegrationTests.E2E;

/// <summary>
/// Full order lifecycle: login → create supplier → create product → seed stock →
/// place order → confirm → dispatch → verify state machine rejects illegal cancel.
/// Exercises two roles (Admin, SalesAgent) and three controllers in one narrative.
/// </summary>
public class OrderLifecycleE2ETests(MarbleCraftFactory factory) : IClassFixture<MarbleCraftFactory>
{
    [Fact]
    public async Task FullOrderLifecycle_PlaceConfirmDispatch_StateIsCorrectAtEachStep()
    {
        // ── Step 1: Admin logs in ──────────────────────────────────────────
        var adminClient = factory.CreateClient();
        await SetBearerAsync(adminClient, "admin", "Admin@123");

        // ── Step 2: Admin creates a supplier ──────────────────────────────
        var supplierResponse = await adminClient.PostAsJsonAsync("/api/v1/suppliers", new
        {
            companyName    = "Tuscany Stone SRL",
            contactName    = "Marco Rossi",
            contactPhone   = "+39 055 1234567",
            contactEmail   = "marco@tuscanystone.it",
            address        = "Via Roma 1, Florence",
            country        = "Italy",
            specialisation = "Marble & Granite"
        });
        Assert.Equal(HttpStatusCode.Created, supplierResponse.StatusCode);
        var supplierBody = await supplierResponse.Content.ReadFromJsonAsync<JsonElement>();
        var supplierId   = supplierBody.GetProperty("id").GetInt32();
        Assert.True(supplierId > 0, "Supplier was created and given a DB-assigned id");

        // ── Step 3: Admin creates a product ───────────────────────────────
        var productResponse = await adminClient.PostAsJsonAsync("/api/v1/products", new
        {
            name            = "Carrara Statuario",
            material        = "Marble",
            format          = "Slab",
            surface         = "Polished",
            color           = "White",
            size            = "120x60",
            countryOfOrigin = "Italy",
            pricePerUnit    = 85.00,
            supplierId
        });
        Assert.Equal(HttpStatusCode.Created, productResponse.StatusCode);
        var productBody = await productResponse.Content.ReadFromJsonAsync<JsonElement>();
        var productId   = productBody.GetProperty("id").GetInt32();
        Assert.Equal("Carrara Statuario", productBody.GetProperty("name").GetString());

        // ── Step 4: Warehouse seeds a stock lot (no HTTP endpoint yet) ────
        // Stock lot creation will be part of a future Warehouse module;
        // for now seed directly so the order flow can be exercised end-to-end.
        var stockLotId = await SeedStockLotAsync(productId, supplierId, onHand: 500);

        // ── Step 5: SalesAgent logs in ────────────────────────────────────
        var salesClient = factory.CreateClient();
        await SetBearerAsync(salesClient, "salesagent", "Sales@123");

        // ── Step 6: SalesAgent places an order ────────────────────────────
        var placeResponse = await salesClient.PostAsJsonAsync("/api/v1/orders", new
        {
            customerId = 1,
            notes      = "E2E test order — Carrara Statuario slabs",
            lines      = new[]
            {
                new { productId, stockLotId, quantity = 20, unitPrice = 85.00 }
            }
        });
        Assert.Equal(HttpStatusCode.Created, placeResponse.StatusCode);
        var placeBody    = await placeResponse.Content.ReadFromJsonAsync<JsonElement>();
        var orderId      = placeBody.GetProperty("orderId").GetInt32();
        var orderNumber  = placeBody.GetProperty("orderNumber").GetString()!;
        Assert.StartsWith("ORD-", orderNumber);

        // ── Step 7: Verify order is Pending ───────────────────────────────
        var pendingOrder = await GetOrderAsync(salesClient, orderId);
        Assert.Equal((int)MarbleCraftOMS.Core.Enums.OrderStatus.Pending,
            pendingOrder.GetProperty("status").GetInt32());
        Assert.Equal(1, pendingOrder.GetProperty("lines").GetArrayLength());

        // ── Step 8: SalesAgent confirms the order ─────────────────────────
        var confirmResponse = await salesClient.PatchAsync(
            $"/api/v1/orders/{orderId}/confirm", null);
        Assert.Equal(HttpStatusCode.NoContent, confirmResponse.StatusCode);

        // ── Step 9: Verify order is Confirmed ─────────────────────────────
        var confirmedOrder = await GetOrderAsync(salesClient, orderId);
        Assert.Equal((int)MarbleCraftOMS.Core.Enums.OrderStatus.Confirmed,
            confirmedOrder.GetProperty("status").GetInt32());

        // ── Step 10: SalesAgent dispatches the order ───────────────────────
        var dispatchResponse = await salesClient.PatchAsync(
            $"/api/v1/orders/{orderId}/dispatch", null);
        Assert.Equal(HttpStatusCode.NoContent, dispatchResponse.StatusCode);

        // ── Step 11: Verify order is Dispatched ───────────────────────────
        var dispatchedOrder = await GetOrderAsync(salesClient, orderId);
        Assert.Equal((int)MarbleCraftOMS.Core.Enums.OrderStatus.Dispatched,
            dispatchedOrder.GetProperty("status").GetInt32());

        // ── Step 12: Business rule — cannot cancel a dispatched order ──────
        var cancelResponse = await salesClient.DeleteAsync($"/api/v1/orders/{orderId}");
        Assert.Equal(HttpStatusCode.Conflict, cancelResponse.StatusCode);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private async Task<JsonElement> GetOrderAsync(HttpClient client, int orderId)
    {
        var response = await client.GetAsync($"/api/v1/orders/{orderId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<int> SeedStockLotAsync(int productId, int supplierId, int onHand)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var lot = new StockLot
        {
            LotNumber        = $"E2E-{Guid.NewGuid().ToString("N")[..8]}",
            ProductId        = productId,
            SupplierId       = supplierId,
            QuantityOnHand   = onHand,
            QuantityCommitted = 0,
            UnitCostPerSqm   = 60m,
            ReceivedDate     = DateTime.UtcNow.AddMonths(-2)
        };
        db.StockLots.Add(lot);
        await db.SaveChangesAsync();
        return lot.Id;
    }

    private async Task SetBearerAsync(HttpClient client, string username, string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/login", new { username, password });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var token = body.GetProperty("token").GetString()!;
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }
}
