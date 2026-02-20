using System.Security.Claims;
using System.Text;
using BankingApi.Data;
using BankingApi.Infrastructure;
using BankingApi.Models;
using BankingApi.Services;
using BankingApi.Services.Admin;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey))
{
    throw new InvalidOperationException("JWT signing key is not configured. Set Jwt:SigningKey in configuration.");
}

builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2),
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthPolicies.IsAdmin, policy => policy.RequireClaim(AppClaimTypes.IsAdmin, "true"));
    options.AddPolicy(AuthPolicies.Staff, policy => policy.RequireClaim(ClaimTypes.Role, nameof(Role.Staff)));
    options.AddPolicy(AuthPolicies.Customer, policy => policy.RequireClaim(ClaimTypes.Role, nameof(Role.Customer)));
});

builder.Services.AddDbContext<TenantDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))
);
builder.Services.AddDbContext<AdminDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IBankAccessor, BankAccessor>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<ICustomersService, CustomersService>();
builder.Services.AddScoped<IAccountsService, AccountsService>();
builder.Services.AddScoped<ITransactionsService, TransactionsService>();

builder.Services.AddScoped<IAdminBanksService, AdminBanksService>();
builder.Services.AddScoped<IAdminCustomersService, AdminCustomersService>();
builder.Services.AddScoped<IAdminAccountsService, AdminAccountsService>();
builder.Services.AddScoped<IAdminTransactionsService, AdminTransactionsService>();

builder.Services.AddControllers();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer(new BearerSecuritySchemeTransformer());
    options.AddOperationTransformer(new AuthorizeOperationTransformer());
});

builder.Services.AddHttpClient<DocRenderer>();
builder.Services.AddSingleton<DocRenderer>();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
    await db.Database.MigrateAsync();
}

await SeedData.EnsureSeededAsync(app.Services);

var docRenderer = app.Services.GetRequiredService<DocRenderer>();
await docRenderer.WarmupAsync("README.md", "ARCHITECTURE.md");

app.Use(
    async (context, next) =>
    {
        try
        {
            await next();
        }
        catch (UnauthorizedAccessException ex)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(
                new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Forbidden",
                    Detail = ex.Message,
                }
            );
        }
        catch (InvalidOperationException ex)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(
                new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Bad Request",
                    Detail = ex.Message,
                }
            );
        }
    }
);

