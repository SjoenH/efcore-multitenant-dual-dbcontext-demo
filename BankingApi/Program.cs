using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using BankingApi.Data;
using BankingApi.Infrastructure;
using BankingApi.Models;
using BankingApi.Services;
using BankingApi.Services.Admin;
using Markdig;
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

var docPipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
var mermaidCodeBlockRegex = new Regex(
    "<pre><code class=\\\"language-mermaid\\\">([\\s\\S]*?)</code></pre>",
    RegexOptions.Compiled
);

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
    await db.Database.MigrateAsync();
}

await SeedData.EnsureSeededAsync(app.Services);

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
                      <a class="btn" href="/docs/readme">
                        <span class="icon">📚</span>
                        <span class="text">
                          <strong>README</strong>
                          <span>Repository overview</span>
                        </span>
                      </a>
                      <a class="btn" href="/docs/architecture">
                        <span class="icon">🧭</span>
                        <span class="text">
                          <strong>Architecture</strong>
                          <span>Patterns and system diagrams</span>
                        </span>
                      </a>
                      <a class="btn" href="/docs/tutorial">
                        <span class="icon">🧪</span>
                        <span class="text">
                          <strong>Tutorial</strong>
                          <span>Step-by-step walkthrough</span>
                        </span>
                      </a>
                      <a class="btn" href="/docs/quiz">
                        <span class="icon">✅</span>
                        <span class="text">
                          <strong>Concept Quiz</strong>
                          <span>Check your understanding</span>
                        </span>
                      </a>
                      <a class="btn" href="/openapi/v1.json">
                        <span class="icon">📄</span>
                        <span class="text">
                          <strong>OpenAPI JSON</strong>
                          <span>Raw OpenAPI 3.1 spec for tooling and code generation</span>
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
        "/docs/architecture",
        () => Results.Content(DocPage("Root Architecture", RenderDoc("../ARCHITECTURE.md")), "text/html")
    )
    .AllowAnonymous();

app.MapGet("/docs", () => Results.Redirect("/docs/readme")).AllowAnonymous();

app.MapGet("/docs/readme", () => Results.Content(DocPage("README", RenderDoc("../README.md")), "text/html"))
    .AllowAnonymous();

app.MapGet("/docs/tutorial", () => Results.Content(DocPage("Tutorial", RenderDoc("../TUTORIAL.md")), "text/html"))
    .AllowAnonymous();

app.MapGet("/docs/quiz", () => Results.Content(DocPage("Concept Quiz", QuizPage()), "text/html")).AllowAnonymous();

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

string RenderDoc(string fileName)
{
    var path = Path.Combine(app.Environment.ContentRootPath, fileName);
    if (!File.Exists(path))
    {
        return "<h1>Document not found</h1>";
    }

    var markdown = File.ReadAllText(path);
    var html = Markdown.ToHtml(markdown, docPipeline);
    html = mermaidCodeBlockRegex.Replace(html, match => $"<div class=\"mermaid\">{match.Groups[1].Value}</div>");
    return html;
}

