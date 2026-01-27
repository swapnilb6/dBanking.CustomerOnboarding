using Core;
using Infrastructure;
using Core.Mappers;
using Core.RepositoryContracts;
using Core.ServiceContracts;
using Core.Services;
using dBanking.Core.Services;
using dBanking.CustomerOnbaording.API.Middlewares;
using API.Consumers;
using FluentValidation.AspNetCore; // Add this using directive
using Infrastructure.DbContext;
using Infrastructure.Repositories;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Logging;
using Microsoft.OpenApi.Models;
using Npgsql;
using System.Net;
using API.Consumers;


var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Add services
Infrastructure.dependancyInjection.AddInfrastructureServices(builder.Services);
Core.dependancyInjection.AddCoreServices(builder.Services);
builder.Services.AddScoped<IKycCaseService, KycCaseService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICorrelationAccessor, HttpCorrelationAccessor>();

builder.Services.AddScoped<IAuditRepository, AuditRepository>();
builder.Services.AddScoped<IAuditService, AuditService>();

builder.WebHost.ConfigureKestrel((ctx, kestrel) =>
{
    // Bind HTTP only on 9090
    kestrel.Listen(IPAddress.Any, 9090);
});

// AuthN / AuthZ
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("App.Read", policy => policy.RequireScope("App.read"));
    options.AddPolicy("App.Write", policy => policy.RequireScope("App.write"));
});

// Swagger + OAuth2
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "dBanking.CMS API", Version = "v1" });

    var tenantId = builder.Configuration["AzureAd:TenantId"];
    var instance = builder.Configuration["AzureAd:Instance"]; // https://login.microsoftonline.com/
    var authUrl = $"{instance}{tenantId}/oauth2/v2.0/authorize";
    var tokenUrl = $"{instance}{tenantId}/oauth2/v2.0/token";

    var apiClientId = builder.Configuration["AzureAd:ClientId"];

    c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri(authUrl),
                TokenUrl = new Uri(tokenUrl),
                Scopes = new Dictionary<string, string>
                {
                    [$"api://{apiClientId}/App.read"] = "Read access",
                    [$"api://{apiClientId}/App.write"] = "Write access"
                }
            }
        },
        Description = "OAuth2 Authorization Code Flow with PKCE (Azure AD / Entra ID)"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" }
            },
            new List<string>
            {
                $"api://{apiClientId}/App.read",
                $"api://{apiClientId}/App.write"
            }
        }
    });
});

IdentityModelEventSource.ShowPII = false;

// ---------- RabbitMQ + MassTransit ----------
var mqHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? configuration["Messaging:RabbitMq:Host"] ?? "op1-rabbitmq";
var mqPort = int.Parse(Environment.GetEnvironmentVariable("RABBITMQ_PORT") ?? configuration["Messaging:RabbitMq:Port"] ?? "5672");
var mqVh = Environment.GetEnvironmentVariable("RABBITMQ_VHOST") ?? configuration["Messaging:RabbitMq:VirtualHost"] ?? "/";
var mqUser = Environment.GetEnvironmentVariable("RABBITMQ_USERNAME") ?? configuration["Messaging:RabbitMq:Username"] ?? "guest";
var mqPass = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? configuration["Messaging:RabbitMq:Password"] ?? "guest";
var mqTls = bool.TryParse(Environment.GetEnvironmentVariable("RABBITMQ_USE_TLS"), out var useTlsEnv)
             ? useTlsEnv
             : bool.TryParse(configuration["Messaging:RabbitMq:UseTls"], out var useTlsCfg) && useTlsCfg;

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<KycStatusChangedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(mqHost, mqVh, h =>
        {
            h.Username(mqUser);
            h.Password(mqPass);

            if (mqTls)
            {
                h.UseSsl(s => { s.Protocol = System.Security.Authentication.SslProtocols.Tls12; });
            }
        });

        cfg.UseInMemoryOutbox();

        cfg.Message<Core.Messages.CustomerCreated>(mt => mt.SetEntityName("event.customer.created"));
        cfg.Message<Core.Messages.KycStatusChanged>(mt => mt.SetEntityName("event.kyc.status"));

        cfg.Publish<Core.Messages.CustomerCreated>(p => { p.ExchangeType = "topic"; p.Durable = true; });
        cfg.Publish<Core.Messages.KycStatusChanged>(p => { p.ExchangeType = "topic"; p.Durable = true; });

        cfg.ReceiveEndpoint("kyc-status-changed-processor", e =>
        {
            e.PrefetchCount = 16;
            e.ConfigureConsumer<KycStatusChangedConsumer>(context);
            e.UseMessageRetry(r => r.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5)));
        });

        // Fan-in queue for diagnostics (okay for dev)
        cfg.ReceiveEndpoint("customer-events-queue", e =>
        {
            e.ConfigureConsumeTopology = false;
            e.Bind("event.customer.created", x =>
            {
                x.ExchangeType = "topic";
                x.RoutingKey = "#";
                x.Durable = true;
            });

            e.Handler<Core.Messages.CustomerCreated>(ctx =>
            {
                Console.WriteLine($"[CustomerCreated] {ctx.Message.CustomerId}");
                return Task.CompletedTask;
            });
        });
    });
});