app.MapGet(
        "/",
        () =>
            Results.Content(
                """
                <!DOCTYPE html>
                <html lang="en">
                <head>
                  <meta charset="UTF-8" />
                  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
                  <title>Banking API — Multi-Tenant EF Core Demo</title>
                  <style>
                    *, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }
                    body {
                      font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
                      background: #0f172a;
                      color: #e2e8f0;
                      min-height: 100dvh;
                      display: flex;
                      align-items: center;
                      justify-content: center;
                      padding: 2rem;
                    }
                    .card {
                      background: #1e293b;
                      border: 1px solid #334155;
                      border-radius: 1rem;
                      max-width: 560px;
                      width: 100%;
                      padding: 2.5rem;
                    }
                    .badge {
                      display: inline-block;
                      background: #0ea5e9;
                      color: #fff;
                      font-size: 0.7rem;
                      font-weight: 700;
                      letter-spacing: .08em;
                      text-transform: uppercase;
                      padding: .2rem .55rem;
                      border-radius: .3rem;
                      margin-bottom: 1rem;
                    }
                    h1 { font-size: 1.6rem; font-weight: 700; line-height: 1.25; margin-bottom: .75rem; }
                    p  { color: #94a3b8; line-height: 1.65; margin-bottom: 1.5rem; }
                    .links { display: flex; flex-direction: column; gap: .75rem; }
                    a.btn {
                      display: flex;
                      align-items: center;
                      gap: .75rem;
                      background: #0f172a;
                      border: 1px solid #334155;
                      border-radius: .6rem;
                      padding: .85rem 1.1rem;
                      color: #e2e8f0;
                      text-decoration: none;
                      font-size: .95rem;
                      transition: border-color .15s, background .15s;
                    }
                    a.btn:hover { border-color: #0ea5e9; background: #1e3a52; }
                    a.btn .icon { font-size: 1.3rem; flex-shrink: 0; }
                    a.btn .text { display: flex; flex-direction: column; }
                    a.btn .text strong { font-weight: 600; }
                    a.btn .text span { font-size: .78rem; color: #64748b; }
                    .divider { border: none; border-top: 1px solid #334155; margin: 1.75rem 0; }
                    .stack { font-size: .78rem; color: #475569; text-align: center; }
                    .stack b { color: #64748b; }
                  </style>
                </head>
                <body>
                  <div class="card">
                    <div class="badge">Demo project</div>
                    <h1>Multi-Tenant Banking API</h1>
                    <p>
                      A .NET 10 reference implementation of multi-tenancy using
                      <strong>EF Core</strong> with dual <code>DbContext</code> — one
                      shared admin context and one tenant-scoped context resolved per request
                      from the JWT bearer token.
                    </p>
                    <div class="links">
                       <a class="btn" href="/scalar/v1">
                        <span class="icon">⚡</span>
                        <span class="text">
                          <strong>Scalar API Explorer</strong>
                          <span>Interactive docs — try every endpoint in the browser</span>
                        </span>
                      </a>
                      <a class="btn" href="/openapi/v1.json">
                        <span class="icon">📄</span>
                        <span class="text">
                          <strong>OpenAPI JSON</strong>
                          <span>Raw OpenAPI 3.1 spec for tooling and code generation</span>
                        </span>
                      </a>
                      <a class="btn" href="/docs/readme">
                        <span class="icon">📖</span>
                        <span class="text">
                          <strong>README</strong>
                          <span>Overview, concepts, and quick-start guide</span>
                        </span>
                      </a>
                      <a class="btn" href="/docs/architecture">
                        <span class="icon">🏛</span>
                        <span class="text">
                          <strong>Architecture</strong>
                          <span>Design patterns and trade-offs</span>
                        </span>
                      </a>
                      <a class="btn" href="https://github.com/SjoenH/efcore-multitenant-dual-dbcontext-demo" target="_blank" rel="noopener">
                        <span class="icon">🐙</span>
                        <span class="text">
                          <strong>Source code on GitHub</strong>
                          <span>SjoenH/efcore-multitenant-dual-dbcontext-demo</span>
                        </span>
                      </a>
                    </div>
                    <hr class="divider" />
                    <p class="stack">Built with <b>.NET 10</b> · <b>EF Core 10</b> · <b>SQLite</b> · deployed on <b>Fly.io</b></p>
                  </div>
                </body>
                </html>
                """,
                "text/html"
            )
    )
    .AllowAnonymous();

app.MapGet(
        "/docs/readme",
        (DocRenderer docs) => Results.Content(DocPage("README", docs.GetHtml("README.md")), "text/html")
    )
    .AllowAnonymous();
