# Backend Overview

## Technology Stack

| Technology | Version | Purpose |
|---|---|---|
| .NET / C# | .NET 8, C# 12 | Runtime and language |
| ASP.NET Core | 8.0 | HTTP server, middleware, DI |
| MySql.Data | Latest | ADO.NET MySQL driver |
| Microsoft.AspNetCore.Authentication.JwtBearer | 8.x | JWT validation middleware |
| System.IdentityModel.Tokens.Jwt | Latest | JWT generation |
| Swashbuckle.AspNetCore | Latest | Swagger / OpenAPI docs |
| Microsoft.Extensions.Diagnostics.HealthChecks | 8.x | Health check framework |

---

## Project Structure

```
backend/
├── HSMS.sln                     ← Solution file (references all 5 projects)
├── HSMS.Domain/                 ← Entity classes only, no external deps
├── HSMS.Application/            ← Interfaces, DTOs, SaleCalculator
├── HSMS.Infrastructure/         ← ADO.NET repositories, DatabaseInitializer
├── HSMS.API/                    ← Controllers, Auth, Services, Program.cs
├── HSMS.Tests/                  ← xUnit unit + integration + security tests
├── HSMS.ApiTests/               ← HTTP-level integration tests (live API)
└── HSMS.E2E/                    ← Selenium browser tests
```

---

## Application Startup Sequence (`Program.cs`)

The startup follows this exact sequence on every `dotnet run`:

1. **`WebApplication.CreateBuilder(args)`** — loads `appsettings.json`, then `appsettings.{Environment}.json`, then environment variables (environment variables win).
2. **`CheckEnvironmentVariables()`** — fails fast if any mandatory config key is missing or empty.
3. **`AssignConnectionStrings()`** — uses `ConnectionStrings:DefaultConnection` if present, otherwise builds the MySQL connection string from `Db:Host`, `Db:Port`, `Db:Name`, `Db:User`, `Db:Password`.
4. **Service Registration** — registers MVC, Swagger, health checks, JWT auth, authorization policies, CORS, and all scoped services.
5. **`builder.Build()`** — instantiates the DI container.
6. **`DatabaseInitializer.InitializeAsync()`** — runs idempotent schema migrations (create tables, add columns, add indexes, enforce FK).
7. **Default user seeding** — seeds `admin`, `manager`, `cashier` users only when `ASPNETCORE_ENVIRONMENT` is `Development` or `Integration`.
8. **Middleware pipeline** — Swagger (dev only) → CORS → Authentication → Authorization → Controllers → Health Checks.
9. **`app.Run()`** — begins listening for requests.

---

## REST API Controllers

### `AuthController` — `POST /api/auth`

| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/api/auth/login` | Anonymous | Validates credentials, returns JWT |
| POST | `/api/auth/register` | Anonymous | **Disabled** — returns 403 Forbidden |

**Login flow:**
1. Validates `username` and `password` fields are present.
2. Calls `AuthenticationService.LoginAsync()`.
3. Service fetches user by username from DB.
4. Service verifies the BCrypt password hash.
5. Service generates a signed JWT with claims: `Name` (username), `Role`, `sub` (userId).
6. Returns `{ userId, accessToken, expiresAtUtc, username, role }`.

---

### `ProductController` — `/api/product` and `/api/products`

| Method | Route | Policy | Description |
|---|---|---|---|
| POST | `/api/product` | `InventoryWrite` (Admin, Manager) | Create a new product |
| GET | `/api/product` | `InventoryRead` (Admin, Manager, Cashier) | Get all products |
| GET | `/api/product/{id}` | `InventoryRead` | Get a single product by ID |
| GET | `/api/product/inventory` | `InventoryManagerRead` (Admin, Manager) | Get all products with inventory info and `isLowStock` flag |
| GET | `/api/product/search?query=` | `InventoryRead` | Full-text search on name, SKU, category |
| PUT | `/api/product/{id}` | `InventoryWrite` | Update product details |
| PUT | `/api/product/{id}/stock` | `InventoryWrite` | Update stock quantity (writes to `StockLogs`) |
| DELETE | `/api/product/{id}` | `InventoryDelete` (Admin only) | Delete a product |

Low-stock threshold defaults to **10 units** — configurable via `LOW_STOCK_THRESHOLD` environment variable.

---

### `SuppliersController` — `/api/suppliers`

| Method | Route | Policy | Description |
|---|---|---|---|
| GET | `/api/suppliers` | `InventoryRead` | List all suppliers |
| POST | `/api/suppliers` | `InventoryWrite` | Create a supplier |
| PUT | `/api/suppliers/{id}` | `InventoryWrite` | Update a supplier |
| DELETE | `/api/suppliers/{id}` | `InventoryWrite` | Delete a supplier (blocked if linked to products) |

Deletion returns `409 Conflict` if the supplier still has linked products. Returns `404 Not Found` if the supplier does not exist.

---

### `SalesController` — `/api/sales`

| Method | Route | Policy | Description |
|---|---|---|---|
| POST | `/api/sales` | `SalesCreate` (Admin, Manager, Cashier) | Create a sale transaction |
| GET | `/api/sales/history` | `SalesRead` (Admin, Manager) | Paginated/filtered sales history |
| GET | `/api/sales/{saleId}` | `SalesRead` | Get full details of a single transaction |
| GET | `/api/sales/{saleId}/invoice` | `SalesRead` | Get the invoice data for a transaction |

**Sale creation flow (transactional):**
1. Validates item list (non-empty, valid product IDs, positive quantities, no duplicates).
2. Extracts `soldBy` from the JWT `Name` claim.
3. Calls `SaleRepository.CreateSaleAsync()` — executes inside a single MySQL transaction:
   - Validates each product exists and has sufficient stock.
   - Deducts quantity from `Products`.
   - Inserts into `Sales` (header).
   - Inserts each `SaleItems` row.
4. On insufficient stock → rolls back, throws `InvalidOperationException`.
5. Returns the completed `SaleResponseDTO`.

---

### `ReportsController` — `/api/reports`

| Method | Route | Policy | Description |
|---|---|---|---|
| GET | `/api/reports/daily` | `SalesRead` | Today's sales grouped by hour |
| GET | `/api/reports/monthly` | `SalesRead` | This month's sales grouped by day |
| GET | `/api/reports/analytics` | `SalesRead` | Filtered analytics (date range, product, category) |
| GET | `/api/reports/summary` | `SalesRead` | KPI summary (total revenue, units, avg transaction) |
| GET | `/api/reports/low-stock` | `SalesRead` | Products below low-stock threshold |
| GET | `/api/reports/low-stock/csv` | `SalesRead` | CSV export of low-stock products |
| GET | `/api/reports/daily/csv` | `SalesRead` | CSV export of daily sales |
| GET | `/api/reports/monthly/csv` | `SalesRead` | CSV export of monthly sales |
| GET | `/api/reports/analytics/csv` | `SalesRead` | CSV export of analytics |

Cost ratio for profit estimation defaults to **0.70** — configurable via `REPORT_COST_RATIO`.

---

### `UsersController` — `/api/users`

All endpoints require the `UsersManage` policy (Admin role only).

| Method | Route | Description |
|---|---|---|
| GET | `/api/users` | List all users |
| POST | `/api/users` | Create a new user (min 8-char password, unique username) |
| PUT | `/api/users/{id}/role` | Change a user's role |
| PUT | `/api/users/{id}/password` | Reset a user's password |
| DELETE | `/api/users/{id}` | Delete a user (cannot self-delete) |

When an admin changes their **own role**, a new JWT is issued automatically and returned in the response body alongside the success message.

---

## Authentication & Authorization

### JWT Token

| Field | Value |
|---|---|
| Algorithm | HMAC-SHA256 (`HS256`) |
| Minimum secret length | 32 bytes |
| Claims | `Name` (username), `Role`, `sub` (userId), `iat`, `exp` |
| Expiry | Configurable — default 60 minutes |
| Clock skew | Zero (`ClockSkew = TimeSpan.Zero`) |
| Validation | Issuer ✓ · Audience ✓ · Signature ✓ · Lifetime ✓ · Signed ✓ |

### Authorization Policies

| Policy Name | Allowed Roles |
|---|---|
| `InventoryRead` | Admin, Manager, Cashier |
| `InventoryManagerRead` | Admin, Manager |
| `InventoryWrite` | Admin, Manager |
| `InventoryDelete` | Admin only |
| `SalesCreate` | Admin, Manager, Cashier |
| `SalesRead` | Admin, Manager |
| `UsersManage` | Admin only |

Policies are enforced by `CurrentUserRoleHandler`, a custom `IAuthorizationHandler` that reads the `Role` claim from the token and checks it against the `CurrentUserRoleRequirement`.

### Password Hashing

BCrypt is used for all password hashing and verification via `PasswordHasher` service.

---

## Data Access

All database access uses **raw ADO.NET** (`MySqlConnection`, `MySqlCommand`, `MySqlDataReader`) — no ORM.

- All queries are **parameterized** — no string concatenation for user input.
- All repository methods are `async` / `await`.
- `DbConnectionFactory` encapsulates connection creation.
- `SaleRepository.CreateSaleAsync()` wraps the entire sale creation in a `MySqlTransaction` for atomicity.

---

## Configuration Reference

See also: [ENVIRONMENT_VARIABLES_SUMMARY.md](../ENVIRONMENT_VARIABLES_SUMMARY.md)

### Configuration Priority (highest wins)

1. Environment variables (e.g., `JWT__SECRET`)
2. `appsettings.{ASPNETCORE_ENVIRONMENT}.json`
3. `appsettings.json`

### ASP.NET Core key mapping for environment variables

Double-underscore (`__`) in environment variable names maps to colon (`:`) in configuration paths.

| Environment Variable | Config Key | Required | Description |
|---|---|---|---|
| `ConnectionStrings__DefaultConnection` | `ConnectionStrings:DefaultConnection` | Either this OR Db__ vars | Full MySQL connection string |
| `DB__HOST` | `Db:Host` | If no full connection string | MySQL server hostname |
| `DB__PORT` | `Db:Port` | If no full connection string | MySQL port (default 3306) |
| `DB__NAME` | `Db:Name` | If no full connection string | Database name |
| `DB__USER` | `Db:User` | If no full connection string | MySQL username |
| `DB__PASSWORD` | `Db:Password` | If no full connection string | MySQL password |
| `JWT__SECRET` | `Jwt:Secret` | ✅ Yes | ≥32-byte signing secret |
| `JWT__ISSUER` | `Jwt:Issuer` | ✅ Yes | Token issuer claim |
| `JWT__AUDIENCE` | `Jwt:Audience` | ✅ Yes | Token audience claim |
| `JWT__ACCESS_TOKEN_EXPIRY_MINUTES` | `Jwt:AccessTokenExpiryMinutes` | ✅ Yes | Token TTL in minutes |
| `URL__FRONTEND` | `Url:Frontend` | ✅ Yes (CORS) | Frontend origin URL |
| `URL__BACKEND` | `Url:Backend` | Optional | Backend public URL (added to CORS) |
| `Password__Admin` | `Password:Admin` | Dev/Integration only | Seeds `admin` user |
| `Password__Manager` | `Password:Manager` | Dev/Integration only | Seeds `manager` user |
| `Password__Cashier` | `Password:Cashier` | Dev/Integration only | Seeds `cashier` user |
| `LOW_STOCK_THRESHOLD` | `LOW_STOCK_THRESHOLD` | Optional (default: 10) | Low-stock alert threshold |
| `REPORT_COST_RATIO` | `REPORT_COST_RATIO` | Optional (default: 0.70) | Cost-to-price ratio for profit estimation |
