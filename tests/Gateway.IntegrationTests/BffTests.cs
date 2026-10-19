using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Gateway.IntegrationTests;

public sealed class BffFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            services.AddAuthentication(o => { o.DefaultAuthenticateScheme = "TestOnly"; o.DefaultChallengeScheme = "TestOnly"; })
                .AddScheme<AuthenticationSchemeOptions, TestIdentity>("TestOnly", _ => { });
        });
    }
}
// Exists in test assembly only; no switch/header can enable this in the application.
public sealed class TestIdentity(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("Test-Identity")) return Task.FromResult(AuthenticateResult.NoResult());
        var identity = new ClaimsIdentity(new[] { new Claim("tid", "11111111-1111-1111-1111-111111111111"),
            new Claim("oid", "22222222-2222-2222-2222-222222222222"), new Claim("name", "Test requester"), new Claim("roles", "LibraryReader") }, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }
}
public sealed class BffTests : IClassFixture<BffFactory>
{
    private readonly BffFactory factory;
    public BffTests(BffFactory factory) => this.factory = factory;
    private HttpClient Client(bool signedIn = false)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });
        if (signedIn) client.DefaultRequestHeaders.Add("Test-Identity", "A");
        return client;
    }
    [Theory][InlineData("/auth/session")][InlineData("/auth/csrf")][InlineData("/api/documents")][InlineData("/api/exports/a/download")]
    public async Task Anonymous_API_is_401_without_HTML_redirect(string path)
    {
        using var client = Client(); var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode); Assert.Null(response.Headers.Location);
        Assert.NotEqual("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString());
    }
    [Fact] public async Task Actual_cookie_challenge_is_structured_without_redirect()
    {
        using var cookieHost = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.PostConfigure<AuthenticationOptions>(o => {
                o.DefaultAuthenticateScheme = "Cookies"; o.DefaultChallengeScheme = "Cookies";
            })));
        using var client = cookieHost.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });
        var response = await client.GetAsync("/auth/session");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode); Assert.Null(response.Headers.Location);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("SESSION_REQUIRED", await response.Content.ReadAsStringAsync());
    }
    [Theory][InlineData("/api/unknown")][InlineData("/auth/unknown")][InlineData("/api/no-such-file.xlsx")]
    public async Task Reserved_routes_never_fall_back_to_SPA(string path)
    {
        using var client = Client(); var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
    [Fact] public async Task Missing_CSRF_blocks_logout_and_all_mutations()
    {
        using var client = Client(true);
        foreach (var path in new[] { "/auth/logout", "/api/documents", "/api/exports" })
        {
            var response = await client.PostAsync(path, new StringContent("{}"));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("CSRF", await response.Content.ReadAsStringAsync());
        }
    }
    [Fact] public async Task Token_must_match_cookie_and_valid_pair_passes_CSRF_layer()
    {
        using var client = Client(true);
        var csrf = await client.GetAsync("/auth/csrf"); csrf.EnsureSuccessStatusCode();
        var cookie = csrf.Headers.GetValues("Set-Cookie").Single();
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase); Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        var token = JsonDocument.Parse(await csrf.Content.ReadAsStringAsync()).RootElement.GetProperty("token").GetString();
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", "invalid");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/api/unknown", new StringContent("{}"))).StatusCode);
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN"); client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", token);
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsync("/api/unknown", new StringContent("{}"))).StatusCode);
        using var other = Client(true); other.DefaultRequestHeaders.Add("X-CSRF-TOKEN", token);
        Assert.Equal(HttpStatusCode.BadRequest, (await other.PostAsync("/api/unknown", new StringContent("{}"))).StatusCode);
    }
    [Fact] public async Task Simulated_download_and_unconfigured_store_fail_closed()
    {
        using var client = Client(true);
        Assert.Equal(HttpStatusCode.Conflict, (await client.GetAsync("/api/exports/a/download")).StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/api/documents")).StatusCode);
    }
    [Fact] public async Task Session_contains_no_tokens_and_login_without_config_is_unavailable()
    {
        using var client = Client(true); var response = await client.GetAsync("/auth/session");
        var body = await response.Content.ReadAsStringAsync(); Assert.DoesNotContain("token", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Simulated", body); Assert.Contains("no-store", response.Headers.CacheControl?.ToString());
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/auth/login?returnUrl=https://example.invalid")).StatusCode);
    }
    [Fact] public async Task Export_OpenAPI_from_actual_server_when_requested()
    {
        using var client = Client(); var response = await client.GetAsync("/openapi/v1.json"); response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("DocumentDto", json); Assert.Contains("SessionDto", json);
        var output = Environment.GetEnvironmentVariable("GATEWAY_EXPORT_OPENAPI");
        if (!string.IsNullOrWhiteSpace(output)) await File.WriteAllTextAsync(output, json);
    }
    [Theory][InlineData("/library")][InlineData("/my-documents")][InlineData("/reports")][InlineData("/my-exports")][InlineData("/audit")]
    public async Task UI_route_reload_serves_compiled_SPA(string path)
    {
        using var client = Client(); var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("<div id=\"root\">", await response.Content.ReadAsStringAsync());
    }
}
