# HSMS / HWSMS

Hardware Store Management System built as a full-stack CSP assignment. The project combines an ASP.NET Core Web API, a React frontend, and a MySQL database to support inventory, suppliers, sales, reporting, and user-role management for a hardware store.

## Table of Contents

1. [Project Overview](#project-overview)
2. [Core Features](#core-features)
3. [Tech Stack](#tech-stack)
4. [Architecture](#architecture)
5. [Repository Structure](#repository-structure)
6. [Role-Based Access](#role-based-access)
7. [Getting Started](#getting-started)
8. [Environment Configuration](#environment-configuration)
9. [Running the Application](#running-the-application)
10. [API Overview](#api-overview)
11. [Testing](#testing)
12. [Documentation](#documentation)
13. [Troubleshooting](#troubleshooting)

## Project Overview

This system is designed to help a hardware store manage day-to-day operations from a single web interface. It supports:

- product and inventory management
- supplier management
- sales transaction processing
- invoice viewing
- daily and monthly sales reporting
- low-stock monitoring
- user management with role-based permissions
- JWT-based authentication

The frontend lives in `frontend/HWSMS_UI` and communicates with the backend API in `backend/HSMS.API`. The backend persists data in MySQL using repository classes built on ADO.NET.

## Core Features

### Inventory and Products

- Create, read, update, and delete products
- Track SKU, category, price, quantity, and supplier
- Search products by query
- View inventory with low-stock indicators
- Update stock quantities independently from full product edits

### Suppliers

- Create, update, list, and delete suppliers
- Prevent deletion when linked records still exist
- Associate suppliers with products

### Sales

- Create sales with one or more sale items
- Prevent duplicate products inside a single sale request
- Validate stock and business rules before saving
- View transaction history
- View transaction details and invoice data

### Reports

- Daily sales reports
- Monthly sales reports
- Filtered analytics
- Low-stock reporting
- CSV export for supported report types
- Summary report endpoint aggregating multiple report areas

### Authentication and Authorization

- Login and registration endpoints
- Password hashing
- JWT token generation and validation
- Policy-based authorization
- Role-aware frontend route protection

### User Management

- Create new users with role assignment
- Update user roles
- Delete users
- **Reset user passwords with confirmation** (NEW)
- **Password confirmation on user creation** (NEW)
- **Password visibility toggles** (NEW)
- Organized user list with filtering and sorting
- Admin-only access with proper authorization

## Tech Stack

| Area | Technology |
|---|---|
| Backend | ASP.NET Core 8 / .NET 8 |
| Language | C# 12 |
| Frontend | React 19 + TypeScript |
| Build Tool | Vite |
| Styling | Tailwind CSS |
| Database | MySQL |
| Data Access | ADO.NET with `MySql.Data` |
| API Docs | Swagger / OpenAPI |
| Backend Tests | xUnit, Moq, coverlet |
| Frontend Tests | Vitest, Testing Library |

## Architecture

The backend follows a clean layered structure:

```text
HSMS.API
  Controllers, auth, startup, middleware, DI configuration

HSMS.Application
  DTOs and repository/service interfaces

HSMS.Domain
  Core entity models

HSMS.Infrastructure
  Database initialization, connection factory, repositories

HSMS.Tests / HSMS.ApiTests / HSMS.E2E
  Unit, integration, API, and browser-oriented test projects
```

Dependency direction is centered around contracts in `HSMS.Application`, while persistence logic is implemented in `HSMS.Infrastructure`.

## Repository Structure

```text
CSP_HWSMS/
├── backend/
│   ├── HSMS.sln
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
├── scripts/
├── QUICK_START.md
├── DEPLOYMENT_GUIDE.md
└── docs/Test-Documents/TESTING_OVERVIEW.md
```

## Role-Based Access

The application currently uses three roles:

| Role | Access Summary |
|---|---|
| `Admin` | Full access including user management and product deletion |
| `Manager` | Inventory, suppliers, sales views, reports, and product updates |
| `Cashier` | Sales creation and basic inventory read access |

### Backend authorization policies

| Policy | Allowed Roles |
|---|---|
| `InventoryRead` | Admin, Manager, Cashier |
| `InventoryManagerRead` | Admin, Manager |
| `InventoryWrite` | Admin, Manager |
| `InventoryDelete` | Admin |
| `SalesCreate` | Admin, Manager, Cashier |
| `SalesRead` | Admin, Manager |
| `UsersManage` | Admin |

### Frontend protected routes

| Route | Access |
|---|---|
| `/dashboard` | Admin, Manager |
| `/inventory` | Admin, Manager |
| `/sales` | Admin, Manager, Cashier |
| `/suppliers` | Admin, Manager |
| `/transactions` | Admin, Manager |
| `/transactions/:transactionId/invoice` | Admin, Manager |
| `/reports/daily` | Admin, Manager |
| `/users` | Admin only |

## Getting Started

### Prerequisites

Install the following before running the project locally:

| Tool | Recommended Version |
|---|---|
| .NET SDK | 8.0+ |
| Node.js | 18+ |
| npm | 9+ |
| MySQL Server | 8.0+ |

### 1. Clone and open the repository

```bash
git clone <your-repository-url>
cd CSP_HWSMS
```

### 2. Configure the backend

The backend reads configuration from:

- `appsettings.json`
- `appsettings.{Environment}.json`
- environment variables

Environment variables override JSON settings.

Use the template file as a reference:

```bash
cd backend
cp .env.example .env
```

The current backend code does not automatically load `.env` files. Use one of these approaches instead:

- set shell environment variables before running `dotnet run`
- configure `appsettings.Development.json` locally
- use your IDE run configuration to inject environment variables

At minimum, make sure you have:

- a valid MySQL connection
- a JWT secret of at least 32 bytes
- valid JWT issuer and audience values
- any development seed passwords you want to use

### 3. Configure the frontend

```bash
cd frontend/HWSMS_UI
cp .env.example .env.development
```

The main frontend variable is:

```env
VITE_API_BASE_URL=http://localhost:5162
```

## Environment Configuration

### Backend

Important backend settings used by the API:

| Setting | Purpose |
|---|---|
| `ConnectionStrings__DefaultConnection` | Primary MySQL connection string |
| `JWT_SECRET` or `Jwt__Secret` | JWT signing secret |
| `JWT_ISSUER` or `Jwt__Issuer` | JWT issuer |
| `JWT_AUDIENCE` or `Jwt__Audience` | JWT audience |
| `JWT_EXPIRY_MINUTES` or `Jwt__AccessTokenExpiryMinutes` | Token expiry |
| `CORS_ORIGINS` | Allowed browser origins |
| `FRONTEND_URL` | Optional frontend URL added to CORS candidates |
| `ASPNETCORE_ENVIRONMENT` | Environment name |
| `ASPNETCORE_URLS` | API listening URLs |
| `LOW_STOCK_THRESHOLD` | Threshold used in inventory/reporting |
| `ADMIN_PASSWORD` | Dev-only admin seed password |
| `MANAGER_PASSWORD` | Dev-only manager seed password |
| `CASHIER_PASSWORD` | Dev-only cashier seed password |

### Notes about backend startup behavior

- The API validates that a database connection string exists at startup.
- In `Production`, JWT secret validation is stricter and missing values stop startup.
- The database initializer runs automatically when the API starts.
- Default users are seeded only in the `Development` environment.
- Development user seeding happens only if `ADMIN_PASSWORD`, `MANAGER_PASSWORD`, and `CASHIER_PASSWORD` are all provided.

### Frontend

The frontend resolves its API base URL in this order:

1. `VITE_API_BASE_URL`
2. legacy `VITE_API_URL`
3. local default `http://localhost:5162` in development
4. deployed backend URL in production

Optional frontend variables:

| Variable | Purpose |
|---|---|
| `VITE_API_BASE_URL` | Base backend URL |
| `VITE_DEBUG` | Optional debug toggle |

Unlike the backend, the frontend does load Vite `.env` files such as `.env.development` automatically.

## Running the Application

### Run the backend

From the `backend` folder:

```bash
dotnet restore
dotnet run --project HSMS.API
```

By default, local development runs on:

- `http://localhost:5162`
- `https://localhost:7111`

Useful backend endpoints:

- Swagger UI: `http://localhost:5162/swagger`
- Health check: `http://localhost:5162/api/health`

### Run the frontend

From `frontend/HWSMS_UI`:

```bash
npm install
npm run dev
```

The Vite development server is typically available at:

`http://localhost:5173`

### Suggested local workflow

Open two terminals:

```bash
cd backend
dotnet run --project HSMS.API
```

```bash
cd frontend/HWSMS_UI
npm install
npm run dev
```

## API Overview

### Authentication

| Method | Endpoint | Purpose |
|---|---|---|
| `POST` | `/api/auth/register` | Register a user and return auth token |
| `POST` | `/api/auth/login` | Authenticate and return auth token |

### Products

| Method | Endpoint | Purpose |
|---|---|---|
| `GET` | `/api/product` or `/api/products` | List products |
| `GET` | `/api/product/{id}` | Get product by ID |
| `GET` | `/api/product/search?query=...&limit=20` | Search products |
| `GET` | `/api/product/inventory` | Inventory view with low-stock flag |
| `GET` | `/api/product/low-stock` | Low-stock products |
| `POST` | `/api/product` | Create product |
| `PUT` | `/api/product/{id}` | Update product |
| `PUT` | `/api/product/{id}/stock` | Update stock only |
| `DELETE` | `/api/product/{id}` | Delete product |

### Suppliers

| Method | Endpoint | Purpose |
|---|---|---|
| `GET` | `/api/suppliers` | List suppliers |
| `POST` | `/api/suppliers` | Create supplier |
| `PUT` | `/api/suppliers/{id}` | Update supplier |
| `DELETE` | `/api/suppliers/{id}` | Delete supplier |

### Sales

| Method | Endpoint | Purpose |
|---|---|---|
| `POST` | `/api/sales` | Create sale |
| `GET` | `/api/sales/history` | List transaction history |
| `GET` | `/api/sales/{saleId}` | Get sale details |
| `GET` | `/api/sales/{saleId}/invoice` | Get invoice data |

### Reports

| Method | Endpoint | Purpose |
|---|---|---|
| `GET` | `/api/reports/daily` | Daily sales report |
| `GET` | `/api/reports/monthly` | Monthly sales report |
| `GET` | `/api/reports/analytics` | Filtered analytics |
| `GET` | `/api/reports/low-stock` | Low-stock report |
| `GET` | `/api/reports/summary` | Aggregated report summary |
| `GET` | `/api/reports/export?type=daily` | CSV export |

### Users

| Method | Endpoint | Purpose |
|---|---|---|
| `GET` | `/api/users` | List users |
| `POST` | `/api/users` | Create user |
| `PUT` | `/api/users/{id}/role` | Update role |
| `PUT` | `/api/users/{id}/password` | Reset password (NEW) |
| `DELETE` | `/api/users/{id}` | Delete user |

## Testing

### Backend tests

From the `backend` directory:

```bash
dotnet test
```

Individual test projects:

```bash
dotnet test HSMS.Tests/HSMS.Tests.csproj
dotnet test HSMS.ApiTests/HSMS.ApiTests.csproj
dotnet test HSMS.E2E/HSMS.E2E.csproj
```

### Frontend tests

From `frontend/HWSMS_UI`:

```bash
npm test
```

Other useful commands:

```bash
npm run build
npm run lint
npm run test:watch
```

### Test coverage references

- [docs/Test-Documents/TESTING_OVERVIEW.md](./docs/Test-Documents/TESTING_OVERVIEW.md)
- [docs/Test-Documents/Guides/HSMS_TESTS_README.md](./docs/Test-Documents/Guides/HSMS_TESTS_README.md)
- [docs/Test-Documents/Guides/HSMS_APITESTS_README.md](./docs/Test-Documents/Guides/HSMS_APITESTS_README.md)

## Documentation

Additional project documentation available in the repository:

- [QUICK_START.md](./QUICK_START.md)
- [DEPLOYMENT_GUIDE.md](./DEPLOYMENT_GUIDE.md)
- [CONFIGURATION_INDEX.md](./CONFIGURATION_INDEX.md)
- [ENV_VARIABLES_CHECKLIST.md](./ENV_VARIABLES_CHECKLIST.md)
- [ENVIRONMENT_VARIABLES_SUMMARY.md](./ENVIRONMENT_VARIABLES_SUMMARY.md)
- [USER_MANAGEMENT_FEATURES.md](./USER_MANAGEMENT_FEATURES.md) - Detailed user management enhancements
- [docs/Test-Documents/TESTING_OVERVIEW.md](./docs/Test-Documents/TESTING_OVERVIEW.md)
- [docs/Test-Documents/API_TEST_PLAN.md](./docs/Test-Documents/API_TEST_PLAN.md)
- [docs/Test-Documents/HSMS_MASTER_TEST_PLAN.md](./docs/Test-Documents/HSMS_MASTER_TEST_PLAN.md)
- [docs/SRS/SRS_Document.pdf](./docs/SRS/SRS_Document.pdf)

## Troubleshooting

### Backend fails on startup

Check:

- MySQL is running
- `ConnectionStrings__DefaultConnection` points to a valid database
- JWT settings are present and valid
- your connection string is not empty

### Frontend cannot call the API

Check:

- backend is running on `http://localhost:5162`
- `VITE_API_BASE_URL` points to the correct backend
- the backend CORS configuration includes your frontend origin

### Default users were not created

This is expected unless all of the following are true:

- `ASPNETCORE_ENVIRONMENT=Development`
- `ADMIN_PASSWORD` is set
- `MANAGER_PASSWORD` is set
- `CASHIER_PASSWORD` is set

### Swagger is not visible

Swagger is enabled only in the `Development` environment in the current API startup configuration.

## Summary

HSMS is a layered full-stack store management application that already includes authentication, role-based access control, reporting, testing, and deployment-oriented configuration support. This repository contains both the working application code and supporting academic documentation for design, testing, and system behavior.
