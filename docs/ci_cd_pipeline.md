# CI/CD Pipeline

## Overview

HSMS uses **GitHub Actions** for continuous integration and deployment.
The pipeline is defined in a single workflow file:
[`.github/workflows/ci-cd.yml`](../.github/workflows/ci-cd.yml)

The pipeline has **6 jobs** that run in a dependency-ordered sequence:

```
backend_ci ──────────────┐
                         ├──► deploy_backend ──►
frontend_ci ─────────────┘                      smoke_test
                         ├──► deploy_frontend ──►
                         │
                         └──► notify_failure (on any failure)
```

---

## Triggers

| Event | Jobs triggered |
|---|---|
| `push` to `main` | All 6 jobs (CI + deploy + smoke test) |
| `pull_request` to `main` | `backend_ci` + `frontend_ci` only |
| `workflow_dispatch` (manual) | All 6 jobs |

**Concurrency control:** Only one pipeline run per branch/workflow combination is active at a time.
New runs cancel any in-progress run for the same branch (`cancel-in-progress: true`).

---

## Job 1 — `backend_ci` (Backend CI)

**Runs on:** `ubuntu-latest` · **Timeout:** 30 minutes

### MySQL Service Container

A fresh MySQL 8.0 Docker container is spun up as a GitHub Actions *service* for this job:

```yaml
services:
  mysql:
    image: mysql:8.0
    env:
      MYSQL_ROOT_PASSWORD: root
      MYSQL_DATABASE: hsms_test
    ports:
      - 3306:3306
    options: --health-cmd="mysqladmin ping -h 127.0.0.1 -proot --silent" ...
```

The container is destroyed when the job ends. This ensures full test isolation between runs.

### Environment Variables injected

| Variable | Source | Purpose |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | Hardcoded: `Integration` | Enables dev seeding, disables production-only guards |
| `ASPNETCORE_URLS` | Hardcoded: `http://127.0.0.1:5162` | Binds the API to a known port |
| `DB__HOST` | Hardcoded: `127.0.0.1` | Points to the MySQL service container |
| `DB__PORT` | Hardcoded: `3306` | MySQL port |
| `DB__NAME` | Hardcoded: `hsms_test` | Test database name |
| `DB__USER` | Hardcoded: `root` | MySQL user |
| `DB__PASSWORD` | Hardcoded: `root` | MySQL password |
| `JWT__SECRET` | GitHub Secret: `JWT__SECRET` | JWT signing key |
| `JWT__ISSUER` | Hardcoded: `hsms-ci` | CI issuer claim |
| `JWT__AUDIENCE` | Hardcoded: `hsms-ci-clients` | CI audience claim |
| `JWT__ACCESS_TOKEN_EXPIRY_MINUTES` | Hardcoded: `60` | Token TTL |
| `URL__BACKEND` | Hardcoded: `http://127.0.0.1:5162` | CORS origin for backend |
| `Password__Admin` | GitHub Secret | Seeds the `admin` user at startup |
| `Password__Manager` | GitHub Secret | Seeds the `manager` user at startup |
| `Password__Cashier` | GitHub Secret | Seeds the `cashier` user at startup |

### Steps

| # | Step | Command |
|---|---|---|
| 1 | Checkout | `actions/checkout@v4` |
| 2 | Setup .NET 8 | `actions/setup-dotnet@v4` (version 8.0.x) |
| 3 | Cache NuGet packages | `actions/cache@v4` (keyed by `.csproj` + `.sln` hash) |
| 4 | Restore | `dotnet restore backend/HSMS.sln` |
| 5 | Build | `dotnet build backend/HSMS.sln -c Release --no-restore` |
| 6 | Run unit/integration/security tests | `dotnet test backend/HSMS.Tests/HSMS.Tests.csproj -c Release --no-build --collect:"XPlat Code Coverage"` |
| 7 | Start backend API (background) | `dotnet run --project HSMS.API/HSMS.API.csproj -c Release --no-build --no-launch-profile &` |
| 8 | Wait for health endpoint | `curl http://127.0.0.1:5162/api/health` — polls every 2s, up to 60s |
| 9 | Run API tests | `dotnet test backend/HSMS.ApiTests/HSMS.ApiTests.csproj -c Release --no-build` |
| 10 | Upload test artifacts | Uploads `.trx` + `coverage.cobertura.xml` + `hsms-api.log` |

