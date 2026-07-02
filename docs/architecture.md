# System Architecture

## Overview

HSMS (Hardware Store Management System) is a multi-tier web application built on a clean-architecture
pattern. It is split into a **React SPA** frontend and an **ASP.NET Core 8 REST API** backend, backed
by a **MySQL 8** relational database. The three tiers are deployed independently to Azure infrastructure
and communicate exclusively over HTTPS.

---

## Architectural Style

| Dimension | Choice |
|---|---|
| Pattern | Clean Architecture (Domain → Application → Infrastructure → API) |
| Communication | REST / HTTP JSON |
| Auth | Stateless JWT Bearer tokens |
| Database | Relational (MySQL 8) via raw ADO.NET |
| Deployment | Azure App Service (backend) + Azure Static Web Apps (frontend) |

---

## High-Level Component Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Browser (Client)                             │
│                                                                     │
│   React 19 SPA  ·  TypeScript  ·  Vite  ·  Tailwind CSS           │
│   Hosted on Azure Static Web Apps                                   │
│                                                                     │
│   ┌─────────┐  ┌──────────┐  ┌──────────┐  ┌──────────────────┐  │
│   │  Login  │  │Inventory │  │  Sales   │  │ Reports / Users  │  │
│   └────┬────┘  └────┬─────┘  └────┬─────┘  └────────┬─────────┘  │
│        │            │             │                   │            │
└────────┼────────────┼─────────────┼───────────────────┼────────────┘
         │ HTTPS / JWT Bearer       │                   │
         ▼                          ▼                   ▼
┌─────────────────────────────────────────────────────────────────────┐
│                  ASP.NET Core 8 REST API                            │
│                  Azure App Service (Linux)                          │
│                                                                     │
│  ┌────────────┐ ┌───────────┐ ┌──────────┐ ┌───────────────────┐  │
│  │AuthContrlr │ │ProdContrlr│ │SalesCntrl│ │Reports/Users/Supp │  │
│  └──────┬─────┘ └─────┬─────┘ └─────┬────┘ └─────────┬─────────┘  │
│         │             │             │                 │             │
│  ┌──────┴─────────────┴─────────────┴─────────────────┴──────────┐ │
│  │              Application Layer (Services / Interfaces)         │ │
│  └──────────────────────────────┬─────────────────────────────────┘ │
│                                 │                                   │
│  ┌──────────────────────────────┴─────────────────────────────────┐ │
│  │              Infrastructure Layer (Repositories / DbInit)       │ │
│  └──────────────────────────────┬─────────────────────────────────┘ │
└─────────────────────────────────┼───────────────────────────────────┘
                                  │ MySQL protocol / TLS
                                  ▼
┌─────────────────────────────────────────────────────────────────────┐
│                   MySQL 8 Database                                  │
│                   (Azure Database for MySQL Flexible Server)        │
│                                                                     │
│   Users · Products · Suppliers · Sales · SaleItems · StockLogs     │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Backend Layer Breakdown

### 1. HSMS.Domain
The innermost layer. Contains **pure C# entity classes** with no dependencies.

| File | Entity |
|---|---|
| `Product.cs` | Hardware product (name, SKU, price, qty, category, supplierId) |
| `Supplier.cs` | Supplier (name, contactInfo) |
| `Sale.cs` | Sales transaction header (soldAt, totalAmount, soldBy) |
| `SaleItem.cs` | Line items within a sale |
| `User.cs` | System user (username, passwordHash, role, createdAt) |

### 2. HSMS.Application
Business logic and contracts, depending only on Domain.

| Sub-folder | Contents |
|---|---|
| `Interfaces/` | `IProductRepository`, `ISupplierRepository`, `ISaleRepository`, `IUserRepository`, `IProductService` |
| `DTOs/` | 23 Data Transfer Objects (request/response shapes) |
| `Services/` | `SaleCalculator` — pure subtotal/total calculation logic |

### 3. HSMS.Infrastructure
Concrete implementations of Application interfaces, depending on Domain + Application.

| File | Responsibility |
|---|---|
| `Data/DatabaseInitializer.cs` | Idempotent schema bootstrap on every startup |
| `Data/DbConnectionFactory.cs` | Provides `MySqlConnection` from connection string |
| `Repositories/ProductRepository.cs` | Full CRUD + inventory view + stock update + stock-log write |
| `Repositories/SupplierRepository.cs` | CRUD + safe delete (linked-records check) |
| `Repositories/SaleRepository.cs` | Transactional sale creation, history, invoice, reports |
| `Repositories/UserRepository.cs` | User CRUD, password management, role update |

### 4. HSMS.API
The outermost layer. ASP.NET Core host, middleware pipeline, DI wiring.

| Sub-folder | Contents |
|---|---|
| `Controllers/` | 6 REST controllers |
| `Auth/` | `AppRoles`, `AuthPolicies`, `CurrentUserRoleHandler`, `CurrentUserRoleRequirement` |
| `Services/` | `JwtTokenService`, `PasswordHasher`, `AuthenticationService` |
| `Configuration/` | `CorsOriginPolicy` |
| `Program.cs` | Full application entry point and DI container wiring |

