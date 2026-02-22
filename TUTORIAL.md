# Multi-Tenant EF Core Tutorial: A Step-by-Step Guide

> **Audience:** Junior developers new to .NET, Entity Framework Core, and multi-tenancy.
>
> This tutorial walks through the Banking API demo in this repository. By the end, you will
> understand how a single database serves multiple tenants (banks) while keeping their data
> completely isolated — using nothing more than JWT claims and EF Core query filters.

---

## Table of Contents

1. [What Is Multi-Tenancy?](#1-what-is-multi-tenancy)
2. [How This Project Is Structured](#2-how-this-project-is-structured)
3. [Key Concepts You Need to Know](#3-key-concepts-you-need-to-know)
   - [Dependency Injection (DI)](#31-dependency-injection-di)
   - [DbContext & Entity Framework Core](#32-dbcontext--entity-framework-core)
   - [JWT Authentication](#33-jwt-authentication)
   - [EF Core Query Filters](#34-ef-core-query-filters)
4. [The Data Model](#4-the-data-model)
5. [Step-by-Step: How Multi-Tenancy Works](#5-step-by-step-how-multi-tenancy-works)
   - [Step 1 — Define Your Entities with a Tenant Key](#step-1--define-your-entities-with-a-tenant-key)
   - [Step 2 — Create a Shared Base DbContext](#step-2--create-a-shared-base-dbcontext)
   - [Step 3 — Create the Tenant-Scoped DbContext](#step-3--create-the-tenant-scoped-dbcontext)
   - [Step 4 — Create the Admin (Unfiltered) DbContext](#step-4--create-the-admin-unfiltered-dbcontext)
   - [Step 5 — Resolve the Tenant from the JWT](#step-5--resolve-the-tenant-from-the-jwt)
   - [Step 6 — Wire Everything Up in Dependency Injection](#step-6--wire-everything-up-in-dependency-injection)
   - [Step 7 — Use the Correct DbContext in Services](#step-7--use-the-correct-dbcontext-in-services)
6. [Full Request Lifecycle](#6-full-request-lifecycle)
7. [Authorization & Roles](#7-authorization--roles)
8. [Running the Demo](#8-running-the-demo)
9. [Common Pitfalls & FAQ](#9-common-pitfalls--faq)

---

## 1. What Is Multi-Tenancy?

Imagine you are building a banking API used by **multiple banks**. Each bank (a "tenant") has its
own customers, accounts, and transactions. Bank A must **never** see Bank B's data.

There are three common strategies:

| Strategy | Description |
|---|---|
| **Separate databases** | Each tenant gets its own database |
| **Separate schemas** | Tenants share a database but use different schemas |
| **Shared database with row-level filtering** ✅ | All tenants share one database; a column like `BankId` identifies which rows belong to which tenant |

This project uses the **third strategy** — a single SQLite database with a `BankId` column on every
tenant-scoped table. EF Core automatically filters rows so each tenant only sees its own data.

```mermaid
flowchart LR
    subgraph "Single Database"
        direction TB
        C1["Customer: Ola (BankId = A)"]
        C2["Customer: Anna (BankId = B)"]
    end

    Staff_A["Staff at Bank A"] -->|"Sees only Ola"| C1
    Staff_B["Staff at Bank B"] -->|"Sees only Anna"| C2
```

---

## 2. How This Project Is Structured

```
BankingApi/
├── Controllers/          # HTTP endpoints (receive requests)
│   ├── Admin/            # Admin-only endpoints (cross-tenant)
│   ├── AuthController    # Login (no auth required)
│   ├── CustomersController
│   ├── AccountsController
│   └── TransactionsController
├── Data/                 # Database contexts
│   ├── AppDbContextBase  # Shared table/column configuration
│   ├── TenantDbContext   # Filtered — sees only current tenant's data
│   └── AdminDbContext    # Unfiltered — sees everything
├── Models/               # Database entities (C# classes → database tables)
├── Dtos/                 # Data Transfer Objects (what the API sends/receives)
├── Services/             # Business logic (sits between controllers and database)
│   └── Admin/            # Admin-specific services
├── Infrastructure/       # Cross-cutting concerns (auth, JWT, tenant resolution)
└── Program.cs            # Application entry point & DI configuration
```

Each layer has a single job:

```mermaid
flowchart TD
    A[Controller] -->|"Receives HTTP request,<br/>validates input"| B[Service]
    B -->|"Contains business logic,<br/>calls database"| C[DbContext]
    C -->|"Translates C# to SQL,<br/>applies filters"| D[(Database)]
```

---

## 3. Key Concepts You Need to Know

### 3.1 Dependency Injection (DI)

**What it is:** Instead of creating objects yourself with `new`, you tell the framework _what you
need_ and it gives it to you. Think of it as a waiter at a restaurant — you don't go to the kitchen
yourself; you say "I need a coffee" and the waiter brings it.

**Why it matters:** It lets you swap implementations without changing your code. For example, a
controller doesn't care _how_ a `CustomersService` is built — it just declares that it needs one.

**How it works in this project:**

In `Program.cs`, we register services into a "container":

```csharp
// Tell DI: "When someone asks for IBankAccessor, create a BankAccessor"
builder.Services.AddScoped<IBankAccessor, BankAccessor>();

// Tell DI: "When someone asks for ICustomersService, create a CustomersService"
builder.Services.AddScoped<ICustomersService, CustomersService>();
```

Then in a controller, we simply _declare_ what we need in the constructor:

```csharp
public sealed class CustomersController : ControllerBase
{
    private readonly ICustomersService _customers;

    // DI automatically provides the ICustomersService here — no "new" needed!
    public CustomersController(ICustomersService customers)
    {
        _customers = customers;
    }
}
```

```mermaid
flowchart LR
    subgraph "DI Container (Program.cs)"
        R1["IBankAccessor → BankAccessor"]
        R2["ICustomersService → CustomersService"]
        R3["TenantDbContext"]
    end

    subgraph "When a request arrives..."
        CC["CustomersController needs ICustomersService"]
        CS["CustomersService needs TenantDbContext"]
        TD["TenantDbContext needs IBankAccessor"]
    end

    R2 -.->|"DI creates"| CC
    R3 -.->|"DI creates"| CS
    R1 -.->|"DI creates"| TD
```

> **Key point:** `AddScoped` means "create one instance per HTTP request." Every class that asks for
> `TenantDbContext` during the same request gets the **same** instance — with the **same** `BankId`.

### 3.2 DbContext & Entity Framework Core

**What is EF Core?** It's an Object-Relational Mapper (ORM). It lets you work with database rows as
C# objects instead of writing raw SQL.

**What is a DbContext?** It's your "gateway" to the database. You use it to query, insert, update,
and delete data. Think of it as a session with the database.

```csharp
// Without EF Core (raw SQL):
// "SELECT * FROM Customers WHERE BankId = '...'"

// With EF Core:
var customers = await _db.Customers.ToListAsync();
// EF Core translates this to SQL for you!
```

**How entities become tables:**

```csharp
// This C# class...
public sealed class Customer
{
    public Guid Id { get; set; }
    public Guid BankId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
}

// ...becomes this database table:
// ┌──────────────────────────────────────────────┐
// │ Customers                                    │
// ├──────────┬──────────┬──────────┬─────────────┤
// │ Id (PK)  │ BankId   │ Name     │ Email       │
// ├──────────┼──────────┼──────────┼─────────────┤
// │ guid-1   │ bank-a   │ Ola      │ ola@...     │
// │ guid-2   │ bank-b   │ Anna     │ anna@...    │
// └──────────┴──────────┴──────────┴─────────────┘
```

A `DbSet<Customer>` property on the DbContext represents the table:

```csharp
public DbSet<Customer> Customers => Set<Customer>();
// _db.Customers.ToListAsync()  →  SELECT * FROM Customers
```

### 3.3 JWT Authentication

**What is a JWT?** A JSON Web Token is a small, signed piece of data that the server gives to the
client after login. It contains "claims" — key-value pairs that describe the user.

```mermaid
flowchart LR
    subgraph "JWT Token (decoded)"
        direction TB
        H["Header: algorithm, type"]
        P["Payload (claims):<br/>sub: user-id<br/>role: Staff<br/>BankId: bank-a-guid<br/>exp: ..."]
        S["Signature: HMAC-SHA256(header + payload, secret)"]
    end
    H --- P --- S
```

**Why JWTs?** The token is **cryptographically signed** — the server can verify it hasn't been
tampered with. If a user tries to change their `BankId` claim, the signature check will fail and the
request will be rejected.

In this project, the login endpoint issues a JWT containing the user's role and `BankId`:

```csharp
// In JwtTokenService.cs — these claims go into the JWT
var claims = new List<Claim>
{
    new(ClaimTypes.Role, user.Role.ToString()),   // "Staff", "Customer", or "Admin"
};

if (user.BankId is not null)
{
    claims.Add(new Claim(AppClaimTypes.BankId, user.BankId.Value.ToString()));
}
```

When the client makes a request, it sends the JWT in the `Authorization` header:

```
GET /api/customers
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

ASP.NET Core automatically validates the token and puts the claims on `HttpContext.User`.

### 3.4 EF Core Query Filters

**This is the magic that makes multi-tenancy work.**

A query filter is a predicate (a condition) that EF Core **automatically appends** to every query
for a given entity. You define it once, and it applies everywhere.

```csharp
// In TenantDbContext.cs
modelBuilder.Entity<Customer>().HasQueryFilter(x => x.BankId == BankId);
```

This means:
- When you write: `_db.Customers.ToListAsync()`
- EF Core generates: `SELECT * FROM Customers WHERE BankId = @bankId`

**You never have to remember to add `WHERE BankId = ...`** — it happens automatically!

```mermaid
flowchart TD
    A["Your code:<br/>_db.Customers.ToListAsync()"] --> B["EF Core adds filter:<br/>WHERE BankId = 'bank-a'"]
    B --> C["SQL sent to database:<br/>SELECT * FROM Customers<br/>WHERE BankId = 'bank-a'"]
    C --> D["Only Bank A's<br/>customers returned"]
```

---

## 4. The Data Model

Every tenant-scoped entity has a `BankId` foreign key. This is the column that query filters use to
isolate data.

```mermaid
erDiagram
    Bank ||--o{ Customer : "has many"
    Bank ||--o{ Account : "has many"
    Bank ||--o{ Transaction : "has many"
    Customer ||--o{ Account : "owns"
    Account ||--o{ Transaction : "has many"

    Bank {
        Guid Id PK
        string Name
        string Code "Unique: NO-001, SE-001"
    }
    Customer {
        Guid Id PK
        Guid BankId FK "← Tenant key"
        string Name
        string Email
        string Phone
    }
    Account {
        Guid Id PK
        Guid BankId FK "← Tenant key"
        Guid CustomerId FK
        string AccountNumber
        decimal Balance
        string Currency
    }
    Transaction {
        Guid Id PK
        Guid BankId FK "← Tenant key"
        Guid AccountId FK
        decimal Amount
        string Type
        string Description
    }
```

Notice that `Bank` itself does **not** have a `BankId` — it _is_ the tenant. Only entities that
belong _to_ a tenant carry the `BankId` column.

---

## 5. Step-by-Step: How Multi-Tenancy Works

### Step 1 — Define Your Entities with a Tenant Key

Every entity that needs tenant isolation gets a `BankId` property:

```csharp
// Models/Customer.cs
public sealed class Customer
{
    public Guid Id { get; set; }
    public Guid BankId { get; set; }       // ← This is the tenant key
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public List<Account> Accounts { get; set; } = new();
}
```

```csharp
// Models/Account.cs
public sealed class Account
{
    public Guid Id { get; set; }
    public Guid BankId { get; set; }       // ← Tenant key
    public Guid CustomerId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    // ...
}
```

> **Why put `BankId` on _every_ entity?** You could theoretically navigate through relationships
> (Account → Customer → BankId), but having it directly on each entity makes query filters simple
> and efficient — no joins needed.

### Step 2 — Create a Shared Base DbContext

Both contexts need the same tables and column configurations. Instead of duplicating that, we put the
shared setup in an abstract base class:

```csharp
// Data/AppDbContextBase.cs
public abstract class AppDbContextBase : DbContext
{
    protected AppDbContextBase(DbContextOptions options) : base(options) { }

    // These DbSet properties represent database tables
    public DbSet<Bank> Banks => Set<Bank>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure table structure, indexes, relationships
        modelBuilder.Entity<Bank>(b =>
        {
            b.HasIndex(x => x.Code).IsUnique();
            b.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<Customer>(b =>
        {
            b.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            // One customer has many accounts
            b.HasMany(x => x.Accounts)
                .WithOne(x => x.Customer)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ... more entity configurations
    }
}
```

> **What is `OnModelCreating`?** It's a method that EF Core calls when it first builds the internal
> model of your database. This is where you configure indexes, relationships, default values, and
> — crucially — **query filters**.

### Step 3 — Create the Tenant-Scoped DbContext

This is where the multi-tenancy magic happens:

```csharp
// Data/TenantDbContext.cs
public sealed class TenantDbContext : AppDbContextBase
{
    public Guid BankId { get; }

    public TenantDbContext(
        DbContextOptions<TenantDbContext> options,
        IBankAccessor bankAccessor        // ← DI injects this automatically
    ) : base(options)
    {
        BankId = bankAccessor.GetRequiredBankId();  // Read BankId from JWT
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);   // Call parent to get table configs

        // These three lines are the ENTIRE tenant isolation mechanism:
        modelBuilder.Entity<Customer>().HasQueryFilter(x => x.BankId == BankId);
        modelBuilder.Entity<Account>().HasQueryFilter(x => x.BankId == BankId);
        modelBuilder.Entity<Transaction>().HasQueryFilter(x => x.BankId == BankId);
    }
}
```

Let's break down what happens:

```mermaid
sequenceDiagram
    participant R as HTTP Request
    participant DI as DI Container
    participant BA as BankAccessor
    participant JWT as HttpContext.User
    participant TDB as TenantDbContext

    R->>DI: Controller needs TenantDbContext
    DI->>BA: Create BankAccessor
    BA->>JWT: Read AppClaimTypes.BankId claim
    JWT-->>BA: "bank-a-guid"
    BA-->>DI: BankId = bank-a-guid
    DI->>TDB: new TenantDbContext(options, bankAccessor)
    TDB->>TDB: BankId = bank-a-guid
    TDB->>TDB: Register query filters:<br/>WHERE BankId = 'bank-a-guid'
    Note over TDB: Every query on Customer,<br/>Account, Transaction now<br/>automatically filters by BankId
```

### Step 4 — Create the Admin (Unfiltered) DbContext

Admin users need to see data across **all** banks. The admin context inherits the same table
configuration but adds **no** query filters:

```csharp
// Data/AdminDbContext.cs
public sealed class AdminDbContext : AppDbContextBase
{
    public AdminDbContext(DbContextOptions<AdminDbContext> options)
        : base(options) { }

    // No OnModelCreating override = no query filters = sees all data
}
```

This context is also used for **database migrations** (schema changes), because migrations need to
see the full schema without tenant filters.

```mermaid
flowchart LR
    subgraph "TenantDbContext (Staff/Customer)"
        T1["_db.Customers.ToListAsync()"]
        T2["SELECT * FROM Customers<br/>WHERE BankId = 'bank-a'"]
    end

    subgraph "AdminDbContext (Admin)"
        A1["_db.Customers.ToListAsync()"]
        A2["SELECT * FROM Customers<br/>(no filter — returns all)"]
    end

    T1 --> T2
    A1 --> A2
```

### Step 5 — Resolve the Tenant from the JWT

The `BankAccessor` reads the `BankId` from the currently authenticated user's JWT claims:

```csharp
// Infrastructure/BankAccessor.cs

// Interface — defines the contract
public interface IBankAccessor
{
    Guid? TryGetBankId();       // Returns null if no BankId claim
    Guid GetRequiredBankId();   // Throws if no BankId claim
}

// Implementation — reads from HttpContext
public sealed class BankAccessor : IBankAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BankAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? TryGetBankId()
    {
        // HttpContext.User is populated by ASP.NET Core's JWT middleware
        var user = _httpContextAccessor.HttpContext?.User;
        return user?.TryGetBankIdClaim();
    }

    public Guid GetRequiredBankId()
    {
        var bankId = TryGetBankId();
        if (bankId is null)
        {
            throw new UnauthorizedAccessException("Missing BankId claim in token.");
        }
        return bankId.Value;
    }
}
```

The helper method `TryGetBankIdClaim()` is a simple extension that looks up the claim:

```csharp
// Infrastructure/ClaimsExtensions.cs
public static Guid? TryGetBankIdClaim(this ClaimsPrincipal user)
{
    var raw = user.FindFirst(AppClaimTypes.BankId)?.Value;
    return Guid.TryParse(raw, out var id) ? id : null;
}
```

> **Why use an interface (`IBankAccessor`)?** It makes testing easier. In unit tests, you can create
> a fake `IBankAccessor` that returns any `BankId` you want — without needing a real HTTP request.

### Step 6 — Wire Everything Up in Dependency Injection

`Program.cs` is where all the pieces connect:

```csharp
// Program.cs

// 1. Register both DbContexts — same database, different behaviors
builder.Services.AddDbContext<TenantDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))
);
builder.Services.AddDbContext<AdminDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// 2. Register infrastructure services
builder.Services.AddHttpContextAccessor();              // Gives access to HttpContext
builder.Services.AddScoped<IBankAccessor, BankAccessor>(); // Tenant resolver

// 3. Register tenant-scoped services (use TenantDbContext)
builder.Services.AddScoped<ICustomersService, CustomersService>();
builder.Services.AddScoped<IAccountsService, AccountsService>();
builder.Services.AddScoped<ITransactionsService, TransactionsService>();

// 4. Register admin services (use AdminDbContext)
builder.Services.AddScoped<IAdminBanksService, AdminBanksService>();
builder.Services.AddScoped<IAdminCustomersService, AdminCustomersService>();
```

Here's the full dependency chain visualized:

```mermaid
flowchart TD
    subgraph "Registered in DI Container"
        HC["HttpContextAccessor"]
        BA["BankAccessor"]
        TDB["TenantDbContext"]
        ADB["AdminDbContext"]
        CS["CustomersService"]
        ABS["AdminBanksService"]
    end

    subgraph "Dependency Chain (per request)"
        HC -->|"injected into"| BA
        BA -->|"injected into"| TDB
        TDB -->|"injected into"| CS
        ADB -->|"injected into"| ABS
    end

    CS -->|"used by"| CC["CustomersController"]
    ABS -->|"used by"| ABC["AdminBanksController"]
```

### Step 7 — Use the Correct DbContext in Services

**Tenant-scoped services** receive `TenantDbContext` — queries are automatically filtered:

```csharp
// Services/CustomersService.cs
public sealed class CustomersService : ICustomersService
{
    private readonly TenantDbContext _db;

    public CustomersService(TenantDbContext db)  // ← DI provides tenant-filtered context
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CustomerResponse>> GetAll()
    {
        // This query AUTOMATICALLY includes WHERE BankId = <current tenant>
        // You don't write the filter — EF Core adds it for you!
        return await _db.Customers
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(CustomerResponse.Projection)
            .ToListAsync();
    }

    public async Task<CustomerResponse> Create(CustomerRequest request)
    {
        var entity = new Customer
        {
            Id = Guid.NewGuid(),
            BankId = _db.BankId,   // ← Set the tenant key from the context
            CreatedAt = DateTimeOffset.UtcNow,
        };
        entity.ApplyFields(request);

        _db.Customers.Add(entity);
        await _db.SaveChangesAsync();

        return entity.ToResponse();
    }
}
```

**Admin services** receive `AdminDbContext` — no filters, full access:

```csharp
// Services/Admin/AdminBanksService.cs
public sealed class AdminBanksService : IAdminBanksService
{
    private readonly AdminDbContext _db;

    public AdminBanksService(AdminDbContext db)  // ← Unfiltered context
    {
        _db = db;
    }

    public async Task<IReadOnlyList<BankResponse>> GetAll()
    {
        // No query filter — returns ALL banks
        return await _db.Banks
            .AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(BankResponse.Projection)
            .ToListAsync();
    }
}
```

---

## 6. Full Request Lifecycle

Let's trace a complete request from start to finish:

```mermaid
sequenceDiagram
    actor U as Staff User
    participant API as ASP.NET Core
    participant Auth as JWT Middleware
    participant Ctrl as CustomersController
    participant Svc as CustomersService
    participant TDB as TenantDbContext
    participant DB as SQLite

    Note over U: Staff at "Norge Bank" (BankId = AAA)

    U->>API: POST /api/auth/login<br/>{ "email": "staff.norge@demo.com" }
    API-->>U: { "accessToken": "eyJ...", "bankId": "AAA" }

    Note over U: Use the token for subsequent requests

    U->>API: GET /api/customers<br/>Authorization: Bearer eyJ...
    API->>Auth: Validate JWT signature & expiry
    Auth->>Auth: Populate HttpContext.User with claims:<br/>role=Staff, BankId=AAA
    Auth->>Ctrl: Request authorized ✓
    Ctrl->>Svc: GetAll()
    Note over Svc: DI created TenantDbContext<br/>with BankId = AAA
    Svc->>TDB: _db.Customers.ToListAsync()
    TDB->>TDB: Apply query filter:<br/>WHERE BankId = 'AAA'
    TDB->>DB: SELECT * FROM Customers<br/>WHERE BankId = 'AAA'
    DB-->>TDB: [Ola Nordmann]
    TDB-->>Svc: Filtered results
    Svc-->>Ctrl: CustomerResponse[]
    Ctrl-->>U: 200 OK [{ "name": "Ola Nordmann", ... }]

    Note over U: Anna Andersson (Bank B) is NOT returned
```

---

## 7. Authorization & Roles

The project defines three roles, each with different access levels:

| Role | What They Can Do | DbContext Used | JWT Contains |
|---|---|---|---|
| **Admin** | See and manage ALL data across all banks | `AdminDbContext` | `role=Admin`, `IsAdmin=true` |
| **Staff** | CRUD operations scoped to their bank | `TenantDbContext` | `role=Staff`, `BankId` |
| **Customer** | View only their own accounts and transactions | `TenantDbContext` | `role=Customer`, `BankId`, `CustomerId` |

Authorization policies are registered in `Program.cs`. In production code, use constants like
`AuthPolicies.*` and `AppClaimTypes.*` to avoid string typos:

```csharp
builder.Services.AddAuthorization(options =>
{
    // Each policy checks for specific claims in the JWT
    options.AddPolicy(AuthPolicies.IsAdmin, policy =>
        policy.RequireClaim(AppClaimTypes.IsAdmin, "true"));

    options.AddPolicy(AuthPolicies.Staff, policy =>
        policy.RequireClaim(ClaimTypes.Role, nameof(Role.Staff)));

    options.AddPolicy(AuthPolicies.Customer, policy =>
        policy.RequireClaim(ClaimTypes.Role, nameof(Role.Customer)));
});
```

Controllers enforce these policies with the `[Authorize]` attribute:

```csharp
[Authorize(Policy = AuthPolicies.Staff)]   // Only Staff users can access these endpoints
public sealed class CustomersController : ControllerBase { }

[Authorize(Policy = AuthPolicies.IsAdmin)] // Only Admin users can access these endpoints
public sealed class AdminBanksController : ControllerBase { }
```

```mermaid
flowchart TD
    R[Incoming Request] --> AuthN{JWT Valid?}
    AuthN -->|No| U1[401 Unauthorized]
    AuthN -->|Yes| Check{Which role?}

    Check -->|"Admin<br/>(IsAdmin=true)"| Admin[AdminDbContext<br/>No filters]
    Check -->|"Staff<br/>(role=Staff)"| Staff[TenantDbContext<br/>Filtered by BankId]
    Check -->|"Customer<br/>(role=Customer)"| Cust[TenantDbContext<br/>Filtered by BankId<br/>+ ownership checks]
    Check -->|"Wrong role<br/>for endpoint"| U2[403 Forbidden]
```

Customers get an extra layer of protection — the controller checks they can only access their own
data:

```csharp
// In CustomersController.cs — GET /api/customers/{id}/accounts
var isCustomer = HttpContext.User.GetRequiredRole() == nameof(Role.Customer);
if (isCustomer && HttpContext.User.TryGetCustomerIdClaim() != id)
{
    return Forbid();   // 403 — you can only view your own accounts
}
```

---

## 8. Running the Demo

> **Demo-only login flow:** The `/api/auth/login` endpoint in this demo accepts an email only and
> the seeded users list can be enumerated. That is intentionally simplified for learning purposes.
> Do not ship this in production — require proper credentials and prevent user enumeration.

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (or whichever version matches the project)

### Start the API

```bash
dotnet run --project BankingApi/BankingApi.csproj
```

The API will:
1. Apply database migrations automatically (using `AdminDbContext`)
2. Seed demo data (two banks, staff users, customers, accounts)
3. Start listening on `http://localhost:5294`

### Try It Out

**1. See available demo logins:**

```bash
curl http://localhost:5294/api/auth/seeded-logins
```

**2. Login as Bank A Staff:**

```bash
curl -X POST http://localhost:5294/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email": "staff.norge@demo.com"}'
```

Copy the `accessToken` from the response.

**3. List customers (only Bank A's customers are returned):**

```bash
curl http://localhost:5294/api/customers \
  -H "Authorization: Bearer <paste-token-here>"
```

**4. Login as Bank B Staff and compare:**

```bash
curl -X POST http://localhost:5294/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email": "staff.svensk@demo.com"}'
```

Use this token — you'll see **different** customers! Same endpoint, same code, but different data
because the JWT carries a different `BankId`.

### Seeded Demo Data

| Entity | Bank A (Norge, NO-001) | Bank B (Svensk, SE-001) |
|---|---|---|
| Staff login | staff.norge@demo.com | staff.svensk@demo.com |
| Customer login | customer.ola@demo.com | customer.anna@demo.com |
| Customer name | Ola Nordmann | Anna Andersson |

Admin login: `admin@demo.com` (cross-tenant access, no `BankId` in JWT)

---

## 9. Common Pitfalls & FAQ

### "Why two DbContexts instead of just using `.Where(x => x.BankId == ...)`?"

You _could_ add `.Where()` to every query manually, but:
- You'd have to **remember** to add it every time — one mistake leaks data across tenants
- Query filters are **automatic** — they apply to every query, including navigation properties and joins
- The dual context pattern makes the intent clear: tenant code uses `TenantDbContext`, admin code uses `AdminDbContext`

### "What if I forget to set `BankId` when creating an entity?"

The `TenantDbContext` exposes a `BankId` property, which services use when creating entities:

```csharp
var entity = new Customer
{
    BankId = _db.BankId,  // Always use the context's BankId
    // ...
};
```

If you accidentally set the wrong `BankId`, the entity would be invisible to the current tenant
(the query filter would exclude it). This is a safety net — data doesn't leak, it just becomes
orphaned.

### "What does `AddScoped` mean?"

ASP.NET Core has three lifetimes for DI:

| Lifetime | Created | Destroyed | Use For |
|---|---|---|---|
| **Transient** | Every time it's requested | Immediately after use | Lightweight, stateless services |
| **Scoped** | Once per HTTP request | At end of request | DbContext, per-request state |
| **Singleton** | Once for the entire app | When app shuts down | Configuration, caches |

`DbContext` must be **Scoped** because it tracks changes for the duration of one request. If it were
Singleton, all requests would share the same BankId — a serious security bug.

### "How do migrations work with two DbContexts?"

Migrations are generated and applied through the `AdminDbContext` (unfiltered). This is important
because:
- Migrations need to see the full schema (all tables, all columns)
- Query filters would interfere with schema introspection
- The design-time factory (`BankingApiDbContextFactory.cs`) creates an `AdminDbContext` for the
  `dotnet ef` CLI tool

### "Can a tenant bypass the filter?"

EF Core query filters can technically be bypassed with `.IgnoreQueryFilters()`, but:
- Tenant-scoped services only have access to `TenantDbContext`
- The `BankId` on `TenantDbContext` is set in the constructor and is read-only
- The `BankId` comes from a cryptographically signed JWT — users cannot tamper with it

### "What is `AsNoTracking()` and why is it used?"

By default, EF Core "tracks" every entity it loads — it watches for changes so it can generate
`UPDATE` statements when you call `SaveChangesAsync()`. For read-only queries (like listing
customers), tracking is unnecessary overhead. `AsNoTracking()` skips tracking, which improves
performance.

### "What is the `Projection` pattern in the DTOs?"

Instead of loading a full entity and then mapping it to a response object in C#, the `Projection`
pattern lets EF Core generate a `SELECT` statement that only fetches the columns you need:

```csharp
// In CustomerResponse.cs — defines which columns to select
public static Expression<Func<Customer, CustomerResponse>> Projection =>
    x => new CustomerResponse
    {
        Id = x.Id,
        BankId = x.BankId,
        Name = x.Name,
        Email = x.Email,
    };

// Usage — EF Core translates this to SELECT Id, BankId, Name, Email FROM Customers
// (Properties omitted here for brevity)
await _db.Customers
    .Select(CustomerResponse.Projection)
    .ToListAsync();
```

This is more efficient than `SELECT *` because it only transfers the data you actually need.
