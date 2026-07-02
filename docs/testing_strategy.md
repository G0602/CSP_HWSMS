# Testing Strategy

## Overview

HSMS uses a **multi-layer automated testing strategy** that covers unit logic, API contracts,
security boundaries, and end-to-end user flows. All tests (except E2E) are executed automatically
in the GitHub Actions CI pipeline on every push and pull request to `main`.

---

## Test Suite Summary

| Suite | Project | Runner | Count | Runs in CI |
|---|---|---|---|---|
| Unit + Integration + Security tests | `HSMS.Tests` | xUnit | **296** | ✅ |
| HTTP API integration tests | `HSMS.ApiTests` | xUnit | **194** | ✅ |
| Frontend component / service tests | `HWSMS_UI` (Vitest) | Vitest | **17** | ✅ |
| Selenium browser E2E tests | `HSMS.E2E` | xUnit + Selenium | 3 | ⚠️ Manual only |
| JMeter load tests | `jmeter/` | Apache JMeter | — | ⚠️ Manual only |
| Postman collection | `postman/` | Newman / Postman | — | ⚠️ Manual only |

---

## Layer 1 — `HSMS.Tests` (xUnit)

**Project path:** `backend/HSMS.Tests/`

This project is split into three sub-namespaces that run together under a single `dotnet test` command.

### 1a. Unit Tests (`Unit/`)

Pure unit tests using **Moq** for dependency mocking. No database or network required.

| File | What is tested |
|---|---|
| `Unit/Services/AuthenticationServiceTests.cs` | `AuthenticationService.LoginAsync()` — valid credentials, wrong password, missing user, empty input |
| `Unit/Services/JwtTokenServiceTests.cs` | `JwtTokenService.GenerateToken()` — claims, expiry, signature, token structure |
| `Unit/Services/SaleCalculatorTests.cs` | `SaleCalculator.CalculateSubtotal()` — price × qty, zero handling |
| `Unit/Services/DecimalPrecisionTests.cs` | Decimal arithmetic precision for financial calculations |
| `Unit/Controllers/` | Controller logic tests (business rule validation, error responses) |
| `Unit/Validation/` | Input validation edge cases |

### 1b. Integration Tests (`Integration/`)

Tests that exercise repository implementations against an in-memory / test database context.

| Folder | What is tested |
|---|---|
| `Integration/Database/` | `DatabaseInitializer` — idempotent schema creation, column migration |
| `Integration/Repositories/` | `ProductRepository`, `SaleRepository`, `SupplierRepository`, `UserRepository` — real SQL logic |

### 1c. Security Tests (`Security/`)

Focused security regression tests.

| File | What is tested |
|---|---|
| `Security/AuthorizationTests.cs` | Role-based policy enforcement — correct roles can access, wrong roles get 403 |
| `Security/AuthSecurityTests.cs` | Auth edge cases — expired token, invalid signature, missing token |
| `Security/CrossUserAuthorizationTests.cs` | Cross-user data isolation — user A cannot modify user B's data |
| `Security/SqlInjectionProtectionTests.cs` | Parameterized query protection — SQL injection payloads in all user-input fields |
| `Security/TokenExpirationTests.cs` | Token expiry enforcement — expired tokens are rejected |

### Running

```bash
# Run all HSMS.Tests
dotnet test backend/HSMS.Tests/HSMS.Tests.csproj -c Release

# Run with code coverage
dotnet test backend/HSMS.Tests/HSMS.Tests.csproj \
  -c Release \
  --collect:"XPlat Code Coverage" \
  --settings backend/HSMS.Tests/coverlet.runsettings \
  --logger "trx;LogFileName=backend-tests.trx"
```

**Coverage settings** (`coverlet.runsettings`): Excludes generated code and test projects from the coverage report. Outputs `coverage.cobertura.xml`.

---

## Layer 2 — `HSMS.ApiTests` (Live HTTP Tests)

**Project path:** `backend/HSMS.ApiTests/`

These are **real HTTP integration tests** that send actual HTTP requests to a running instance of the
backend API. The API is started as a background process in the CI pipeline before these tests run.

### Architecture

- All tests use `HttpClient` configured to point at `http://127.0.0.1:5162` (or the URL from config).
- `AssemblyInfo.cs` sets `[assembly: CollectionBehavior(DisableTestParallelization = true)]` — tests run sequentially to avoid race conditions on shared database state.
- The `Helpers/` folder provides shared helpers for authentication, HTTP setup, and common assertions.

### Test Folders

| Folder | Tests cover |
|---|---|
| `Auth/AuthApiTests.cs` | Login happy path — valid credentials return a valid JWT |
| `Auth/AuthApiNegativeTests.cs` | Login failure cases — wrong password, unknown user, malformed body |
| `Auth/AuthRegisterTests.cs` | `POST /api/auth/register` always returns 403 |
| `Products/` | Full CRUD for products — create, read, update, delete, inventory view, stock update |
| `Suppliers/` | Full CRUD for suppliers — including conflict on duplicate name, blocked delete |
| `Sales/` | Sale creation, history retrieval, invoice retrieval |
| `Reports/` | Daily report, monthly report, analytics (with date filters), summary, low-stock, CSV exports |
| `Users/` | User creation, role change, password reset, deletion, self-delete prevention |

### How the CI pipeline runs these