string DocPage(string title, string bodyHtml) =>
    $$"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="UTF-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1.0" />
          <title>{{title}} — Banking API</title>
          <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/highlight.js@11.9.0/styles/github.min.css" />
          <script defer src="https://cdn.jsdelivr.net/npm/highlight.js@11.9.0/lib/highlight.min.js"></script>
          <script defer src="https://cdn.jsdelivr.net/npm/mermaid@10/dist/mermaid.min.js"></script>
          <style>
            *, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }
            html { background: #f5f5f5; }
            body {
              font-family: "IBM Plex Sans", "Segoe UI", system-ui, sans-serif;
              background: #fff;
              color: #222;
              padding: 3rem 2rem;
              line-height: 1.7;
              max-width: 900px;
              margin: 2rem auto;
              box-shadow: 0 8px 24px rgba(0,0,0,.08);
              min-height: calc(100vh - 4rem);
            }
            .back {
              display: inline-flex; align-items: center; gap: .4rem;
              color: #555; text-decoration: none; font-size: .85rem; margin-bottom: 2rem;
            }
            .back:hover { color: #0066cc; }
            .prose h1 { font-size: 1.9rem; font-weight: 600; margin-bottom: 1.25rem; color: #111; }
            .prose h2 { font-size: 1.35rem; font-weight: 600; margin: 2rem 0 .6rem; color: #111; border-bottom: 1px solid #ddd; padding-bottom: .3rem; }
            .prose h3 { font-size: 1.1rem; font-weight: 600; margin: 1.5rem 0 .4rem; color: #333; }
            .prose h4 { font-size: .95rem; font-weight: 600; margin: 1.25rem 0 .3rem; color: #444; }
            .prose p  { margin-bottom: .85rem; color: #333; }
            .prose a  { color: #0066cc; text-decoration: none; }
            .prose a:hover { text-decoration: underline; }
            .prose ul, .prose ol { padding-left: 1.5rem; margin-bottom: .85rem; color: #333; }
            .prose li { margin-bottom: .25rem; }
            .prose pre {
              background: #f8f8f8; border: 1px solid #e0e0e0; border-radius: 6px;
              padding: .85rem 1rem; overflow-x: auto; margin-bottom: 1rem;
              font-size: .85rem;
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
            .mermaid { margin: 1.5rem 0; }
            .quiz { display: grid; gap: 1.25rem; margin-top: 1.5rem; }
            .quiz-question { border: 1px solid #e0e0e0; border-radius: 10px; padding: 1rem 1.2rem; }
            .quiz-question legend { font-weight: 600; margin-bottom: .75rem; }
            .quiz-question label { display: flex; gap: .6rem; align-items: flex-start; margin-bottom: .4rem; cursor: pointer; }
            .quiz-question input { margin-top: .25rem; }
            .quiz-question.is-correct { border-color: #86efac; background: #f0fdf4; }
            .quiz-question.is-incorrect { border-color: #fecaca; background: #fef2f2; }
            .quiz-explanation { margin-top: .5rem; color: #475569; font-size: .9rem; }
            .quiz-actions { display: flex; flex-wrap: wrap; gap: .75rem; align-items: center; margin-top: 1.5rem; }
            .quiz-actions button {
              border: 1px solid #cbd5f5; background: #1e293b; color: #fff;
              padding: .55rem 1rem; border-radius: .6rem; cursor: pointer; font-weight: 600;
            }
            .quiz-actions button:hover { background: #334155; }
            .quiz-result { font-weight: 600; color: #0f172a; }
          </style>
        </head>
        <body>
          <a class="back" href="/">← Back</a>
          <div class="prose">
            {{bodyHtml}}
          </div>
          <script>
            window.addEventListener("load", () => {
              if (window.hljs) {
                document.querySelectorAll("pre code").forEach((block) => {
                  window.hljs.highlightElement(block);
                });
              }
              if (window.mermaid) {
                window.mermaid.initialize({ startOnLoad: false });
                window.mermaid.run({ nodes: document.querySelectorAll(".mermaid") });
              }
            });
          </script>
        </body>
        </html>
        """;

string QuizPage() =>
    """
        <h1>Concept Quiz</h1>
        <p>Answer the questions below to check your understanding of the architecture and multi-tenancy concepts.</p>
        <form id="quiz" class="quiz"></form>
        <div class="quiz-actions">
          <button id="quiz-submit" type="button">Check answers</button>
          <button id="quiz-reset" type="button">Reset</button>
          <p id="quiz-result" class="quiz-result" aria-live="polite"></p>
        </div>
        <script>
          const questions = [
            {
              text: "What is the purpose of the TenantDbContext?",
              options: [
                "Apply tenant-specific query filters",
                "Manage migrations only",
                "Store configuration settings",
                "Run background jobs"
              ],
              answer: 0,
              explanation: "TenantDbContext applies global query filters so each query is scoped to the current tenant."
            },
            {
              text: "Which pattern keeps layers depending only on the layer below?",
              options: [
                "Layered architecture",
                "Event sourcing",
                "Saga",
                "Actor model"
              ],
              answer: 0,
              explanation: "Layered architecture organizes the app into horizontal slices with clear dependencies."
            },
            {
              text: "What does AdminDbContext provide compared to TenantDbContext?",
              options: [
                "Unfiltered access across all tenants",
                "A separate database per tenant",
                "Automatic encryption",
                "Request routing"
              ],
              answer: 0,
              explanation: "AdminDbContext omits query filters, so it can see all tenant rows."
            },
            {
              text: "Why use EF Core global query filters for tenant isolation?",
              options: [
                "To enforce tenant scoping on every query",
                "To speed up migrations",
                "To cache responses",
                "To remove the need for DTOs"
              ],
              answer: 0,
              explanation: "Global filters ensure every query is scoped without manual where clauses."
            },
            {
              text: "What is the role of the Options pattern here?",
              options: [
                "Bind configuration into strongly typed objects",
                "Generate SQL migrations",
                "Implement authorization policies",
                "Handle request routing"
              ],
              answer: 0,
              explanation: "Options bind configuration sections into POCOs like JwtOptions."
            }
          ];

          const quiz = document.getElementById("quiz");
          const result = document.getElementById("quiz-result");

          function renderQuiz() {
            quiz.innerHTML = "";
            questions.forEach((q, idx) => {
              const block = document.createElement("fieldset");
              block.className = "quiz-question";
              block.innerHTML = `
                <legend>${idx + 1}. ${q.text}</legend>
                ${q.options.map((opt, oidx) => `
                  <label>
                    <input type="radio" name="q${idx}" value="${oidx}" />
                    <span>${opt}</span>
                  </label>
                `).join("")}
                <p class="quiz-explanation" hidden></p>
              `;
              quiz.appendChild(block);
            });
          }

          function checkAnswers() {
            let score = 0;
            quiz.querySelectorAll(".quiz-question").forEach((block, idx) => {
              const selected = block.querySelector("input[type=radio]:checked");
              const explanation = block.querySelector(".quiz-explanation");
              if (!selected) {
                explanation.textContent = "Choose an answer to see the explanation.";
                explanation.hidden = false;
                return;
              }
              const correct = Number(selected.value) === questions[idx].answer;
              if (correct) score += 1;
              explanation.textContent = questions[idx].explanation;
              explanation.hidden = false;
              block.classList.toggle("is-correct", correct);
              block.classList.toggle("is-incorrect", !correct);
            });
            result.textContent = `Score: ${score}/${questions.length}`;
          }

          function resetQuiz() {
            renderQuiz();
            result.textContent = "";
          }

          document.getElementById("quiz-submit").addEventListener("click", checkAnswers);
          document.getElementById("quiz-reset").addEventListener("click", resetQuiz);

          renderQuiz();
        </script>
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
