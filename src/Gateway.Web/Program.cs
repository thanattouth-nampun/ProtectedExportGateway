using System.Security.Claims;
using System.Text.Json.Serialization;
using Gateway.Web;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);
if (builder.Configuration["Gateway:Mode"] is { } mode && mode != "Simulated")
    throw new InvalidOperationException("LiveMip is unavailable until G1/G2 and durable release integration pass.");
builder.Logging.AddFilter("Microsoft.AspNetCore.Authentication", LogLevel.Warning);
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow);
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
if (builder.Environment.IsDevelopment() && OperatingSystem.IsWindows())
{
    var keyPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "../../.runtime/web-keys"));
    builder.Services.AddDataProtection().SetApplicationName("ProtectedExportGateway.Local")
        .PersistKeysToFileSystem(new DirectoryInfo(keyPath)).ProtectKeysWithDpapi();
}
builder.Services.AddAntiforgery(o =>
{
    o.HeaderName = "X-CSRF-TOKEN";
    o.Cookie.Name = "__Host-gateway-csrf";
    o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Strict;
});
var tenant = builder.Configuration["WebIdentity:TenantId"];
var clientId = builder.Configuration["WebIdentity:ClientId"];
var configured = Guid.TryParse(tenant, out var tenantGuid) && tenantGuid != Guid.Empty
    && Guid.TryParse(clientId, out var clientGuid) && clientGuid != Guid.Empty
    && !string.IsNullOrWhiteSpace(builder.Configuration["WebIdentity:ClientSecret"]);
// Disabled until explicitly configured. No development persona/authentication backdoor.
var auth = builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.Cookie.Name = "__Host-gateway-session";
        o.Cookie.HttpOnly = true; o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        o.Cookie.SameSite = SameSiteMode.Lax; o.ExpireTimeSpan = TimeSpan.FromMinutes(30); o.SlidingExpiration = false;
        o.Events.OnRedirectToLogin = c => { c.Response.StatusCode = 401; return c.Response.WriteAsJsonAsync(new { code = "SESSION_REQUIRED" }); };
        o.Events.OnRedirectToAccessDenied = c => { c.Response.StatusCode = 403; return c.Response.WriteAsJsonAsync(new { code = "ACCESS_DENIED" }); };
        o.Events.OnValidatePrincipal = c =>
        {
            var tid = c.Principal?.FindFirstValue("tid"); var oid = c.Principal?.FindFirstValue("oid");
            if (!Guid.TryParse(tid, out var actual) || actual != tenantGuid || !Guid.TryParse(oid, out _)) c.RejectPrincipal();
            return Task.CompletedTask;
        };
    });
