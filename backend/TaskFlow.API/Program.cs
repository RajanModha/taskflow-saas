using System.Security.Claims;
using System.Reflection;
using System.Text;
using Asp.Versioning;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Asp.Versioning.ApiExplorer;
using HealthChecks.UI.Client;
using MediatR;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Context;
using Serilog.Events;
using Serilog.Formatting.Json;
using TaskFlow.API;
using TaskFlow.API.Middleware;
using TaskFlow.API.ExceptionHandling;
using TaskFlow.Application;
using TaskFlow.Application.Abstractions;
using TaskFlow.Application.Auth;
using TaskFlow.Application.Workspaces;
using TaskFlow.Infrastructure;
using TaskFlow.Infrastructure.Auth;
using TaskFlow.Infrastructure.Email;
using TaskFlow.Infrastructure.Features.Tasks;
using TaskFlow.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading.RateLimiting;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        new JsonFormatter(),
        "logs/taskflow-.json",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .CreateLogger();

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 10_485_760; // 10MB
});

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddMemoryCache();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddMediatR(typeof(TaskFlow.Infrastructure.DependencyInjection).Assembly);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "60";
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            type = "rate_limit_exceeded",
            title = "Too many requests",
            detail = "Please slow down and try again shortly.",
            status = 429,
        }, ct);
    };

    // Endpoint policies (e.g. auth) apply in addition to this global limiter.
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var userId = httpContext.User.FindFirst("sub")?.Value
                     ?? httpContext.Connection.RemoteIpAddress?.ToString()
                     ?? "anon";

        return RateLimitPartition.GetSlidingWindowLimiter(
            userId,
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 200,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 4,
                QueueLimit = 0,
                AutoReplenishment = true,
            });
    });

    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true,
            }));

    options.AddPolicy("strict", httpContext =>
        RateLimitPartition.GetTokenBucketLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 5,
                ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                TokensPerPeriod = 1,
                AutoReplenishment = true,
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            }));

    options.AddPolicy("api", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.User.FindFirst("sub")?.Value
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "anon",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true,
            }));

    options.AddPolicy("export", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.User.FindFirst("sub")?.Value
            ?? httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "anon",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0,
                AutoReplenishment = true,
            }));

    // Single endpoint attribute: chain auth (10/min per IP) + strict token bucket (same as "strict").
    options.AddPolicy("auth_strict", httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.Get(ip, _ => RateLimiter.CreateChained(
            new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true,
            }),
            new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = 5,
                ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                TokensPerPeriod = 1,
                AutoReplenishment = true,
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            })));
    });
});

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new QueryStringApiVersionReader("api-version"),
        new HeaderApiVersionReader("x-api-version"));
})
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

builder.Services.AddSwaggerGen(c =>
{
    c.ExampleFilters();
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TaskFlow API",
        Version = "v1",
        Description = """
            TaskFlow is a multi-tenant project and task management API.
            
            **Authentication**: Use Bearer JWT. Register -> verify email -> login -> use token.
            
            **Versioning**: URL segment (/api/v1/), query string (?api-version=1.0),
            or header (x-api-version: 1.0).
            
            **Rate Limits**: Auth endpoints 10 req/min. API 200 req/min per user.
            
            Built with .NET 8, EF Core, Resend email.
            """,
        Contact = new OpenApiContact { Name = "Your Name", Url = new Uri("https://github.com/yourusername") },
        License = new OpenApiLicense { Name = "MIT" },
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter your JWT token. Get it from POST /api/v1/Auth/login",
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
            },
            Array.Empty<string>()
        },
    });
    c.IncludeXmlComments(Path.Combine(
        AppContext.BaseDirectory,
        $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
});

builder.Services.AddSwaggerExamplesFromAssemblyOf<TaskFlow.API.Swagger.DashboardStatsExampleProvider>();

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
                  ?? throw new InvalidOperationException($"Configuration section '{JwtSettings.SectionName}' is required.");
