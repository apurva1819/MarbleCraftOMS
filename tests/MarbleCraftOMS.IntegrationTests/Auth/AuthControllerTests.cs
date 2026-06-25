using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace MarbleCraftOMS.IntegrationTests.Auth;

public class AuthControllerTests(MarbleCraftFactory factory)
    : IClassFixture<MarbleCraftFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokenAndRole()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/login", new
        {
            username = "admin",
            password = "Admin@123"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("token", out var token));
        Assert.False(string.IsNullOrEmpty(token.GetString()));

        Assert.True(body.TryGetProperty("role", out var role));
        Assert.Equal("Admin", role.GetString());
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/login", new
        {
            username = "admin",
            password = "wrong"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownUser_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/login", new
        {
            username = "nobody",
            password = "anything"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/inventory/summary");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithValidToken_Returns200()
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/login", new
        {
            username = "admin",
            password = "Admin@123"
        });
        var body = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = body.GetProperty("token").GetString()!;

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/inventory/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