if (configured)
    auth.AddOpenIdConnect("Entra", o =>
    {
        o.Authority = $"https://login.microsoftonline.com/{tenantGuid:D}/v2.0";
        o.ClientId = clientId; o.ClientSecret = builder.Configuration["WebIdentity:ClientSecret"];
        o.ResponseType = "code"; o.UsePkce = true; o.SaveTokens = false; o.MapInboundClaims = false;
        o.CallbackPath = "/auth/callback"; o.SignedOutCallbackPath = "/auth/signed-out";
        o.Scope.Clear(); o.Scope.Add("openid"); o.Scope.Add("profile");
        o.TokenValidationParameters = new TokenValidationParameters { ValidateIssuer = true, NameClaimType = "name", RoleClaimType = "roles" };
        o.Events.OnTokenValidated = c =>
        {
            if (!Guid.TryParse(c.Principal?.FindFirstValue("tid"), out var actual) || actual != tenantGuid
                || !Guid.TryParse(c.Principal?.FindFirstValue("oid"), out _)) c.Fail("TENANT_OR_IDENTITY_INVALID");
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();
var app = builder.Build();
app.UseExceptionHandler();
if (!app.Environment.IsDevelopment()) { app.UseHsts(); app.UseHttpsRedirection(); }
app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; font-src 'self'; img-src 'self' data:; connect-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
    if (context.Request.Path.StartsWithSegments("/api") || context.Request.Path.StartsWithSegments("/auth"))
        context.Response.Headers.CacheControl = "no-store";
    await next();
});
app.UseAuthentication(); app.UseAuthorization();
// Validate ALL unsafe API/auth methods, including unknown routes and logout.
app.Use(async (context, next) =>
{
    if ((context.Request.Path.StartsWithSegments("/api") || context.Request.Path.StartsWithSegments("/auth"))
        && !HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method)
        && !HttpMethods.IsOptions(context.Request.Method)
        && context.Request.Path != "/auth/callback")
    {
        if (context.User.Identity?.IsAuthenticated != true) { await Results.Problem(statusCode: 401, title: "Session required").ExecuteAsync(context); return; }
        try { await context.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(context); }
        catch (AntiforgeryValidationException) { await Results.Problem(statusCode: 400, title: "CSRF validation failed").ExecuteAsync(context); return; }
    }
    await next();
});
app.MapGet("/health/live", () => Results.Ok(new { status = "alive" }));
app.MapGet("/health/ready", () => Results.Problem(statusCode: 503, title: "Workflow dependencies not configured"));
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapGet("/auth/login", (string? returnUrl) =>
{
    if (!configured) return Results.Problem(statusCode: 503, title: "Entra configuration required");
    var destination = returnUrl is not null && new[] { "/library", "/my-documents", "/reports", "/my-exports", "/audit" }.Contains(returnUrl) ? returnUrl : "/library";
    return Results.Challenge(new AuthenticationProperties { RedirectUri = destination }, ["Entra"]);
});
app.MapGet("/auth/session", (ClaimsPrincipal user) => new SessionDto(user.FindFirstValue("tid")!, user.FindFirstValue("oid")!,
    user.FindFirstValue("name") ?? "Signed-in user", user.FindAll("roles").Select(c => c.Value).Order().ToArray(), "Simulated")).RequireAuthorization();
app.MapGet("/auth/csrf", (HttpContext ctx, IAntiforgery csrf) => new CsrfDto(csrf.GetAndStoreTokens(ctx).RequestToken!)).RequireAuthorization();
app.MapPost("/auth/logout", () => configured
    ? Results.SignOut(new AuthenticationProperties { RedirectUri = "/sign-in" }, [CookieAuthenticationDefaults.AuthenticationScheme, "Entra"])
    : Results.SignOut(new AuthenticationProperties { RedirectUri = "/sign-in" }, [CookieAuthenticationDefaults.AuthenticationScheme])).RequireAuthorization();
// Explicitly unavailable rather than inventing published sources or in-memory durable jobs.
app.MapGet("/api/documents", () => Results.Problem(statusCode: 503, title: "Document store not configured")).Produces<DocumentPageDto>().RequireAuthorization();
app.MapGet("/api/exports/{id}/download", (string id) => Results.Problem(statusCode: 409, title: "Simulated mode has no downloadable artifacts")).RequireAuthorization();
app.MapMethods("/api/{**path}", ["GET", "HEAD", "POST", "PATCH", "PUT", "DELETE"], () => Results.Problem(statusCode: 404, title: "API route not implemented"));
app.MapMethods("/auth/{**path}", ["GET", "HEAD", "POST", "PATCH", "PUT", "DELETE"], () => Results.Problem(statusCode: 404, title: "Authentication route not found"));
app.UseStaticFiles(new StaticFileOptions { FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(Path.Combine(app.Environment.ContentRootPath, "wwwroot", "app")) });
foreach (var route in new[] { "/", "/library", "/my-documents", "/reports", "/my-exports", "/audit", "/sign-in" })
    app.MapGet(route, () => Results.File(Path.Combine(app.Environment.ContentRootPath, "wwwroot", "app", "index.html"), "text/html"));
app.Run();
public partial class Program;