---

## Frontend Layer Breakdown

Built with **React 19**, **TypeScript**, **Vite**, and **Tailwind CSS 3**.

| Layer | Files |
|---|---|
| Entry Point | `main.tsx` → `App.tsx` |
| Routing | React Router v7 (`BrowserRouter` + `Routes`) |
| Pages | 11 page components under `src/pages/` |
| Services | 10 Axios-based API service modules under `src/services/` |
| Auth | `src/auth/roles.ts` + sessionStorage token management |
| Components | `ProtectedRoute`, `PublicOnlyRoute`, `BackendHealthBanner`, etc. |
| Configuration | `src/config/api.ts` reads `VITE_API_BASE_URL` at build time |
| Hooks | Custom React hooks for shared data-fetching logic |

---

## Request Flow (End-to-End)

```
Browser
  │
  │  1. User types credentials → POST /api/auth/login
  ▼
AuthController.Login()
  │  2. Calls AuthenticationService.LoginAsync()
  ▼
AuthenticationService
  │  3. Fetches user via IUserRepository.GetByUsernameAsync()
  │  4. Verifies password via IPasswordHasher.VerifyPassword()
  │  5. Generates JWT via IJwtTokenService.GenerateToken()
  ▼
JwtTokenService → returns signed JWT string
  │
  ▼
AuthController → 200 OK { userId, accessToken, expiresAtUtc, username, role }
  │
  ▼
Browser stores token in sessionStorage
  │
  │  6. Subsequent requests: Authorization: Bearer <token>
  ▼
[Any protected endpoint]
  │  7. JwtBearerMiddleware validates signature, expiry, issuer, audience
  │  8. AuthorizationMiddleware evaluates policy (role check via CurrentUserRoleHandler)
  ▼
Controller executes, calls Repository, executes raw SQL on MySQL
  │
  ▼
JSON response → React state update → UI re-renders
```

---

## Cross-Cutting Concerns

### CORS
- Configured in `Program.cs` via `"FrontendPolicy"`.
- Allowed origins are derived from `Url:Frontend` and `Url:Backend` configuration keys.
- Supports multiple origins (comma / semicolon separated).
- Origins that are not HTTP or HTTPS are rejected at startup.

### Health Checks
- Endpoint: `GET /api/health`
- Checks: MySQL connectivity (opens a real connection on each call)
- Response shape: `{ "status": "healthy", "timestamp": "...", "checks": { "mysql": { "status": "healthy", "description": "..." } } }`

### Database Auto-Migration
- `DatabaseInitializer.InitializeAsync()` is called at startup before any request.
- Creates all 6 tables (`Users`, `Suppliers`, `Products`, `StockLogs`, `Sales`, `SaleItems`) using `CREATE TABLE IF NOT EXISTS`.
- Adds missing columns via `EnsureColumnExistsAsync()` — safe for existing databases.
- Adds missing indexes via `EnsureIndexExistsAsync()`.
- Adds the `FK_Products_Suppliers` foreign key if absent.
- Clears orphaned `SupplierId` values before enforcing the FK.
- Thread-safe via `SemaphoreSlim` — runs at most once per process lifetime.

### Swagger / OpenAPI
- Enabled only when `ASPNETCORE_ENVIRONMENT=Development`.
- Bearer token security scheme pre-configured in Swagger UI.
- Accessible at `/swagger`.

---

## Deployment Architecture

```
GitHub Repository
      │
      │  Push/PR to main
      ▼
GitHub Actions CI/CD Pipeline
  ├── backend_ci (xUnit tests + API tests against ephemeral MySQL 8 service container)
  ├── frontend_ci (Vitest + npm build)
  ├── deploy_backend → Azure App Service (dotnet publish)
  ├── deploy_frontend → Azure Static Web Apps (dist/)
  └── smoke_test → curl /api/health + frontend HTML check
```

See [ci_cd_pipeline.md](./ci_cd_pipeline.md) for full detail.

---

## Diagram References

| Diagram | File |
|---|---|
| System Architecture | [Diagrams/System Architecture.drawio.png](./Diagrams/System%20Architecture.drawio.png) |
| Deployment Architecture | [Diagrams/Deployment Architecture.drawio.png](./Diagrams/Deployment%20Architecture.drawio.png) |
| ER Diagram | [Diagrams/ER_Diagram_HWSMS_CSP.png](./Diagrams/ER_Diagram_HWSMS_CSP.png) |
| Activity Diagram | [Diagrams/Activity_Diagram_HWSMS_CSP.png](./Diagrams/Activity_Diagram_HWSMS_CSP.png) |
| Use Case Diagram | [Diagrams/Usecase_Diagram_HWSMS_CSP.png](./Diagrams/Usecase_Diagram_HWSMS_CSP.png) |
| Sequence Diagrams | [Diagrams/Sequence_diagrams/](./Diagrams/Sequence_diagrams/) |
