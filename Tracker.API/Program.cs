using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Tracker.API.Data;
using Tracker.Infrastructure.Data;
using Tracker.Infrastructure.Models;
using Tracker.API.Services;
using Tracker.Shared.Auth;

// Create the builder and configure it to use specific URLs
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    EnvironmentName = Environments.Development,
    ApplicationName = "Tracker.API",
    WebRootPath = "wwwroot"
});

// Explicitly configure Kestrel to use our desired ports
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenLocalhost(5002); // HTTP
    serverOptions.ListenLocalhost(5003, listenOptions =>
    {
        listenOptions.UseHttps(); // HTTPS
    });
});

// Add services to the container.
builder.Services.AddControllers();

// Options and supporting services
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();

// Application services
builder.Services.AddScoped<IAuthService, AuthService>();

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Add SQLite DbContext
builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
{
    var logger = serviceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
    
    // Enable detailed errors and sensitive data logging in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging()
               .EnableDetailedErrors();
    }
});

// Configure Identity
builder.Services.AddIdentity<User, IdentityRole<Guid>>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["Secret"] ?? "DefaultSecretKey_ShouldBeLongAndSecure";
var key = Encoding.ASCII.GetBytes(secretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Tracker API", Version = "v1" });
    
    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.\r\n\r\nEnter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Tracker API v1"));
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Simple test endpoint
app.MapGet("/test", () => "API is running!");

// Seed admin user in Development environment
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await DataSeeder.SeedAdminUserAsync(scope.ServiceProvider);
}

// Database test endpoint with detailed diagnostics
app.MapGet("/test-db", async (ApplicationDbContext dbContext, IConfiguration config) =>
{
    try
    {
        var connectionString = config.GetConnectionString("DefaultConnection");
        var databaseProvider = dbContext.Database.ProviderName;
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
        var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync();
        
        // Test database connection with timeout
        var canConnect = false;
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        
        try
        {
            canConnect = await dbContext.Database.CanConnectAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException)
        {
            return Results.Problem("Database connection timed out after 5 seconds");
        }
        
        // If database doesn't exist, create it and apply migrations
        if (!canConnect)
        {
            try 
            {
                await dbContext.Database.EnsureCreatedAsync();
                canConnect = await dbContext.Database.CanConnectAsync();
                
                if (canConnect)
                {
                    await dbContext.Database.MigrateAsync();
                }
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: $"Error initializing database: {ex}", title: "Database Initialization Error");
            }
        }
        
        // Get table counts if possible
        int? userCount = null;
        try
        {
            userCount = await dbContext.Users.CountAsync();
        }
        catch (Exception)
        {
            // Ignore if we can't get counts
        }
        
        return Results.Ok(new 
        {
            status = "Success",
            database = new 
            {
                provider = databaseProvider,
                connectionString = connectionString,
                canConnect,
                pendingMigrations = pendingMigrations.ToArray(),
                appliedMigrations = appliedMigrations.ToArray()
            },
            tables = new 
            {
                users = userCount != null ? $"Exists ({userCount} users)" : "Not accessible"
            },
            environment = app.Environment.EnvironmentName,
            timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            statusCode: 500,
            title: "Database Error",
            detail: ex.ToString(),
            type: "https://tools.ietf.org/html/rfc7231#section-6.6.1"
        );
    }
});

// Health check endpoint
app.MapGet("/health", async (ApplicationDbContext dbContext) =>
{
    try
    {
        var canConnect = await dbContext.Database.CanConnectAsync();
        return canConnect 
            ? Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow })
            : Results.Problem("Cannot connect to database", statusCode: 503);
    }
    catch (Exception ex)
    {
        return Results.Problem("Health check failed: " + ex.Message, statusCode: 503);
    }
});

app.Run();