app.MapGet(
        "/docs/architecture",
        (DocRenderer docs) =>
            Results.Content(DocPage("Architecture & Design Patterns", docs.GetHtml("ARCHITECTURE.md")), "text/html")
    )
    .AllowAnonymous();

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.Title = "Banking API";
    options.AddPreferredSecuritySchemes("Bearer").EnablePersistentAuthentication();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static string DocPage(string title, string bodyHtml) =>
    $$"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="UTF-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1.0" />
          <title>{{title}} — Banking API</title>
          <style>
            *, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }
            html { background: #f5f5f5; }
            body {
              font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
              background: #fff;
              color: #222;
              padding: 3rem 2rem;
              line-height: 1.7;
              max-width: 800px;
              margin: 2rem auto;
              box-shadow: 0 1px 3px rgba(0,0,0,.08);
              min-height: calc(100vh - 4rem);
            }
            .back {
              display: inline-flex; align-items: center; gap: .4rem;
              color: #666; text-decoration: none; font-size: .85rem; margin-bottom: 2rem;
            }
            .back:hover { color: #0066cc; }
            .prose h1 { font-size: 1.75rem; font-weight: 600; margin-bottom: 1.25rem; color: #111; }
            .prose h2 { font-size: 1.3rem; font-weight: 600; margin: 2rem 0 .6rem; color: #111; border-bottom: 1px solid #ddd; padding-bottom: .3rem; }
            .prose h3 { font-size: 1.1rem; font-weight: 600; margin: 1.5rem 0 .4rem; color: #333; }
            .prose h4 { font-size: .95rem; font-weight: 600; margin: 1.25rem 0 .3rem; color: #444; }
            .prose p  { margin-bottom: .85rem; color: #333; }
            .prose a  { color: #0066cc; text-decoration: none; }
            .prose a:hover { text-decoration: underline; }
            .prose ul, .prose ol { padding-left: 1.5rem; margin-bottom: .85rem; color: #333; }
            .prose li { margin-bottom: .25rem; }
            .prose pre {
              background: #f8f8f8; border: 1px solid #e0e0e0; border-radius: 4px;
              padding: .85rem 1rem; overflow-x: auto; margin-bottom: 1rem;
              font-size: .8rem;
            }
            .prose code {
              font-family: "JetBrains Mono", "Fira Code", Consolas, monospace;
              font-size: .85em;
            }
            .prose p > code, .prose li > code {
              background: #f0f0f0; border-radius: 3px;
              padding: .1em .35em; color: #c7254e;
            }
            .prose blockquote {
              border-left: 3px solid #ccc; padding-left: 1rem; color: #666;
              margin-bottom: .85rem;
            }
            .prose table { border-collapse: collapse; width: 100%; margin-bottom: 1rem; font-size: .9rem; }
            .prose th { background: #f5f5f5; font-weight: 600; text-align: left; padding: .5rem .6rem; border: 1px solid #ddd; }
            .prose td { padding: .5rem .6rem; border: 1px solid #ddd; }
            .prose tr:nth-child(even) td { background: #fafafa; }
            .prose hr { border: none; border-top: 1px solid #ddd; margin: 1.5rem 0; }
            .mermaid-diagram {
              background: #fafafa; border: 1px solid #e0e0e0; border-radius: 4px;
              padding: 1.25rem; margin-bottom: 1rem; overflow-x: auto; text-align: center;
            }
            .mermaid-diagram svg { max-width: 100%; height: auto; }
            .mermaid-fallback { background: #fff5f5; border: 1px solid #fcc; border-radius: 4px; padding: .85rem; margin-bottom: 1rem; }
            .mermaid-error { color: #c00; font-size: .8rem; margin-top: .4rem; }
          </style>
        </head>
        <body>
          <a class="back" href="/">← Back</a>
          <div class="prose">
            {{bodyHtml}}
          </div>
        </body>
        </html>
        """;

internal sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken
    )
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "JWT Authorization header using the Bearer scheme. Enter your token below.",
        };

        return Task.CompletedTask;
    }
}

/// <summary>
/// Adds Bearer security requirement to operations that have [Authorize] and removes it from
/// operations that have [AllowAnonymous], so the Scalar UI shows the lock icon correctly.
/// </summary>
internal sealed class AuthorizeOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken
    )
    {
        var hasAllowAnonymous = context.Description.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any();

        if (hasAllowAnonymous)
        {
            return Task.CompletedTask;
        }

        var hasAuthorize = context.Description.ActionDescriptor.EndpointMetadata.OfType<IAuthorizeData>().Any();

        if (!hasAuthorize)
        {
            return Task.CompletedTask;
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer")] = [] });

        return Task.CompletedTask;
    }
}
