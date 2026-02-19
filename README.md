Multi-tenant demo (single database) showing the "filtered vs unfiltered DbContext" pattern.

What this demonstrates
- Tenant-scoped DbContext (`TenantDbContext`) uses EF Core global query filters to enforce `TenantId`.
- Admin/unfiltered DbContext (`AdminDbContext`) maps to the same tables but has no tenant filter.
- Tenant is resolved from request header `X-Tenant-Id` (header-only for clarity).
- Admin endpoints require an `IsAdmin=true` claim in JWT.

Tenant header
- All non-admin list endpoints require `X-Tenant-Id: <guid>`.

Admin auth
- Configure admin emails in appsettings under `Admin:Emails`.
- Login with one of those emails; the JWT will include `IsAdmin=true`.

Endpoints
- Auth:
  - `POST /api/auth/login`
  - `GET /api/auth/me`
- Tenant-scoped lists (requires JWT + `X-Tenant-Id`):
  - `GET /api/lists`
  - `GET /api/lists/{id}`
  - `POST /api/lists`
  - `PUT /api/lists/{id}`
  - `DELETE /api/lists/{id}`
- Admin lists (requires JWT with `IsAdmin=true`):
  - `GET /api/admin/lists`
  - `GET /api/admin/lists/{id}`
  - `POST /api/admin/lists` (body includes `tenantId`)
  - `PUT /api/admin/lists/{id}`
  - `DELETE /api/admin/lists/{id}`

Run
1) `dotnet run --project TodoApi`
2) Use `TodoApi/TodoApi.http`

Database
- Default connection string points to `TodoApi.mt.db` (so the old demo DB doesn't conflict).