builder.Services.AddOptions<MassTransitHostOptions>().Configure(options =>
{
    options.WaitUntilStarted = true;
    options.StartTimeout = TimeSpan.FromSeconds(30);
});

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// Controllers & AutoMapper & FluentValidation
builder.Services.AddControllers();
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<CustomerMappingProfile>();
    cfg.AddProfile<KycMappingProfile>();
});
builder.Services.AddFluentValidationAutoValidation();

// DataProtection (dev note: runs as root in container to avoid volume perm issues)
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/keys"))
    .SetApplicationName("op1-cust-onboarding-api");

// ---------- Postgres ----------
var pgHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "op1-postgres";
var pgPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
var pgDb = Environment.GetEnvironmentVariable("DB_NAME") ?? "dBanking_CMS";
var pgUser = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
var pgPass = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "postgres";
var pgDetail = Environment.GetEnvironmentVariable("PG_INCLUDE_ERROR_DETAIL") == "true" ? "true" : "false";

var connString = $"Host={pgHost};Port={pgPort};Database={pgDb};Username={pgUser};Password={pgPass};Include Error Detail={pgDetail}";

builder.Services.AddDbContext<AppPostgresDbContext>(options =>
    options.UseNpgsql(connString));


builder.Services.AddHealthChecks()
    .AddCheck("postgres", () =>
    {
        try
        {
            using var conn = new NpgsqlConnection(connString);
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT 1;", conn);
            var _ = cmd.ExecuteScalar();
            return HealthCheckResult.Healthy("Postgres is reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Postgres health check failed.", ex);
        }
    });

// .AddRabbitMQ($"amqp://{mqUser}:{mqPass}@{mqHost}:{mqPort}{mqVh}", name: "rabbitmq");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "dBanking.CMS API v1");
        c.OAuthClientId(builder.Configuration["Swagger:ClientId"]);
        c.OAuthScopes(builder.Configuration.GetSection("Swagger:Scopes").Get<string[]>() ?? Array.Empty<string>());
        c.OAuthUsePkce();
        c.OAuth2RedirectUrl(builder.Configuration["Swagger:RedirectUri"]);
    });
}

// ---- Dev-only EF Core auto-migrations (APPLY_MIGRATIONS=true) ----
var applyMigrationsEnv = Environment.GetEnvironmentVariable("APPLY_MIGRATIONS");
var applyMigrations = string.Equals(applyMigrationsEnv, "true", StringComparison.OrdinalIgnoreCase);

if (applyMigrations && app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
                                      .CreateLogger("EFMigrations");
    var db = scope.ServiceProvider.GetRequiredService<Infrastructure.DbContext.AppPostgresDbContext>();

    // Basic retry (no Polly dependency) - 10 attempts, 3s apart
    const int maxAttempts = 10;
    var attempt = 0;

    while (true)
    {
        try
        {
            attempt++;
            logger.LogInformation("EF Migration: attempt {Attempt}/{MaxAttempts} - checking pending migrations...", attempt, maxAttempts);

            // Ensure DB exists (CreateDatabaseIfNotExists pattern)
            // For Npgsql, EnsureCreated is not recommended with Migrations; use Migrate directly.
            // We'll just open a connection to fail fast if DB is unavailable.
            await db.Database.OpenConnectionAsync();
            await db.Database.CloseConnectionAsync();

            var pending = await db.Database.GetPendingMigrationsAsync();
            if (pending.Any())
            {
                logger.LogInformation("EF Migration: {Count} pending migration(s) found: {Migrations}", pending.Count(), string.Join(", ", pending));
                await db.Database.MigrateAsync();
                logger.LogInformation("EF Migration: completed successfully.");
            }
            else
            {
                logger.LogInformation("EF Migration: no pending migrations.");
            }
            break; // success
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "EF Migration: attempt {Attempt} failed.", attempt);
            if (attempt >= maxAttempts)
            {
                // For dev, we’ll log and continue to let the app run.
                // You can change this to rethrow if you want hard failure.
                logger.LogError(ex, "EF Migration: all attempts exhausted. Continuing without applying migrations.");
                break;
            }
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}

app.UseExceptionHandellingMW();
app.UseMiddleware<CorrelationIdMiddleware>();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map /health so compose healthcheck passes
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }
