# Contributing Guide

## Development Environment Setup

### Prerequisites

| Tool | Version | Purpose |
|---|---|---|
| .NET SDK | 8.0.x | Backend build and run |
| Node.js | 20.x LTS | Frontend build and run |
| MySQL Server | 8.0 | Local database |
| Git | Any | Version control |
| Chrome | Latest | Selenium E2E tests (optional) |

---

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/<your-org>/CSP_HWSMS.git
cd CSP_HWSMS
```

### 2. Set Up the Backend

The backend reads configuration from `appsettings.Development.json` in development mode.
The file already contains working defaults for a local MySQL installation:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=CSP_HSMS;Uid=<user>;Pwd=<password>;"
  },
  "Jwt": {
    "Secret": "<at-least-32-bytes>",
    "Issuer": "HSMS.API",
    "Audience": "HSMS.Client",
    "AccessTokenExpiryMinutes": 60
  },
  "Url": {
    "Frontend": "http://localhost:5173",
    "Backend": "https://localhost:7111;http://localhost:5162"
  }
}
```

Update the MySQL credentials to match your local setup, then:

```bash
cd backend
dotnet restore HSMS.sln
dotnet run --project HSMS.API/HSMS.API.csproj
```

The backend will:
1. Validate all required configuration.
2. Auto-create the `CSP_HSMS` database and all tables on first run.
3. Seed `admin`, `manager`, and `cashier` users if `Password:Admin` etc. are set.
4. Start listening on `http://localhost:5162` (and HTTPS on `https://localhost:7111`).

Swagger UI is available at: `http://localhost:5162/swagger`

### 3. Set Up the Frontend

```bash
cd frontend/HWSMS_UI
npm install
```

Create a `.env.development` file (or verify the existing one):

```env
VITE_API_BASE_URL=http://localhost:5162
```

Start the dev server:

```bash
npm run dev
```

Frontend is available at: `http://localhost:5173`

### 4. Optional — Seed Development Data

```bash
# From the project root
node backend/reset-and-seed-db.js
```

This populates the database with realistic sample products, suppliers, sales, and users.

---

## Branching Strategy

| Branch | Purpose |
|---|---|
| `main` | Production-ready code. Protected. Requires PR + CI pass. |
| `feature/<name>` | New features |
| `fix/<name>` | Bug fixes |
| `chore/<name>` | Non-functional changes (deps, docs, config) |

### Creating a feature branch

```bash
git checkout main
git pull origin main
git checkout -b feature/your-feature-name
```

---

## Commit Message Convention

Use a short, imperative present-tense summary:

```
feat: add supplier contact info field
fix: prevent self-deletion in UsersController
chore: update .NET to 8.0.15
docs: add backend configuration reference
test: add SQL injection tests for ProductController
```

Accepted prefixes: `feat`, `fix`, `chore`, `docs`, `test`, `refactor`, `style`, `ci`

---

## Making a Pull Request

1. Ensure all tests pass locally before opening a PR:

   ```bash
   # Backend
   cd backend
   dotnet test HSMS.Tests/HSMS.Tests.csproj -c Release

   # Frontend
   cd frontend/HWSMS_UI
   npm test
   npm run build
   ```

2. Open a PR against `main`.
3. The CI pipeline will automatically run `backend_ci` and `frontend_ci`.
4. A reviewer must approve before merging.
5. Merge is done via **Squash and Merge** to keep a clean `main` history.

---

## Code Style

### Backend (C#)

- Follow the existing file structure: Domain → Application → Infrastructure → API.
- Controllers must **not** contain business logic — delegate to repositories or services.
- All database queries must be **parameterized** — never concatenate user input into SQL.
- All public methods should be `async Task<T>` — never `.Wait()` or `.Result`.
- Use `IActionResult` return types on controllers.
- All new policies must be added to `AuthPolicies.cs` and registered in `Program.cs`.
- XML doc comments (`///`) are required on all public controller methods.

### Frontend (TypeScript / React)

