# Banking API: Multi-Tenant EF Core Demo

A minimal demo of multi-tenant data isolation using EF Core with a dual DbContext pattern.

## Overview

```mermaid
flowchart TB
    subgraph Client
        R[HTTP Request]
    end

    subgraph "ASP.NET Core Pipeline"
        AUTH[Authentication]
        AUTHZ[Authorization]
    end

    subgraph "DbContexts"
        TDB[TenantDbContext<br/>Query Filters]
        ADB[AdminDbContext<br/>No Filters]
    end

    subgraph Database
        DB[(SQLite<br/>Single Database)]
    end

    R --> AUTH --> AUTHZ
    AUTHZ -->|Staff/Customer| TDB --> DB
    AUTHZ -->|Admin| ADB --> DB
```

## Key Concepts

### 1. Dual DbContext Pattern

Two DbContexts map to the same database tables but with different behaviors:

| DbContext | Purpose | Query Filters |
|-----------|---------|---------------|
| `TenantDbContext` | Tenant-scoped operations | Yes - filters by `BankId` |
| `AdminDbContext` | Cross-tenant admin + migrations | No - sees all data |

```mermaid
flowchart LR
    subgraph "TenantDbContext"
        TF1[Customer.BankId == BankId]
        TF2[Account.BankId == BankId]
        TF3[Transaction.BankId == BankId]
    end

    subgraph "AdminDbContext"
        AF[No Filters]
    end

    TF1 & TF2 & TF3 --> D[(Database)]
    AF --> D
```

**Implementation:**

[`BankingApi/Data/TenantDbContext.cs`](BankingApi/Data/TenantDbContext.cs):
```csharp
public sealed class TenantDbContext : AppDbContextBase
{
    public Guid BankId { get; }

    public TenantDbContext(DbContextOptions<TenantDbContext> options, IBankAccessor bankAccessor)
        : base(options)
    {
        BankId = bankAccessor.GetRequiredBankId();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Global query filters ensure tenant isolation
        modelBuilder.Entity<Customer>().HasQueryFilter(x => x.BankId == BankId);
        modelBuilder.Entity<Account>().HasQueryFilter(x => x.BankId == BankId);
        modelBuilder.Entity<Transaction>().HasQueryFilter(x => x.BankId == BankId);
    }
}
```

[`BankingApi/Data/AdminDbContext.cs`](BankingApi/Data/AdminDbContext.cs):
```csharp
public sealed class AdminDbContext : AppDbContextBase
{
    public AdminDbContext(DbContextOptions<AdminDbContext> options) : base(options) { }
    // No query filters - sees all data across all tenants
}
```

### 2. Tenant Resolution

Tenant is resolved from the `BankId` claim in the JWT. Because the JWT is cryptographically signed, no separate header is needed — the claim alone is sufficient and tamper-proof:

```mermaid
sequenceDiagram
    participant C as Client
    participant BA as BankAccessor
    participant TDB as TenantDbContext
    participant DB as Database

    C->>BA: Request with Authorization: Bearer jwt
    BA->>BA: Read BankId claim from HttpContext.User
    BA->>TDB: Provide BankId
    TDB->>TDB: Apply query filter
    TDB->>DB: SELECT ... WHERE BankId = ?
    DB-->>TDB: Filtered results
    TDB-->>C: Tenant-scoped data
```

[`BankingApi/Infrastructure/BankAccessor.cs`](BankingApi/Infrastructure/BankAccessor.cs):
```csharp
public sealed class BankAccessor : IBankAccessor
{
    public Guid GetRequiredBankId()
    {
        if (_httpContextAccessor.HttpContext?.User.TryGetBankIdClaim(out var bankId) == true)
            return bankId;
        throw new UnauthorizedAccessException("Missing BankId claim in token.");
    }
}
```

### 3. Entity Relationships

```mermaid
erDiagram
    Bank ||--o{ Customer : has
    Bank ||--o{ Account : has
    Customer ||--o{ Account : owns
    Account ||--o{ Transaction : has

    Bank {
        Guid Id PK
        string Name
        string Code
    }
    Customer {
        Guid Id PK
        Guid BankId FK
        string Name
        string Email
    }
    Account {
        Guid Id PK
        Guid BankId FK
        Guid CustomerId FK
        string AccountNumber
        decimal Balance
    }
    Transaction {
        Guid Id PK
        Guid BankId FK
        Guid AccountId FK
        string Type
        decimal Amount
    }
```

### 4. Roles & Access Control

| Role | Scope | JWT Claims | Endpoints |
|------|-------|------------|-----------|
| **Admin** | Cross-tenant | `IsAdmin=true` | `/api/admin/*` |
| **Staff** | Single bank | `role=Staff`, `BankId` | `/api/customers`, `/api/accounts`, `/api/transactions` |
| **Customer** | Own data only | `role=Customer`, `BankId`, `CustomerId` | `/api/my/accounts` (read-only) |

