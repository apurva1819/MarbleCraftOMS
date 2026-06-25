using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace MarbleCraftOMS.IntegrationTests.Notifications;

public class NotificationsControllerTests : IClassFixture<MarbleCraftFactory>, IAsyncLifetime
{
    private readonly MarbleCraftFactory _factory;
    private HttpClient _adminClient = null!;

    public NotificationsControllerTests(MarbleCraftFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        _adminClient = _factory.CreateClient();
        await SetBearerAsync(_adminClient, "admin", "Admin@123");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ── Auth guard ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetRecent_WithoutToken_Returns401()
    {
        var anon = _factory.CreateClient();
        var response = await anon.GetAsync("/api/v1/notifications");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Read ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetRecent_Returns200WithArray()
    {
        var response = await _adminClient.GetAsync("/api/v1/notifications");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, body.ValueKind);
    }

    [Fact]
    public async Task GetRecent_WithCountParam_Returns200()
    {
        var response = await _adminClient.GetAsync("/api/v1/notifications?count=5");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── Mark read ──────────────────────────────────────────────────────────

    [Fact]
    public async Task MarkRead_Returns204EvenWhenNotificationMissing()
    {
        // MarkRead is idempotent — safe to call with any id
        var response = await _adminClient.PatchAsync("/api/v1/notifications/999/read", null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task MarkAllRead_Returns204()
    {
        var response = await _adminClient.PatchAsync("/api/v1/notifications/read-all", null);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // ── Helper ─────────────────────────────────────────────────────────────

    private async Task SetBearerAsync(HttpClient client, string username, string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/login", new { username, password });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var token = body.GetProperty("token").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
