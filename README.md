# HSMS / HWSMS

Hardware Store Management System for the CSP assignment. This repository contains a .NET 8 backend API, a React 19 + Vite frontend, MySQL-backed persistence, automated tests, generated API artifacts, and supporting project documentation.

## Overview

HSMS supports the day-to-day workflows of a hardware store:

- authentication with JWT
- product and inventory management
- supplier management
- sales creation and transaction history
- invoice retrieval
- daily, monthly, analytics, summary, and low-stock reporting
- admin-only user management with role changes and password reset

The current repository structure is:

```text
CSP_HWSMS/
├── backend/
│   ├── HSMS.API/
│   ├── HSMS.Application/
│   ├── HSMS.Domain/
│   ├── HSMS.Infrastructure/
│   ├── HSMS.Tests/
│   ├── HSMS.ApiTests/
│   └── HSMS.E2E/
├── frontend/
│   └── HWSMS_UI/
├── docs/
│   ├── Diagrams/
│   ├── SRS/
│   └── Test-Documents/
├── jmeter/
├── postman/
└── scripts/
```

## Stack

| Area | Technology |
|---|---|
| Backend | ASP.NET Core 8 / .NET 8 |
| Frontend | React 19 + TypeScript + Vite |
| Styling | Tailwind CSS |
| Database | MySQL |
| Data access | ADO.NET with `MySql.Data` |
| API docs | Swagger in `Development` |
| Backend tests | xUnit, Moq, coverlet |
| Frontend tests | Vitest, Testing Library |
| CI/CD | GitHub Actions + Azure App Service + Azure Static Web Apps |

## Current Application Surface

### Backend controllers

- `AuthController`
- `ProductController`
- `SuppliersController`
- `SalesController`
- `ReportsController`
- `UsersController`

### Frontend routes

| Route | Access |
|---|---|
| `/login` | Public |
| `/dashboard` | Admin, Manager |
| `/inventory` | Admin, Manager |
| `/sales` | Admin, Manager, Cashier |
| `/suppliers` | Admin, Manager |
| `/transactions` | Admin, Manager |
| `/transactions/:transactionId/invoice` | Admin, Manager |
| `/reports/daily` | Admin, Manager |
| `/users` | Admin |
| `/access-denied` | Authenticated users |

### Role model

| Role | Summary |
|---|---|
| `Admin` | Full access including user management and product deletion |
| `Manager` | Inventory, suppliers, sales history, reports, product updates |
| `Cashier` | Sales creation and general product read access |

## Auth and API Notes

- `POST /api/auth/login` is active.
- `POST /api/auth/register` exists but currently returns `403 Forbidden` because self-registration is disabled.
- Password reset is handled through `PUT /api/users/{id}/password` and is admin-only.
- Swagger is enabled only when `ASPNETCORE_ENVIRONMENT=Development`.
- Health checks are exposed at `GET /api/health`.

## Local Development

Use the dedicated setup guide for step-by-step instructions:

- [QUICK_START.md](./QUICK_START.md)

Short version:

```bash
cd backend
dotnet restore
dotnet run --project HSMS.API
```

```bash
cd frontend/HWSMS_UI
npm install
npm run dev
```

Default local URLs:

- backend: `http://localhost:5162`
- frontend: `http://localhost:5173`

## Configuration

Documentation is split by purpose:

- [CONFIGURATION_INDEX.md](./CONFIGURATION_INDEX.md): configuration and doc navigation
- [ENVIRONMENT_VARIABLES_SUMMARY.md](./ENVIRONMENT_VARIABLES_SUMMARY.md): runtime variable reference
- [ENV_VARIABLES_CHECKLIST.md](./ENV_VARIABLES_CHECKLIST.md): pre-demo / deployment checklist
- [DEPLOYMENT_GUIDE.md](./DEPLOYMENT_GUIDE.md): deployment workflow and CI/CD

Key backend settings:

- `ConnectionStrings__DefaultConnection` (or individual database parameters `Db__Host`, `Db__Port`, `Db__Name`, `Db__User`, `Db__Password`)
- `Jwt__Secret`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Jwt__AccessTokenExpiryMinutes`
- `Url__Frontend`
- `Url__Backend`
- `Password__Admin`
- `Password__Manager`
- `Password__Cashier`

Key frontend settings:

- `VITE_API_BASE_URL`
- `VITE_DEBUG`

## Testing

The current validated automated suite includes:

- backend tests: `296` passing in `backend/HSMS.Tests`
- API tests: `194` passing in `backend/HSMS.ApiTests`
- frontend tests: `17` passing in `frontend/HWSMS_UI`

Common commands:

```bash
dotnet test backend/HSMS.Tests/HSMS.Tests.csproj --no-restore
dotnet test backend/HSMS.ApiTests/HSMS.ApiTests.csproj --no-restore
```

```bash
cd frontend/HWSMS_UI
npm test
npm run build
```

Testing documentation:

- [docs/Test-Documents/TESTING_OVERVIEW.md](./docs/Test-Documents/TESTING_OVERVIEW.md)
- [docs/Test-Documents/HSMS_MASTER_TEST_PLAN.md](./docs/Test-Documents/HSMS_MASTER_TEST_PLAN.md)
- [docs/Test-Documents/API_TEST_PLAN.md](./docs/Test-Documents/API_TEST_PLAN.md)
- [docs/Test-Documents/Guides/TEST_EXECUTION_GUIDE.md](./docs/Test-Documents/Guides/TEST_EXECUTION_GUIDE.md)

## Generated Test Artifacts

The repo includes generated and source-driven test assets:

- Postman collection: [postman/HSMS_API.postman_collection.json](./postman/HSMS_API.postman_collection.json)
- Postman environment: [postman/HSMS_Local.postman_environment.json](./postman/HSMS_Local.postman_environment.json)
- JMeter plan: [jmeter/HSMS_API_100_users.jmx](./jmeter/HSMS_API_100_users.jmx)
- JMeter properties: [jmeter/hsms-jmeter.properties](./jmeter/hsms-jmeter.properties)

Supporting scripts:

- `scripts/generate-postman-collection.js`
- `scripts/generate-jmeter-test-plan.js`
- `scripts/reset-and-seed-db.js`

## Additional Project Docs

- [backend/README.md](./backend/README.md)
- [frontend/HWSMS_UI/README.md](./frontend/HWSMS_UI/README.md)
- [USER_MANAGEMENT_FEATURES.md](./USER_MANAGEMENT_FEATURES.md)
- [docs/EPIC_4_4_DEVOPS_DEPLOYMENT.md](./docs/EPIC_4_4_DEVOPS_DEPLOYMENT.md)
- [docs/SRS/SRS_Document_V1.3.pdf](./docs/SRS/SRS_Document_V1.3.pdf)

## Notes

- Historical test reports and superseded summaries are kept under [docs/Test-Documents/Archive](./docs/Test-Documents/Archive).
- Active operational documentation should be treated as the source of truth over archived sprint-era reports.