### How the API receives environment variables

ASP.NET Core automatically reads process environment variables. When `dotnet run` starts the API
as a child process of the GitHub Actions runner, **all `env:` variables declared in the job are
inherited** by the child process. Double-underscore (`__`) in env var names is mapped to colon (`:`)
in configuration paths by the ASP.NET Core configuration system.

---

## Job 2 — `frontend_ci` (Frontend CI)

**Runs on:** `ubuntu-latest` · **Timeout:** 20 minutes

### Steps

| # | Step | Command |
|---|---|---|
| 1 | Checkout | `actions/checkout@v4` |
| 2 | Setup Node.js 20 | `actions/setup-node@v4` (with npm cache on `package-lock.json`) |
| 3 | Install dependencies | `npm ci` (in `frontend/HWSMS_UI`) |
| 4 | Run frontend tests | `npm test` (runs `vitest run`) |
| 5 | Build frontend | `npm run build` (with `VITE_API_BASE_URL=${{ vars.VITE_API_BASE_URL }}`) |
| 6 | Upload build artifact | Uploads `frontend/HWSMS_UI/dist` as `frontend-build` |

> `VITE_API_BASE_URL` is a **GitHub Actions Variable** (not a secret) — it is the public production
> backend URL that gets baked into the compiled JavaScript bundle.

---

## Job 3 — `deploy_backend`

**Runs on:** `ubuntu-latest` · **Timeout:** 20 minutes
**Needs:** `backend_ci` + `frontend_ci`
**Condition:** Only on `push` to `main` (not on PRs)
**Environment:** `production` (requires GitHub environment approval if configured)

### Steps

| # | Step | Command |
|---|---|---|
| 1 | Checkout | `actions/checkout@v4` |
| 2 | Setup .NET 8 | `actions/setup-dotnet@v4` |
| 3 | Restore | `dotnet restore backend/HSMS.sln` |
| 4 | Publish | `dotnet publish backend/HSMS.API/HSMS.API.csproj -c Release -o $RUNNER_TEMP/backend_publish` |
| 5 | Deploy to Azure | `azure/webapps-deploy@v3` with `AZURE_BACKEND_APP_NAME` + `AZURE_WEBAPP_PUBLISH_PROFILE` |

**Azure App Service** runtime environment variables (JWT, database, CORS URLs) must be configured
separately in the Azure Portal or via Azure CLI. They are **not** set by the pipeline — the pipeline
only deploys the compiled application binary.

---

## Job 4 — `deploy_frontend`

**Runs on:** `ubuntu-latest` · **Timeout:** 20 minutes
**Needs:** `backend_ci` + `frontend_ci`
**Condition:** Only on `push` to `main`

### Steps

| # | Step | Command |
|---|---|---|
| 1 | Download artifact | Downloads `frontend-build` (the `dist/` folder from Job 2) |
| 2 | Deploy to Azure Static Web Apps | `Azure/static-web-apps-deploy@v1` with `AZURE_STATIC_WEB_APP_TOKEN` |

The frontend is deployed as a static site. Azure Static Web Apps handles HTTPS, CDN, and SPA
routing fallback automatically.

> **Note:** `VITE_API_BASE_URL` is already baked into the `dist/` bundle from the build step in Job 2.
> It cannot be changed at this stage.

---

## Job 5 — `smoke_test`

**Runs on:** `ubuntu-latest` · **Timeout:** 10 minutes
**Needs:** `deploy_backend` + `deploy_frontend`
**Condition:** Only on `push` to `main`

