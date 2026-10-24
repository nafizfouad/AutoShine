using AutoShine.Data;
using AutoShine.Data.Seeders;
using AutoShine.Middleware;
using AutoShine.Repository.Implementations;
using AutoShine.Repository.Interfaces;
using AutoShine.Service.Implementations;
using AutoShine.Service.Interfaces;
using Mapster;
using MapsterMapper;
using AutoShine.Service.Mappings;
using AutoShine.Service.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Hosting platforms (Render, Koyeb, Cloud Run, ...) tell the app which port to listen on via PORT.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// ─── Configuration ─────────────────────────────────────────────────────────
// Secrets live in appsettings.json locally (git-ignored; copy appsettings.example.json)
// and in environment variables in production, e.g. ConnectionStrings__DefaultConnection,
// Jwt__SecretKey. Environment variables override appsettings.json.
var config = builder.Configuration;

var connectionString = config.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection is not configured. Set it in appsettings.json (local) " +
        "or the ConnectionStrings__DefaultConnection environment variable (production).");

var jwtKey = config["Jwt:SecretKey"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException(
        "Jwt:SecretKey must be set and at least 32 characters long. Set it in appsettings.json (local) " +
        "or the Jwt__SecretKey environment variable (production).");

// Non-secret defaults, so production only has to provide the secrets.
config["Jwt:Issuer"] ??= "AutoShine.API";
config["Jwt:Audience"] ??= "AutoShine.Client";
config["Jwt:ExpiryHours"] ??= "24";

// ─── Database ──────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

// ─── Shop settings ─────────────────────────────────────────────────────────
// Time zone that schedule templates (e.g. 09:00–17:00) are expressed in.
builder.Services.AddSingleton(ShopSettings.FromTimeZoneId(config["Shop:TimeZone"]));

// ─── Repositories ──────────────────────────────────────────────────────────
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IPackageRepository, PackageRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IScheduleRepository, ScheduleRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ─── Services ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IPackageService, PackageService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IScheduleService, ScheduleService>();
builder.Services.AddScoped<IProfileService, ProfileService>();

// ─── Mapster ───────────────────────────────────────────────────────────────
var mapsterConfig = TypeAdapterConfig.GlobalSettings;
mapsterConfig.Scan(typeof(AutoShineProfile).Assembly);
builder.Services.AddSingleton(mapsterConfig);
builder.Services.AddScoped<IMapper, ServiceMapper>();

// ─── JWT Authentication ────────────────────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        // Reject tokens of users who were deactivated or whose role changed after login.
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async ctx =>
            {
                var idClaim = ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var roleClaim = ctx.Principal?.FindFirstValue(ClaimTypes.Role);
                var users = ctx.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
                var user = int.TryParse(idClaim, out var userId) ? await users.GetByIdAsync(userId) : null;

                if (user == null || !user.IsActive || user.Role.ToString() != roleClaim)
                    ctx.Fail("Account is inactive or its role has changed. Please sign in again.");
            }
        };
    });

builder.Services.AddAuthorization();

// ─── CORS ──────────────────────────────────────────────────────────────────
// Only needed when the frontend is served from a different origin than the API.
// The Docker image serves both from one origin.
var corsOrigins = (config["Cors:AllowedOrigins"] ?? "http://localhost:5173,http://localhost:3000")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactClient", policy =>
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// ─── Reverse proxy & health ────────────────────────────────────────────────
// Hosting platforms terminate TLS at their proxy; trust its X-Forwarded-* headers.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.AddHealthChecks();

// ─── Controllers & Swagger ─────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AutoShine API",
        Version = "v1",
        Description = "Auto Repair & Car Wash Management System API"
    });

    // JWT Auth in Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token (without 'Bearer' prefix)"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ─── Database Migration + Seeding ─────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    // Demo accounts are only ever created in Development.
    var seedSettings = new SeedSettings(
        AdminEmail: config["Seed:AdminEmail"],
        AdminPassword: config["Seed:AdminPassword"],
        SeedDemoUsers: app.Environment.IsDevelopment() && config.GetValue<bool>("Seed:DemoUsers"),
        DemoPassword: config["Seed:DemoPassword"]);
    var seedLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseSeeder");
    await DatabaseSeeder.SeedAsync(db, seedSettings, seedLogger);
}

// ─── Middleware Pipeline ───────────────────────────────────────────────────
app.UseForwardedHeaders();
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "AutoShine API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

// The Docker image puts the built React app in wwwroot; `dotnet run` alone serves only the API.
var serveSpa = Directory.Exists(app.Environment.WebRootPath);
if (serveSpa)
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

app.UseCors("ReactClient");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

if (serveSpa)
{
    // Unknown API routes stay 404; every other path is a client-side route of the React app.
    app.MapFallback("/api/{**path}", () => Results.NotFound());
    app.MapFallbackToFile("index.html");
}

app.Run();