if (string.IsNullOrWhiteSpace(jwtSettings.SigningKey) || jwtSettings.SigningKey.Length < 32)
{
    throw new InvalidOperationException("Jwt:SigningKey must be at least 32 characters.");
}

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        // JWT validation is strict to avoid accepting cross-audience tokens.
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role,
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        "OwnerPolicy",
        policy => policy.RequireClaim(WorkspaceJwtClaims.Role, WorkspaceRoleStrings.Owner));
    options.AddPolicy(
        "AdminPolicy",
        policy => policy.RequireAssertion(ctx =>
        {
            var v = ctx.User.FindFirst(WorkspaceJwtClaims.Role)?.Value;
            return v is WorkspaceRoleStrings.Owner or WorkspaceRoleStrings.Admin;
        }));
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    if (builder.Environment.IsDevelopment())
    {
        // Docker / dev reverse proxies are often not in the default loopback-only proxy list.
#pragma warning disable ASPDEPR005 // Clear legacy + current collections so forwarded headers work in dev (.NET 10).
        options.KnownNetworks.Clear();
#pragma warning restore ASPDEPR005
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    }
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<TaskFlowDbContext>("database")
    .AddCheck("self", () => HealthCheckResult.Healthy());

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("Email"));

var emailSettings = builder.Configuration.GetSection("Email").Get<EmailSettings>() ?? new EmailSettings();
if (!builder.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(emailSettings.ApiKey))
{
    throw new InvalidOperationException("Email:ApiKey must be configured in non-development environments.");
}

builder.Services.AddHttpClient<Resend.ResendClient>();
builder.Services.Configure<Resend.ResendClientOptions>(options =>
{
    options.ApiToken = builder.Configuration["Email:ApiKey"] ?? string.Empty;
    options.ThrowExceptions = true;
});
builder.Services.TryAddTransient<Resend.IResend, Resend.ResendClient>();
builder.Services.AddScoped<IEmailService, ResendEmailService>();
builder.Services.AddHostedService<ReminderHostedService>();

var allowedCorsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
if (allowedCorsOrigins is null || allowedCorsOrigins.Length == 0)
{
    throw new InvalidOperationException("Cors:AllowedOrigins must contain at least one origin.");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "TaskFlowPolicy",
        policy =>
        {
            policy
                .WithOrigins(allowedCorsOrigins)
                .AllowAnyMethod()
                .WithHeaders("Content-Type", "Authorization", "x-api-version")
                .WithExposedHeaders("X-Correlation-ID", "Retry-After");
        });
});

var app = builder.Build();

app.UseForwardedHeaders();

await app.InitializeAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskFlow API v1");
        c.DocumentTitle = "TaskFlow API Docs";
        c.DisplayRequestDuration();
        c.EnableFilter();
        c.EnableDeepLinking();
    });
}

app.Use(async (ctx, next) =>
{
    var correlationId = ctx.Request.Headers["X-Correlation-ID"].FirstOrDefault();
    correlationId = string.IsNullOrWhiteSpace(correlationId)
        ? Guid.NewGuid().ToString("N")
        : correlationId.Trim();
    if (correlationId.Length > 64 || correlationId.Any(char.IsControl))
    {
        correlationId = Guid.NewGuid().ToString("N");
    }

    ctx.Items["CorrelationId"] = correlationId;
    ctx.Response.Headers["X-Correlation-ID"] = correlationId;

    using (LogContext.PushProperty("CorrelationId", correlationId))
    {
        await next();
    }
});
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diag, ctx) =>
    {
        diag.Set("UserId", ctx.User.FindFirst("sub")?.Value ?? "anon");
        diag.Set("CorrelationId", ctx.Items["CorrelationId"]?.ToString() ?? string.Empty);
    };
    options.GetLevel = (ctx, _, ex) => ex is not null || ctx.Response.StatusCode >= 500
        ? LogEventLevel.Error
        : LogEventLevel.Information;
});
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseExceptionHandler();
// Avoid HTTP->HTTPS redirects in local dev: browser preflight (OPTIONS) requests
// fail CORS when redirected.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors("TaskFlowPolicy");
app.UseRateLimiter();
app.UseAuthentication();
// Tenant guard runs after auth so claim-based org resolution is available.
app.UseMiddleware<TenantGuardMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
});

try
{
    app.Run();
}
finally
{
    Log.CloseAndFlush();
}
