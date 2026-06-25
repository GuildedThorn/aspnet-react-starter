using System.Text.Json;
using System.Threading.RateLimiting;
using App.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Sinks.Grafana.Loki;

// ---------- Load env first ----------
// Optional: container / CI / flake deploys supply real environment variables
// directly, so don't crash when there's no .env file on disk.
if (File.Exists(".env")) DotNetEnv.Env.Load(".env");

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// Config sources: appsettings.json (SDK default) + optional Resources/config.json + env vars.
builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("Resources/config.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();
var configuration = builder.Configuration;

// ---------- Logging ----------
// Console always; ship to Grafana Loki only when Loki:Uri is configured.
var logConfig = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");

var lokiUri = configuration["Loki:Uri"];
if (!string.IsNullOrWhiteSpace(lokiUri)) {
    logConfig = logConfig.WriteTo.GrafanaLoki(lokiUri, labels: [
        new LokiLabel { Key = "app", Value = "app" },
        new LokiLabel { Key = "env", Value = builder.Environment.EnvironmentName },
    ]);
}
Log.Logger = logConfig.CreateLogger();
builder.Host.UseSerilog();

// ---------- Services ----------
services.AddHttpClient();
services.AddControllers();
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();
services.AddHealthChecks();
services.AddHttpContextAccessor();

// ---- JWT signing key (base64-encoded HMAC-SHA256 key) ----
var keyBytes = Convert.FromBase64String(
    configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key not configured (base64 HMAC-SHA256 key)."));
var key = new SymmetricSecurityKey(keyBytes);
services.AddSingleton(key);
services.AddSingleton(new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

// ---- CORS ----
// Dev defaults to the Vite server; override with Cors:Origins in config/env.
var corsOrigins = configuration.GetSection("Cors:Origins").Get<string[]>()
    ?? ["https://localhost:5173"];
services.AddCors(options => {
    options.AddPolicy("AllowFrontend", policy => policy
        .WithOrigins(corsOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

// ---- Auth ----
services.AddAuthorizationBuilder()
    .AddPolicy("PrivilegedOnly", p => p.RequireRole("owner", "user"));

services.AddAuthentication(options => {
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = configuration["Jwt:Issuer"],
            ValidAudience = configuration["Jwt:Audience"],
            IssuerSigningKey = key,
            NameClaimType = "name",
        };
        // Read the JWT from the HttpOnly "token" cookie the auth endpoints set.
        options.Events = new JwtBearerEvents {
            OnMessageReceived = ctx => {
                if (ctx.Request.Cookies.TryGetValue("token", out var token))
                    ctx.Token = token;
                return Task.CompletedTask;
            },
        };
    });

// ---- SignalR ----
services.AddSignalR(opts => opts.EnableDetailedErrors = builder.Environment.IsDevelopment())
    .AddJsonProtocol(opts => opts.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);

// ---- App services ----
services.AddSingleton<MongoDbService>();
services.AddSingleton<RabbitMqService>();
services.AddSingleton<JwtTokenService>();

// ---- WebAuthn / FIDO2 (passkeys, security keys) ----
// RP ID must be the site's registrable domain (no scheme/port); Origins must be
// the exact browser origins. Defaults target local dev; set Fido2:* for prod.
services.AddSingleton<WebAuthnChallengeStore>();
services.AddFido2(options => {
    options.ServerDomain = configuration["Fido2:ServerDomain"] ?? "localhost";
    options.ServerName = configuration["Fido2:ServerName"] ?? "App";
    options.Origins = new HashSet<string>(
        configuration.GetSection("Fido2:Origins").Get<string[]>() ?? ["https://localhost:5173"]);
});

// ---- Forwarded headers (real client IP/scheme behind a reverse proxy) ----
services.Configure<ForwardedHeadersOptions>(options => {
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

services.AddHsts(options => {
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
    options.Preload = true;
});

// ---- Rate limiting: per-IP window guarding the auth endpoints ----
services.AddRateLimiter(options => {
    options.RejectionStatusCode = 429;
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions {
                PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0,
            }));
});

// ---------- Build ----------
var app = builder.Build();

// ---------- Middleware ----------
app.UseForwardedHeaders();

if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
} else {
    app.UseHsts();
}

app.UseHttpsRedirection();

// ---- Security headers on every response ----
app.Use(async (context, next) => {
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
    headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "base-uri 'self'; " +
        "object-src 'none'; " +
        "frame-ancestors 'none'; " +
        "img-src 'self' data: https:; " +
        "font-src 'self' data:; " +
        "style-src 'self' 'unsafe-inline'; " +   // React/Tailwind inline styles
        "script-src 'self'; " +
        "connect-src 'self' ws: wss:; " +          // /api + SignalR
        "form-action 'self'; " +
        "upgrade-insecure-requests";
    await next();
});

// Vite content-hashes everything under /assets, so cache those immutably for a
// year; index.html must always revalidate so redeploys are picked up.
static void SetStaticCache(StaticFileResponseContext ctx) {
    var resp = ctx.Context.Response;
    if (ctx.Context.Request.Path.StartsWithSegments("/assets"))
        resp.Headers.CacheControl = "public, max-age=31536000, immutable";
    else if (string.Equals(ctx.File.Name, "index.html", StringComparison.OrdinalIgnoreCase))
        resp.Headers.CacheControl = "no-cache";
    else
        resp.Headers.CacheControl = "public, max-age=604800";
}

app.UseStaticFiles(new StaticFileOptions { OnPrepareResponse = SetStaticCache });
app.UseRouting();
app.UseCors("AllowFrontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// ---- Hubs & controllers ----
app.MapHub<RealtimeHub>("/hub").RequireCors("AllowFrontend");
app.MapControllers().RequireCors("AllowFrontend");
app.MapHealthChecks("/health").AllowAnonymous();

// ---- SPA fallback ----
// Client-routed paths return index.html; unknown /api paths get a plain 404.
string[] spaRoutes = ["/", "/login", "/register", "/notes", "/settings"];
foreach (var route in spaRoutes)
    app.MapGet(route, ctx => ServeSpaIndex(ctx, StatusCodes.Status200OK));

app.MapFallback(ctx => {
    if (ctx.Request.Path.StartsWithSegments("/api")) {
        ctx.Response.StatusCode = StatusCodes.Status404NotFound;
        return Task.CompletedTask;
    }
    return ServeSpaIndex(ctx, StatusCodes.Status404NotFound);
});

app.Run();

static Task ServeSpaIndex(HttpContext context, int statusCode) {
    context.Response.StatusCode = statusCode;
    context.Response.ContentType = "text/html; charset=utf-8";
    var indexPath = Path.Combine(
        context.RequestServices.GetRequiredService<IHostEnvironment>().ContentRootPath,
        "wwwroot", "index.html");
    // The frontend may not be built (backend tests / CI) — return the status
    // without the SPA shell rather than throwing on a missing file.
    return File.Exists(indexPath) ? context.Response.SendFileAsync(indexPath) : Task.CompletedTask;
}

// Exposed so integration tests can boot the real app via WebApplicationFactory<Program>.
public partial class Program { }
