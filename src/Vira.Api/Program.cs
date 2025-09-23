using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using Vira.Application;             // AddApplication()
using Vira.Domain.Entities;
using Vira.Infrastructure;
using Vira.Infrastructure.Auth;

var builder = WebApplication.CreateBuilder(args);

// JWT
var jwtKey = builder.Configuration["Jwt:Key"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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

// rate limiting
builder.Services.AddRateLimiter(o => o.AddFixedWindowLimiter("api", opt =>
{
    opt.Window = TimeSpan.FromSeconds(1);
    opt.PermitLimit = 20; // saniyede 20 istek
}));


// ---------- Logging: Serilog ----------
builder.Host.UseSerilog((ctx, sp, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext());

// ---------- Services ----------
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(opt =>
    {
        // ApiController: otomatik 400 üretir; ProblemDetails formatýnda
        // Burada özelleþtirme yapacaksak opt.InvalidModelStateResponseFactory ile yaparýz.
    });

builder.Services.AddOpenApi();                // OpenAPI þema üretimi

builder.Services.AddCors(opt => opt.AddPolicy("frontend", b =>
    b.WithOrigins("https://app.vira.gov.tr", "https://mobile.vira.gov.tr")
     .AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddApplication();            // MediatR + Validators + Behaviors
builder.Services.AddInfrastructure(builder.Configuration); // DbContext + Repo + UoW

builder.Services.AddHealthChecks();

// ---------- Build ----------
var app = builder.Build();
// ---------- Pipeline ----------
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
app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();
app.MapOpenApi();                      // /openapi/v1.json
// pipeline
app.UseRateLimiter();
app.UseHttpsRedirection();
app.UseCors("frontend");
app.MapControllers().RequireRateLimiting("api"); ;                  // <-- Controller route’larý aktif et
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    if (!db.Users.Any(u => u.Email == "admin@vira.gov"))
    {
        db.Users.Add(new User("admin@vira.gov", hasher.Hash("Admin!234"), "Sistem Yöneticisi", "Admin"));
        await db.SaveChangesAsync();
    }
}
app.Run();
