# HSMS — Hardware Store Management System

> **A full-stack web application for managing hardware store inventory, sales, suppliers, and reporting.**
> Built as a SLIIT CSP Y3S1 assignment.

---

## Quick Links

| Document | Description |
|---|---|
| [Quick Start](./QUICK_START.md) | Get the app running locally in minutes |
| [Architecture](./docs/architecture.md) | System design, layer breakdown, request flow |
| [Backend Overview](./docs/backend_overview.md) | API endpoints, auth, configuration reference |
| [Frontend Overview](./docs/frontend_overview.md) | Pages, routing, services, environment variables |
| [Database Schema](./docs/database_schema.md) | All tables, columns, indexes, relationships |
| [Testing Strategy](./docs/testing_strategy.md) | All 5 testing layers explained |
| [CI/CD Pipeline](./docs/ci_cd_pipeline.md) | GitHub Actions pipeline — all 6 jobs |
| [Maintenance Guide](./docs/maintenance.md) | How to add features, rotate secrets, update deps |
| [Contributing Guide](./docs/contributing.md) | Branching, code style, PR workflow |
| [Deployment Guide](./DEPLOYMENT_GUIDE.md) | Azure deployment step-by-step |
| [Environment Variables](./ENVIRONMENT_VARIABLES_SUMMARY.md) | All runtime configuration keys |
| [Pre-Deploy Checklist](./ENV_VARIABLES_CHECKLIST.md) | Checklist before going live |

---

## What is HSMS?

HSMS (Hardware Store Management System) supports the day-to-day operations of a hardware store:

- 🔐 **Authentication** — JWT-based login with role-enforced access control
- 📦 **Inventory management** — full product CRUD, stock updates, low-stock alerts
- 🏭 **Supplier management** — manage supplier records linked to products
- 🛒 **Sales processing** — POS-style transaction creation with automatic stock deduction
- 🧾 **Invoices** — retrieve and preview invoices for completed transactions
- 📊 **Reports** — daily, monthly, analytics, summary, and low-stock reports with CSV exports
- 👥 **User management** — admin-only user creation, role management, and password reset

---

## Technology Stack

| Area | Technology |
|---|---|
| **Backend** | ASP.NET Core 8 / .NET 8 / C# 12 |
| **Frontend** | React 19 + TypeScript + Vite 7 |
| **Styling** | Tailwind CSS 3 |
| **Database** | MySQL 8 |
| **Data Access** | Raw ADO.NET (`MySql.Data`) — no ORM |
| **Authentication** | JWT Bearer (HMAC-SHA256) + BCrypt passwords |
| **API Documentation** | Swagger (Development only) |
| **Backend Tests** | xUnit, Moq, Coverlet |
| **Frontend Tests** | Vitest, @testing-library/react |
| **E2E Tests** | Selenium WebDriver (Chrome headless) |
| **Load Tests** | Apache JMeter |
| **CI/CD** | GitHub Actions |
| **Backend Hosting** | Azure App Service (Linux) |
| **Frontend Hosting** | Azure Static Web Apps |
| **Database Hosting** | Azure Database for MySQL Flexible Server |

---

## Repository Structure

```
CSP_HWSMS/
├── .github/
│   └── workflows/
│       └── ci-cd.yml              ← GitHub Actions CI/CD pipeline
│
├── backend/                       ← ASP.NET Core 8 solution
│   ├── HSMS.sln
│   ├── HSMS.API/                  ← HTTP layer: controllers, auth, middleware
│   ├── HSMS.Application/          ← Interfaces, DTOs, business logic
│   ├── HSMS.Domain/               ← Pure entity classes (no dependencies)
│   ├── HSMS.Infrastructure/       ← ADO.NET repositories + DatabaseInitializer
│   ├── HSMS.Tests/                ← Unit + Integration + Security tests (296)
│   ├── HSMS.ApiTests/             ← Live HTTP integration tests (194)
│   └── HSMS.E2E/                  ← Selenium browser E2E tests
│
├── frontend/
│   └── HWSMS_UI/                  ← React 19 SPA (Vite + TypeScript + Tailwind)
│
├── docs/
│   ├── architecture.md            ← System architecture and layer breakdown
│   ├── backend_overview.md        ← API endpoints and configuration reference
│   ├── frontend_overview.md       ← Pages, routing, services
│   ├── database_schema.md         ← Full schema with column types and relationships
│   ├── testing_strategy.md        ← All test layers documented
│   ├── ci_cd_pipeline.md          ← All 6 CI/CD jobs explained
│   ├── maintenance.md             ← How to add features and maintain the app
│   ├── contributing.md            ← Developer setup and contribution workflow
│   ├── Diagrams/                  ← Architecture, ER, sequence, activity diagrams
│   ├── SRS/                       ← Software Requirements Specification
│   └── Test-Documents/            ← Test plans, guides, load test docs
│
├── jmeter/                        ← JMeter load test plan (100 concurrent users)
├── postman/                       ← Postman collection + local environment
├── scripts/                       ← DB seed, Postman/JMeter generation scripts
│
├── README.md                      ← This file
├── QUICK_START.md                 ← Local dev setup
├── DEPLOYMENT_GUIDE.md            ← Azure deployment guide
├── ENVIRONMENT_VARIABLES_SUMMARY.md ← All config variables
├── ENV_VARIABLES_CHECKLIST.md    ← Pre-deploy checklist
└── CONFIGURATION_INDEX.md         ← Configuration doc index
```

