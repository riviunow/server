using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

using Endpoint.Extensions;
using Endpoint.SignalRHub;
using Shared.Config;
using Domain.Enums;
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

var configBuilder = builder.Configuration.SetBasePath(Directory.GetCurrentDirectory())
    .AddEnvironmentVariables();

if (builder.Environment.IsDevelopment())
    configBuilder.AddJsonFile("appsettings.json", reloadOnChange: true, optional: false);

var config = configBuilder.Build();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

builder.Services.Configure<JwtSettings>(config.GetSection("JwtSettings"));
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>() ?? throw new InvalidOperationException("JwtSettings configuration section is missing or invalid.");

    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSettings.SecretKey)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnChallenge = async context =>
        {
            if (!context.Response.HasStarted)
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogWarning("Unauthorized request {Method} {Path} resulted in 401 Unauthorized",
                    context.HttpContext.Request.Method, context.HttpContext.Request.Path);

                context.HandleResponse();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new { message = "Unauthorized - Token is invalid or expired." }));
            }
        }
    };
});

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminPolicy", policy => policy.RequireRole(Role.Admin.ToString()))
    .AddPolicy("UserPolicy", policy => policy.RequireRole(Role.User.ToString(), Role.Admin.ToString()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", builder =>
    {
        var allowedOrigins = config.GetSection("AllowedOrigins").Get<string[]>();
        if (allowedOrigins != null)
        {
            _ = builder.WithOrigins(allowedOrigins)
                       .AllowAnyMethod()
                       .AllowAnyHeader();
        }
    });
    options.AddPolicy("AllowAnyOrigin", builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddOptions();
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(config.GetSection("RateLimiting"));
builder.Services.Configure<IpRateLimitPolicies>(config.GetSection("RateLimitingPolicies"));
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ISharedDb, SharedDb>();
builder.Services.AddSignalR();

AppServiceCollectionExtensions.AddInfrastructure(builder.Services, config);
AppServiceCollectionExtensions.AddApplicationServices(builder.Services, config);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/root/.aspnet/DataProtection-Keys"))
    .SetApplicationName("MyApp");

var loggerFactory = LoggerFactory.Create(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);
});

var logger = loggerFactory.CreateLogger<Program>();

logger.LogInformation("=============== Application Configuration ===============");
logger.LogInformation("Rate Limit: {Limit}", config.GetValue<int>("RateLimiting:Limit"));
logger.LogInformation("Rate Limit Period: {Period}", config.GetValue<string>("RateLimiting:Period"));
logger.LogInformation("Refresh Token Expiry (days): {RefreshTokenExpiryInDays}", config.GetValue<int>("JwtSettings:RefreshTokenExpiryInDays"));
logger.LogInformation("Access Token Expiry (hours): {AccessTokenExpiryInHours}", config.GetValue<int>("JwtSettings:AccessTokenExpiryInHours"));
logger.LogInformation("Allowed Origins: {AllowedOrigins}", config.GetSection("CorsPolicy").Value != "AllowAnyOrigin" ? string.Join(", ", config.GetSection("AllowedOrigins").Get<string[]>() ?? []) : "Any");

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Warning);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
    app.UseHttpsRedirection();
}

app.UseCors(config.GetSection("CorsPolicy").Value ?? "AllowAnyOrigin");
app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();

    var originalBodyStream = context.Response.Body;

    using var memoryStream = new MemoryStream();
    context.Response.Body = memoryStream;

    try
    {
        await next();

        if (context.Response.StatusCode >= 400)
        {
            memoryStream.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(memoryStream).ReadToEndAsync();
            memoryStream.Seek(0, SeekOrigin.Begin);

            logger.LogWarning("Request {Method} {Path} resulted in {StatusCode} with response: {ResponseBody}",
                context.Request.Method, context.Request.Path, context.Response.StatusCode, responseBody);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unhandled exception for request {Method} {Path}",
            context.Request.Method, context.Request.Path);

        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { message = "Internal Server Error" }));
        }
    }
    finally
    {
        memoryStream.Seek(0, SeekOrigin.Begin);
        await memoryStream.CopyToAsync(originalBodyStream);
        context.Response.Body = originalBodyStream;
    }
});

app.UseStaticFiles();
app.MapControllers();
app.MapHub<VocabHub>("/vocabhub");

app.Run();