- All service functions must include the `Authorization: Bearer <token>` header via `getAuthHeader()`.
- All user-facing error messages must be caught and displayed — never silently swallow errors.
- TypeScript strict mode is enabled — avoid `any` types.
- Organize new pages in `src/pages/`, new services in `src/services/`, new shared components in `src/components/`.
- All new routes must be added to `App.tsx` with appropriate `ProtectedRoute` / `PublicOnlyRoute` wrappers and `allowedRoles`.

---

## Running Tests Locally

### Backend unit + integration + security tests

```bash
cd backend
dotnet test HSMS.Tests/HSMS.Tests.csproj -c Release --logger "console;verbosity=normal"
```

### Backend API tests (requires live backend)

```bash
# Terminal 1: Start the backend
cd backend
dotnet run --project HSMS.API/HSMS.API.csproj

# Terminal 2: Run API tests
dotnet test backend/HSMS.ApiTests/HSMS.ApiTests.csproj --logger "console;verbosity=normal"
```

### Frontend tests

```bash
cd frontend/HWSMS_UI
npm test
```

### E2E Selenium tests

```bash
export HSMS_E2E_BASE_URL="http://localhost:5173"
export HSMS_E2E_USERNAME="admin"
export HSMS_E2E_PASSWORD="your-dev-admin-password"

dotnet test backend/HSMS.E2E/HSMS.E2E.csproj
```

---

## Project Documentation

Update the relevant documentation file when making changes:

| Change type | Document to update |
|---|---|
| New API endpoint | [`docs/backend_overview.md`](./backend_overview.md) |
| New frontend page or route | [`docs/frontend_overview.md`](./frontend_overview.md) |
| Database schema change | [`docs/database_schema.md`](./database_schema.md) |
| New test suite or coverage change | [`docs/testing_strategy.md`](./testing_strategy.md) |
| CI/CD pipeline change | [`docs/ci_cd_pipeline.md`](./ci_cd_pipeline.md) |
| New environment variable | [`ENVIRONMENT_VARIABLES_SUMMARY.md`](../ENVIRONMENT_VARIABLES_SUMMARY.md), [`ENV_VARIABLES_CHECKLIST.md`](../ENV_VARIABLES_CHECKLIST.md), [`docs/backend_overview.md`](./backend_overview.md) |

---

## File Structure Reference

```
CSP_HWSMS/
├── .github/
│   └── workflows/
│       └── ci-cd.yml              ← CI/CD pipeline definition
├── backend/
│   ├── HSMS.sln
│   ├── HSMS.API/                  ← API layer (controllers, auth, middleware)
│   ├── HSMS.Application/          ← Interfaces, DTOs, business logic
│   ├── HSMS.Domain/               ← Entity classes
│   ├── HSMS.Infrastructure/       ← Repositories, DatabaseInitializer
│   ├── HSMS.Tests/                ← Unit + Integration + Security tests
│   ├── HSMS.ApiTests/             ← Live HTTP integration tests
│   └── HSMS.E2E/                  ← Selenium browser tests
├── frontend/
│   └── HWSMS_UI/                  ← React 19 + Vite + TypeScript + Tailwind SPA
├── docs/
│   ├── Diagrams/                  ← All architecture, ER, sequence, deployment diagrams
│   ├── SRS/                       ← Software Requirements Specification
│   ├── Test-Documents/            ← Test plans, test overview, guides
│   ├── architecture.md
│   ├── backend_overview.md
│   ├── frontend_overview.md
│   ├── database_schema.md
│   ├── testing_strategy.md
│   ├── ci_cd_pipeline.md
│   ├── maintenance.md
│   └── contributing.md
├── jmeter/                        ← JMeter load test plan
├── postman/                       ← Postman collection + environment
├── scripts/                       ← Code generation and DB utility scripts
├── README.md                      ← Project entry point
├── QUICK_START.md                 ← Fast-track local setup
├── DEPLOYMENT_GUIDE.md            ← Azure deployment steps
├── ENVIRONMENT_VARIABLES_SUMMARY.md
└── ENV_VARIABLES_CHECKLIST.md
```
