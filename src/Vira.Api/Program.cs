using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

using Vira.Application;                 // AddApplication()
using Vira.Domain.Entities;
using Vira.Infrastructure;             // AddInfrastructure()
using Vira.Infrastructure.Auth;        // IPasswordHasher

// ----------------------------------------------------
// Builder
// ----------------------------------------------------
var builder = WebApplication.CreateBuilder(args);

// ---- Helper: rate limit partition key (userId → IP) ----
static string GetClientPartition(HttpContext http)
{
    var uid = http.User.FindFirst("sub")?.Value
           ?? http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (!string.IsNullOrWhiteSpace(uid))
        return $"user:{uid}";

    var ip = http.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    return $"ip:{ip}";
}

// ---------------- JWT ----------------
var jwtKey = builder.Configuration["Jwt:Key"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

// ---------------- Rate Limiting ----------------
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (ctx, _) =>
    {
        ctx.HttpContext.Response.Headers["Retry-After"] = "60";
        await Results.Json(new
        {
            code = "TooManyRequests",
            message = "İstek sınırını aştınız. Lütfen biraz sonra tekrar deneyin."
        }).ExecuteAsync(ctx.HttpContext);
    };

    // Global: kullanıcı/IP başına dakikada 60 istek
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(http =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetClientPartition(http),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // Upload: 30 sn'de 10 dosya
    options.AddPolicy("Uploads", http =>
        RateLimitPartition.GetTokenBucketLimiter(
            GetClientPartition(http),
            _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 10,
                TokensPerPeriod = 10,
                ReplenishmentPeriod = TimeSpan.FromSeconds(30),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    // Upload eşzamanlı işlem sınırı (opsiyonel)
    options.AddPolicy("UploadsConcurrency", _ =>
        RateLimitPartition.GetConcurrencyLimiter(
            partitionKey: "uploads-global",
            factory: _ => new ConcurrencyLimiterOptions
            {
                PermitLimit = Math.Max(2, Environment.ProcessorCount * 2),
                QueueLimit = 0
            }));

    // Auth: 1 dk’da 10 deneme (brute-force koruması)
    options.AddPolicy("Auth", http =>
        RateLimitPartition.GetSlidingWindowLimiter(
            GetClientPartition(http),
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6, // 10 sn'lik dilimler
                QueueLimit = 0
            }));

    // Bazı yerlerde RequireRateLimiting("api") varsa 500 almamak için
    options.AddPolicy("api", http =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetClientPartition(http),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // Yorum: 1 dk'da 30 işlem (sliding window)
    options.AddPolicy("Comments", http =>
        RateLimitPartition.GetSlidingWindowLimiter(
            GetClientPartition(http),
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6, // 10 sn'lik dilimler
                QueueLimit = 0
            }));

    // Arama: 10 sn'de 20 istek (fixed window) -> ağır sorguları korur
    options.AddPolicy("Search", http =>
        RateLimitPartition.GetSlidingWindowLimiter(
            GetClientPartition(http),
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromSeconds(10),
                SegmentsPerWindow = 6,
                QueueLimit = 0
            }));

    // Yazma: 1 dk'da 60 (istersen daha sıkı yapabilirsin)
    options.AddPolicy("Writes", http =>
        RateLimitPartition.GetFixedWindowLimiter(
            GetClientPartition(http),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

});

// ---------------- Serilog ----------------
builder.Host.UseSerilog((ctx, sp, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext());

// ---------------- Services ----------------
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(_ => { /* ProblemDetails varsayılanı kalsın */ });

builder.Services.AddOpenApi(); // .NET 9 OpenAPI (v1: /openapi/v1.json)

// Prod’a göre ayarla; geliştirmede localhost’u da ekleyebilirsin
builder.Services.AddCors(opt => opt.AddPolicy("frontend", b =>
    b.WithOrigins(
         "https://app.vira.gov.tr",
         "https://mobile.vira.gov.tr",
         "http://localhost:5173", "http://localhost:4200") // dev için
     .AllowAnyHeader()
     .AllowAnyMethod()
     .AllowCredentials()));

builder.Services.AddApplication();                         // MediatR + Validators + Behaviors
builder.Services.AddInfrastructure(builder.Configuration); // DbContext + Repo + UoW

// Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Default")!);

// ----------------------------------------------------
// Build
// ----------------------------------------------------
var app = builder.Build();

// ----------------------------------------------------
// Pipeline
// ----------------------------------------------------

// Proxy arkasında doğru IP almak için (rate limit için önemli!)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Global hata cevabı (ProblemDetails)
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async ctx =>
    {
        var ex = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        var problem = new ProblemDetails
        {
            Type = "https://httpstatuses.com/500",
            Title = "Unexpected error",
            Status = StatusCodes.Status500InternalServerError,
            Detail = ex?.Message
        };
        ctx.Response.StatusCode = problem.Status ?? 500;
        ctx.Response.ContentType = "application/problem+json";
        await ctx.Response.WriteAsJsonAsync(problem);
    });
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSerilogRequestLogging();
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["Referrer-Policy"] = "no-referrer";
    await next();
});
app.UseCors("frontend");

app.UseAuthentication();
app.UseAuthorization();

// Swagger/Health/OpenAPI hariç tüm yollara rate limit uygula
app.UseWhen(
    ctx => !ctx.Request.Path.StartsWithSegments("/swagger")
        && !ctx.Request.Path.StartsWithSegments("/openapi")
        && !ctx.Request.Path.StartsWithSegments("/health"),
    branch => branch.UseRateLimiter());

// Controller’ları haritaya ekle (404’ün sebebi buydu)
app.MapControllers();

// Health endpoints
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// OpenAPI (v1 json)
app.MapOpenApi();

// ---- Seed: admin kullanıcı (idempotent) ----
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

    if (!db.Users.Any(u => u.Email == "admin@vira.gov"))
    {
        db.Users.Add(new User(
            email: "admin@vira.gov",
            passwordHash: hasher.Hash("Admin!234"),
            fullName: "Sistem Yöneticisi",
            role: "Admin"));

        await db.SaveChangesAsync();
    }
}

// ----------------------------------------------------
app.Run();

