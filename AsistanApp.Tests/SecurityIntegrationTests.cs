using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ilk_projem.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AsistanApp.Tests;

public sealed class SecurityFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), $"safeguardian-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "local-development-only-key-change-me-2026",
                ["Jwt:Issuer"] = "safeguardian.app",
                ["Jwt:Audience"] = "safeguardian-mobile",
                ["RevenueCat:WebhookAuthKey"] = "test-webhook-secret",
                ["Cors:AllowedOrigins:0"] = "https://localhost"
            }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite($"Data Source={_databasePath}"));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && File.Exists(_databasePath)) File.Delete(_databasePath);
    }
}

public sealed class SecurityIntegrationTests : IClassFixture<SecurityFactory>
{
    private readonly SecurityFactory _factory;
    private readonly HttpClient _client;

    public SecurityIntegrationTests(SecurityFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task WrongPasswordIsRejected()
    {
        var account = await RegisterAsync();
        var response = await _client.PostAsJsonAsync("/api/elderly/login", new
        {
            email = account.Email,
            password = "WrongPassword1!"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ForgedTokenCannotReadProtectedData()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/health-records");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "forged.token.value");
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RefreshTokenRotatesAndCannotBeReplayed()
    {
        var account = await RegisterAsync();
        var first = await _client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = account.RefreshToken
        });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var replay = await _client.PostAsJsonAsync("/api/auth/refresh", new
        {
            refreshToken = account.RefreshToken
        });
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
    }

    [Fact]
    public async Task UsersCannotReadEachOthersHealthRecords()
    {
        var first = await RegisterAsync();
        var second = await RegisterAsync();

        var add = Authorized(HttpMethod.Post, "/api/health-records", first.Token, new
        {
            recordType = "heart-rate",
            value = 72,
            unit = "bpm"
        });
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(add)).StatusCode);

        var list = Authorized(HttpMethod.Get, "/api/health-records", second.Token);
        var response = await _client.SendAsync(list);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, json.GetArrayLength());
    }

    [Fact]
    public async Task AccountDeletionRemovesOwnedDataAndSessions()
    {
        var account = await RegisterAsync();
        await _client.SendAsync(Authorized(HttpMethod.Post, "/api/health-records", account.Token, new
        {
            recordType = "glucose",
            value = 95,
            unit = "mg/dL"
        }));

        var delete = Authorized(HttpMethod.Delete, "/api/elderly/account", account.Token, new
        {
            password = account.Password
        });
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(delete)).StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.Users.AnyAsync(x => x.Email == account.Email));
        Assert.False(await db.HealthRecords.AnyAsync());
        Assert.False(await db.RefreshSessions.AnyAsync(x => x.UserId == account.UserId));
    }

    [Fact]
    public async Task WebhookAndSmsFailClosedWithoutAuthentication()
    {
        var webhook = await _client.PostAsJsonAsync("/api/subscription/revenuecat-webhook", new { });
        var sms = await _client.PostAsJsonAsync("/api/emergency-sms/dispatch", new { message = "test" });
        Assert.Equal(HttpStatusCode.Unauthorized, webhook.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, sms.StatusCode);
    }

    private async Task<TestAccount> RegisterAsync()
    {
        var id = Guid.NewGuid().ToString("N");
        var email = $"security-{id}@example.com";
        const string password = "StrongPassword1!";
        var response = await _client.PostAsJsonAsync("/api/elderly-self-enroll", new
        {
            fullName = "Security Test",
            email,
            password,
            phone = "",
            birthDate = ""
        });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var account = new TestAccount(
            email,
            password,
            json.GetProperty("token").GetString()!,
            json.GetProperty("refreshToken").GetString()!,
            json.GetProperty("userId").GetString()!);
        var me = await _client.SendAsync(Authorized(HttpMethod.Get, "/api/me", account.Token));
        Assert.True(
            me.IsSuccessStatusCode,
            $"Newly issued token was rejected: {me.StatusCode}; auth={string.Join(" | ", me.Headers.WwwAuthenticate)}");
        return account;
    }

    private static HttpRequestMessage Authorized(
        HttpMethod method,
        string path,
        string token,
        object? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null) request.Content = JsonContent.Create(body);
        return request;
    }

    private sealed record TestAccount(
        string Email,
        string Password,
        string Token,
        string RefreshToken,
        string UserId);
}