---

## Local Development (Short Version)

See [QUICK_START.md](./QUICK_START.md) for the full guide.

### Backend

```bash
cd backend
dotnet restore HSMS.sln
dotnet run --project HSMS.API/HSMS.API.csproj
```

Default URL: `http://localhost:5162`
Swagger UI: `http://localhost:5162/swagger`

### Frontend

```bash
cd frontend/HWSMS_UI
npm install
npm run dev
```

Default URL: `http://localhost:5173`

---

## Role Model

| Role | Capabilities |
|---|---|
| **Admin** | Everything — including user management, product deletion, all reports |
| **Manager** | Inventory, suppliers, sales history, reports, product updates (no deletion, no user management) |
| **Cashier** | Sales creation, product search/read only |

---

## API Surface

| Controller | Base Route | Key Endpoints |
|---|---|---|
| `AuthController` | `/api/auth` | `POST /login` |
| `ProductController` | `/api/product` | Full CRUD + `/inventory` + `/search` + `/{id}/stock` |
| `SuppliersController` | `/api/suppliers` | Full CRUD |
| `SalesController` | `/api/sales` | `POST /`, `GET /history`, `GET /{id}`, `GET /{id}/invoice` |
| `ReportsController` | `/api/reports` | `daily`, `monthly`, `analytics`, `summary`, `low-stock` + CSV variants |
| `UsersController` | `/api/users` | Full CRUD + `/{id}/role` + `/{id}/password` |
| — | `/api/health` | Database health check |

---

## Frontend Routes

| Route | Roles |
|---|---|
| `/login` | Public |
| `/dashboard` | Admin, Manager |
| `/inventory` | Admin, Manager |
| `/sales` | Admin, Manager, Cashier |
| `/suppliers` | Admin, Manager |
| `/transactions` | Admin, Manager |
| `/transactions/:id/invoice` | Admin, Manager |
| `/reports/daily` | Admin, Manager |
| `/users` | Admin only |

---

## Testing Summary

| Suite | Count | CI |
|---|---|---|
| `HSMS.Tests` (unit + integration + security) | **296** | ✅ |
| `HSMS.ApiTests` (live HTTP) | **194** | ✅ |
| Frontend (Vitest) | **17** | ✅ |
| E2E Selenium | 3 | Manual |

Run all backend tests:
```bash
dotnet test backend/HSMS.Tests/HSMS.Tests.csproj
dotnet test backend/HSMS.ApiTests/HSMS.ApiTests.csproj
```

Run frontend tests:
```bash
cd frontend/HWSMS_UI && npm test
```

---

## Diagrams

| Diagram | Location |
|---|---|
| System Architecture | [docs/Diagrams/System Architecture.drawio.png](./docs/Diagrams/System%20Architecture.drawio.png) |
| Deployment Architecture | [docs/Diagrams/Deployment Architecture.drawio.png](./docs/Diagrams/Deployment%20Architecture.drawio.png) |
| Entity Relationship (ER) | [docs/Diagrams/ER_Diagram_HWSMS_CSP.png](./docs/Diagrams/ER_Diagram_HWSMS_CSP.png) |
| Activity Diagram | [docs/Diagrams/Activity_Diagram_HWSMS_CSP.png](./docs/Diagrams/Activity_Diagram_HWSMS_CSP.png) |
| Use Case Diagram | [docs/Diagrams/Usecase_Diagram_HWSMS_CSP.png](./docs/Diagrams/Usecase_Diagram_HWSMS_CSP.png) |
| Sequence Diagrams (7) | [docs/Diagrams/Sequence_diagrams/](./docs/Diagrams/Sequence_diagrams/) |

---

## Additional Documentation

- [Software Requirements Specification (SRS)](./docs/SRS/SRS_Document_V1.3.pdf)
- [Master Test Plan](./docs/Test-Documents/HSMS_MASTER_TEST_PLAN.md)
- [API Test Plan](./docs/Test-Documents/API_TEST_PLAN.md)
- [JMeter Load Test Guide](./docs/Test-Documents/JMETER_LOAD_TEST.md)
- [User Management Features](./USER_MANAGEMENT_FEATURES.md)

---

## Notes

- Historical test reports are archived under [docs/Test-Documents/Archive/](./docs/Test-Documents/Archive/).
- The `HSMS.E2E` Selenium tests skip automatically unless `HSMS_E2E_BASE_URL`, `HSMS_E2E_USERNAME`, and `HSMS_E2E_PASSWORD` environment variables are set.
- Self-registration (`POST /api/auth/register`) is permanently disabled — all users must be created by an Admin.
- Swagger is only exposed when `ASPNETCORE_ENVIRONMENT=Development`.
