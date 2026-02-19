# Banking API: Multi-Tenant EF Core Demo

This repo demonstrates a practical multi-tenant pattern using EF Core:

- One SQLite database
- Two DbContexts mapped to the same tables
  - `TenantDbContext`: tenant-scoped via global query filters (`BankId`)
  - `AdminDbContext`: unfiltered for admin and migrations

Tenancy is resolved from an HTTP header:

- `X-Bank-Id: <guid>`

## Roles

- `Admin`: cross-bank access (JWT claim `IsAdmin=true`)
- `Staff`: bank-scoped access (JWT claim `role=Staff` + `X-Bank-Id`)
- `Customer`: bank-scoped + row-scoped to their own `CustomerId` (JWT claim `role=Customer` + `CustomerId` + `X-Bank-Id`)

## Run

```bash
dotnet run --project BankingApi/BankingApi.csproj
```

The app:

- Applies migrations on startup (via `AdminDbContext`)
- Seeds demo data on first run
- Starts on `http://localhost:5294`

## Try It

Use `BankingApi/BankingApi.http`.

1) Get seeded login emails:

`GET /api/auth/seeded-logins`

2) Login:

`POST /api/auth/login` with one of the seeded emails.

3) For staff/customer endpoints, include:

- `Authorization: Bearer <token>`
- `X-Bank-Id: <bank-guid>`

## Where Tenant Isolation Happens

- `BankingApi/Data/TenantDbContext.cs` applies global query filters:
  - `Customer.BankId == BankId`
  - `Account.BankId == BankId`
  - `Transaction.BankId == BankId`

Admin endpoints use `AdminDbContext` which has no filters.