```mermaid
flowchart LR
    subgraph Admin
        A1[GET /api/admin/banks]
        A2[GET /api/admin/customers]
        A3[GET /api/admin/accounts]
    end

    subgraph Staff
        S1[CRUD /api/customers]
        S2[CRUD /api/accounts]
        S3[CRUD /api/transactions]
    end

    subgraph Customer
        C1[GET /api/my/accounts]
        C2[GET /api/my/accounts/id/transactions]
    end
```

## Request Flow Example

```mermaid
sequenceDiagram
    participant C as Staff Client
    participant A as BankAccessor
    participant T as TenantDbContext
    participant D as SQLite

    Note over C: 1. Login, get JWT with BankId claim
    C->>A: GET /api/customers<br/>Authorization: Bearer jwt
    A->>A: 2. Read BankId from JWT claim
    A->>T: 3. Create TenantDbContext(bank-a-guid)
    T->>T: 4. Query filter: WHERE BankId = bank-a-guid
    T->>D: SELECT * FROM Customers WHERE BankId = bank-a-guid
    D-->>T: Only Bank A customers
    T-->>C: 5. Return filtered results
```

## Run

```bash
dotnet run --project BankingApi/BankingApi.csproj
```

The app:
- Applies migrations on startup (via `AdminDbContext`)
- Seeds demo data on first run
- Starts on `http://localhost:5294`

## Try It

Use [`BankingApi/BankingApi.http`](BankingApi/BankingApi.http) for ready-to-use HTTP examples.

1. **Get seeded login info:**
   ```
   GET /api/auth/seeded-logins
   ```

2. **Login** (copy `accessToken` from response):
   ```
   POST /api/auth/login
   { "email": "staff.norge@demo.com" }
   ```

3. **Call tenant-scoped endpoints:**
   ```
   GET /api/customers
   Authorization: Bearer <accessToken>
   ```

## Seeded Demo Data

| Entity | Bank A (Norge) | Bank B (Svensk) |
|--------|----------------|-----------------|
| Bank Code | NO-001 | SE-001 |
| Staff | staff.norge@demo.com | staff.svensk@demo.com |
| Customer | customer.ola@demo.com | customer.anna@demo.com |
| Account | NOK account | SEK account |

Admin: `admin@demo.com` (cross-tenant access)

## Project Structure

```
BankingApi/
├── Controllers/
│   ├── Admin/              # Cross-tenant admin endpoints
│   ├── CustomersController.cs   # Staff: CRUD customers
│   ├── AccountsController.cs    # Staff: CRUD accounts
│   ├── TransactionsController.cs # Staff: CRUD transactions
│   ├── MyAccountsController.cs  # Customer: read own accounts
│   └── AuthController.cs        # Login
├── Data/
│   ├── AppDbContextBase.cs      # Shared model configuration
│   ├── TenantDbContext.cs       # Query-filtered context
│   └── AdminDbContext.cs        # Unfiltered context
├── Infrastructure/
│   ├── AuthConstants.cs         # AppClaimTypes and AuthPolicies constants
│   ├── BankAccessor.cs          # JWT claim-based tenant resolution
│   ├── JwtTokenService.cs       # JWT generation with claims
│   └── SeedData.cs              # Demo data seeding
├── Models/                      # Bank, Customer, Account, Transaction
├── Dtos/                        # Request/response DTOs with Projection + ToResponse()
├── Services/                    # Business logic (tenant-scoped)
│   ├── TransactionFactory.cs    # Shared transaction construction
│   └── Admin/                   # Admin services (unfiltered)
└── Program.cs                   # DI, auth, middleware pipeline
```

## Key Files

| File | Purpose |
|------|---------|
| [`Program.cs`](BankingApi/Program.cs) | DI setup, auth policies, middleware pipeline |
| [`Data/TenantDbContext.cs`](BankingApi/Data/TenantDbContext.cs) | Global query filters for tenant isolation |
| [`Data/AdminDbContext.cs`](BankingApi/Data/AdminDbContext.cs) | Unfiltered context for admin/migrations |
| [`Infrastructure/BankAccessor.cs`](BankingApi/Infrastructure/BankAccessor.cs) | Resolve tenant from JWT claim |
| [`Infrastructure/AuthConstants.cs`](BankingApi/Infrastructure/AuthConstants.cs) | `AppClaimTypes` and `AuthPolicies` string constants |
| [`Infrastructure/JwtTokenService.cs`](BankingApi/Infrastructure/JwtTokenService.cs) | Issue JWTs with role/BankId/CustomerId claims |
| [`Infrastructure/SeedData.cs`](BankingApi/Infrastructure/SeedData.cs) | Create demo banks, customers, accounts, users |