Verifies that both the backend and frontend are live and healthy after deployment.

### Backend health check

- Polls `${Url__Backend}/api/health` up to 20 times (every 10 seconds = max 3.3 min).
- Asserts the JSON response contains:
  - `"status":"healthy"`
  - `"mysql"` key in checks
  - `"status":"healthy","description":"Database connection is available."`

### Frontend availability check

- Fetches the root URL `${Url__Frontend}/` up to 20 times (every 10 seconds).
- Asserts the HTML response contains `<div id="root"></div>` (React mount point).

**Secrets required:**

| Secret | Description |
|---|---|
| `Url__Backend` | Production backend URL |
| `Url__Frontend` | Production frontend URL |

---

## Job 6 — `notify_failure`

**Runs on:** `ubuntu-latest`
**Condition:** `always() && contains(needs.*.result, 'failure')` — runs if ANY upstream job fails.

### Actions

1. Writes a failure summary to the GitHub Actions step summary (`$GITHUB_STEP_SUMMARY`).
2. If `SLACK_WEBHOOK_URL` secret is configured, sends a Slack notification with the failing workflow run URL.

---

## Required GitHub Secrets

| Secret | Used in | Description |
|---|---|---|
| `JWT__SECRET` | `backend_ci` | JWT signing secret for test API |
| `Password__Admin` | `backend_ci` | Admin seed password for integration tests |
| `Password__Manager` | `backend_ci` | Manager seed password |
| `Password__Cashier` | `backend_ci` | Cashier seed password |
| `AZURE_WEBAPP_PUBLISH_PROFILE` | `deploy_backend` | Azure App Service publish profile XML |
| `AZURE_STATIC_WEB_APP_TOKEN` | `deploy_frontend` | Azure Static Web Apps deployment token |
| `Url__Backend` | `smoke_test` | Production backend URL |
| `Url__Frontend` | `smoke_test` | Production frontend URL |
| `SLACK_WEBHOOK_URL` | `notify_failure` | (Optional) Slack incoming webhook URL |

## Required GitHub Variables (non-secret)

| Variable | Used in | Description |
|---|---|---|
| `VITE_API_BASE_URL` | `frontend_ci` | Backend API URL baked into the frontend bundle |
| `AZURE_BACKEND_APP_NAME` | `deploy_backend` | Azure App Service app name |

---

## Full Pipeline Flow Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│ Trigger: push to main / PR to main / manual dispatch               │
└────────────────────────┬───────────────────────────────────────────┘
                         │
          ┌──────────────┴──────────────┐
          ▼                             ▼
  ┌───────────────┐             ┌───────────────┐
  │  backend_ci   │             │  frontend_ci  │
  │               │             │               │
  │ 1. Restore    │             │ 1. npm ci     │
  │ 2. Build      │             │ 2. npm test   │
  │ 3. Unit tests │             │ 3. npm build  │
  │ 4. Start API  │             │ 4. Upload dist│
  │ 5. API tests  │             └───────┬───────┘
  │ 6. Upload     │                     │
  └───────┬───────┘                     │
          │                             │
          └──────────────┬──────────────┘
       (only on push to main)
                         │
          ┌──────────────┴──────────────┐
          ▼                             ▼
  ┌───────────────┐             ┌───────────────┐
  │deploy_backend │             │deploy_frontend│
  │               │             │               │
  │ dotnet publish│             │ Download dist │
  │ → Azure App   │             │ → Azure SWA   │
  │   Service     │             └───────┬───────┘
  └───────┬───────┘                     │
          └──────────────┬──────────────┘
                         ▼
                 ┌───────────────┐
                 │  smoke_test   │
                 │               │
                 │ curl /health  │
                 │ curl frontend │
                 └───────┬───────┘
                         │ (on any failure)
                         ▼
                 ┌───────────────┐
                 │notify_failure │
                 │               │
                 │ GitHub summary│
                 │ Slack alert   │
                 └───────────────┘
```