```yaml
# 1. Backend is started as a background process
dotnet run --project HSMS.API/HSMS.API.csproj -c Release --no-build --no-launch-profile &

# 2. CI waits for the health endpoint to respond (up to 60 seconds)
for i in {1..30}; do
  if curl --fail --silent http://127.0.0.1:5162/api/health; then exit 0; fi
  sleep 2
done

# 3. API tests are run against the live server
dotnet test backend/HSMS.ApiTests/HSMS.ApiTests.csproj -c Release --no-build
```

### Data Isolation

The CI pipeline uses an ephemeral `hsms_test` MySQL database (Docker service container) that is
created fresh for each pipeline run. The backend seeds default `admin`, `manager`, and `cashier`
users automatically because `ASPNETCORE_ENVIRONMENT=Integration`.

### Running Locally

You must have the backend running before running API tests locally:

```bash
# Terminal 1: Start the backend
cd backend
dotnet run --project HSMS.API/HSMS.API.csproj

# Terminal 2: Run API tests
dotnet test backend/HSMS.ApiTests/HSMS.ApiTests.csproj
```

---

## Layer 3 — Frontend Tests (Vitest)

**Project path:** `frontend/HWSMS_UI/`

Tests use **Vitest** as the runner and **@testing-library/react** for component rendering.

### Test Files

| File | Tests cover |
|---|---|
| `src/services/authService.test.ts` | `login()`, `logout()`, `isAuthenticated()`, `persistSession()`, token expiry logic |
| Other files in `src/test/` | Key React component rendering and interaction |

### Running

```bash
cd frontend/HWSMS_UI
npm test          # Single run (used in CI)
npm run test:watch  # Watch mode (used in development)
```

---

## Layer 4 — E2E Tests (Selenium) — Manual Only

**Project path:** `backend/HSMS.E2E/`

Browser-level end-to-end tests using **Selenium WebDriver** with Chrome in headless mode.

> **Note:** These tests are **not executed in the CI pipeline**. They skip automatically if the required
> environment variables are not set.

### Environment Variables Required

| Variable | Description |
|---|---|
| `HSMS_E2E_BASE_URL` | Frontend URL, e.g. `https://hwsms.z13.web.core.windows.net` |
| `HSMS_E2E_USERNAME` | Login username for the test user |
| `HSMS_E2E_PASSWORD` | Login password for the test user |

If any of these is unset, each test method calls `CreateDriverIfConfigured()` which returns `null`,
and the test body returns early — effectively **skipping** without failure.

### Test Scenarios

| Test | Description |
|---|---|
| `LoginFlow_Should_Navigate_To_Authorized_Landing_Page` | Full login → redirected away from `/login` |
| `SalesFlow_Should_Show_Sales_Workspace_And_Empty_Cart_State` | Navigate to `/sales` → verify Sales workspace + Cart visible |
| `ReportViewingFlow_Should_Render_Analytics_Dashboard` | Navigate to `/reports/daily` → verify Analytics Dashboard renders |

### Running Manually

```bash
export HSMS_E2E_BASE_URL="http://localhost:5173"
export HSMS_E2E_USERNAME="admin"
export HSMS_E2E_PASSWORD="your-admin-password"

dotnet test backend/HSMS.E2E/HSMS.E2E.csproj
```

---

## Layer 5 — Load Tests (JMeter) — Manual Only

**Plan file:** `jmeter/HSMS_API_100_users.jmx`
**Properties:** `jmeter/hsms-jmeter.properties`

Simulates **100 concurrent users** performing common API operations. Used for performance
benchmarking and identifying bottlenecks under load.

### Running

```bash
jmeter -n \
  -t jmeter/HSMS_API_100_users.jmx \
  -p jmeter/hsms-jmeter.properties \
  -l results.jtl \
  -e -o jmeter-report/
```

---

## Layer 6 — Postman Collection — Manual Only

**Collection:** `postman/HSMS_API.postman_collection.json`
**Environment:** `postman/HSMS_Local.postman_environment.json`

A complete Postman collection covering all API endpoints. Used for interactive manual testing
and sharing with team members. Can also be run via Newman for CI integration.

### Running with Newman

```bash
npm install -g newman
newman run postman/HSMS_API.postman_collection.json \
  -e postman/HSMS_Local.postman_environment.json
```

---

## Code Coverage

Code coverage is collected during the `HSMS.Tests` run using **coverlet** with the
`XPlat Code Coverage` data collector.

- Output format: **Cobertura XML** (`coverage.cobertura.xml`)
- Uploaded as a CI artifact: `backend-test-results`
- Settings file: `backend/HSMS.Tests/coverlet.runsettings`

---

## Test Artifacts in CI

The `Upload backend test artifacts` step always runs (even on failure) and uploads:

- `backend/**/TestResults/**/coverage.cobertura.xml` — coverage report
- `backend/**/TestResults/**/*.trx` — test result XML files
- `$RUNNER_TEMP/hsms-api.log` — backend stdout/stderr during API tests (critical for debugging failures)

These artifacts are available in the GitHub Actions run summary under the name `backend-test-results`.

---

## Further Reading

- [docs/Test-Documents/HSMS_MASTER_TEST_PLAN.md](./Test-Documents/HSMS_MASTER_TEST_PLAN.md)
- [docs/Test-Documents/API_TEST_PLAN.md](./Test-Documents/API_TEST_PLAN.md)
- [docs/Test-Documents/TESTING_OVERVIEW.md](./Test-Documents/TESTING_OVERVIEW.md)
- [docs/Test-Documents/JMETER_LOAD_TEST.md](./Test-Documents/JMETER_LOAD_TEST.md)
- [docs/Test-Documents/Guides/TEST_EXECUTION_GUIDE.md](./Test-Documents/Guides/TEST_EXECUTION_GUIDE.md)
