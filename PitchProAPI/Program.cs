using Microsoft.AspNetCore.Mvc;
using BusinessLayer.Services;
using BusinessLayer.Interfaces;
using BusinessLayer.Helpers;
using Chargily.Pay;
using Chargily.Pay.AspNet;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using System.Security.Claims;
using PitchProAPI.Middleware;

var builder = WebApplication.CreateBuilder(args);

// On Linux containers (Render/Railway) the default config file reload uses
// FileSystemWatcher -> inotify. The free-tier host caps inotify instances (128),
// which crashes startup with "user limit on the number of inotify instances".
// Rebuild the file-based config sources with auto-reload disabled so no file
// watcher is registered on hosts where config files never change at runtime.
var env = builder.Environment.EnvironmentName;

builder.Configuration.Sources.Clear();
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
builder.Configuration.AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: false);
builder.Configuration.AddEnvironmentVariables();

// ========================================================
// 1. DEPENDENCY INJECTION REGISTRATIONS (Before .Build())
// ========================================================

// Add azure storage
builder.Services.AddScoped<BlobService>();

// Add email service
builder.Services.AddScoped<IEmailService, EmailService>();

// Add cache
builder.Services.AddMemoryCache();

builder.Services.AddScoped<PlanLimitService>();

builder.Services.AddHttpContextAccessor();

// Add controllers with Custom Model Validation formatting
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errorMessage = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault();

            return new BadRequestObjectResult(new { message = errorMessage });
        };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Define the CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowPitchProVue",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173",
                "https://pitch-pro-frontend.vercel.app",
                "https://pitchprobackend-production.up.railway.app",
                "https://pitchpro.islam-taleb.me") // Note: Removed trailing slash to prevent CORS issues 
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

// FIXED: Moved Chargily registration UP before builder.Build()
builder.Services
    .AddGlobalChargilyPayClient(config =>
    {
        config.IsLiveMode = false;
        config.ApiSecretKey = "test_sk_wgaVTKRNY1ijpPCCqxFWVHT1t6i6pCqKqBUEuHmE";
    })
    .AddChargilyPayWebhookValidationMiddleware();





//// Register authentication services in the dependency injection container.
//// JwtBearerDefaults.AuthenticationScheme tells ASP.NET Core that
//// JWT Bearer authentication will be the default authentication method.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // TokenValidationParameters define how incoming JWTs will be validated.
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Ensures the token has not expired.
            ValidateLifetime = true,

            // The login token does not include issuer or audience claims.
            ValidateIssuer = false,
            ValidateAudience = false,

            ClockSkew = TimeSpan.Zero,
            // Ensures the token signature is valid and was signed by the API.
            ValidateIssuerSigningKey = true,

            // The secret key used to validate the JWT signature.
            // This must be the same key used when generating the token.
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });



builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("AuthLimiter", httpContext =>
    {
        var ip = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
                 ?? httpContext.Connection.RemoteIpAddress?.ToString()
                 ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ip,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });

    // Per-user limiter for authenticated endpoints.
    // Keyed by the JWT subject (NameIdentifier claim) AND the endpoint, so each
    // authenticated user gets 20 requests/minute for each individual endpoint.
    // Falls back to the client IP when there is no authenticated user (e.g. if
    // the attribute is ever placed on an anonymous endpoint) so the limiter
    // stays safe and never collapses everyone into one shared bucket.
    options.AddPolicy("UserLimiter", httpContext =>
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            userId = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
                     ?? httpContext.Connection.RemoteIpAddress?.ToString()
                     ?? "anonymous";
        }

        var endpoint = httpContext.GetEndpoint()?.DisplayName ?? httpContext.Request.Path.ToString();
        var partitionKey = $"user:{userId}|endpoint:{endpoint}";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: partitionKey,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });
});



builder.Services.AddHttpClient();


// --------------------------------------------------------
// FIX 1: FORCE .NET TO BIND TO RAILWAY'S PORT
// --------------------------------------------------------
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://*:{port}");


// ========================================================
// 2. BUILD THE APPLICATION
// ========================================================
var app = builder.Build();

// Allow static Staff classes to resolve your registered cache service context
AppServicesHelper.ServiceProvider = app.Services;

var cacheInstance = app.Services.GetRequiredService<IMemoryCache>();
BusinessLayer.ClubSubscription.InitializeCache(cacheInstance);


// ========================================================
// 3. MIDDLEWARE PIPELINE CONFIGURATION
// ========================================================

// FIX 2: Enable Swagger for BOTH local dev and Railway Production.
// Also, maps Swagger to the root URL (/) so you don't get a 404 landing page.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PitchPro API V1");
    c.RoutePrefix = string.Empty; // <-- This forces Swagger to load immediately at your root URL!
});

// Enable CORS policy
app.UseCors("AllowPitchProVue");

// NOTE: Only use HTTPS redirection if you have configured certificates locally.
// Railway handles SSL termination automatically at the proxy level.
app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// UseRateLimiter MUST come after UseRouting (endpoint metadata) AND after
// UseAuthentication (so the JWT user id is available for the UserLimiter
// policy). Placing it before auth meant the per-user limiter had no user id.
app.UseRateLimiter();

app.MapControllers();

app.Run();