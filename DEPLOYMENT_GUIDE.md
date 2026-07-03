# HWSMS Deployment Guide

This guide describes the current deployment model for this repository.

## Deployment Model

The project is deployed as three containerised services, with images stored in **Azure Container Registry (ACR)**:

| Service | Image | Serves |
|---|---|---|
| Backend | `hsmsdocker.azurecr.io/hsms-backend` | ASP.NET Core 8 REST API |
| Frontend | `hsmsdocker.azurecr.io/hsms-frontend` | React/Vite SPA via Nginx |
| Database | _(external Azure MySQL server)_ | MySQL — not containerised |

CI/CD is handled through GitHub Actions (`.github/workflows/ci-cd.yml`).

---

## CI/CD Workflow

The active pipeline is [`.github/workflows/ci-cd.yml`](./.github/workflows/ci-cd.yml).

### Job graph

```
PR / push to main
├── backend_ci          Build · unit tests · API tests
├── frontend_ci         Install · vitest · Vite build
│
push to main only (after CI passes):
├── build_push_backend  docker build → ACR  hsms-backend:<sha> + latest
├── build_push_frontend docker build → ACR  hsms-frontend:<sha> + latest
│
└── smoke_test          GET /api/health · frontend availability check
└── notify_failure      Slack on any failure
```

### Trigger

| Event | CI jobs | Build & push | Smoke test |
|---|---|---|---|
| Pull request → `main` | ✅ | ❌ | ❌ |
| Push → `main` | ✅ | ✅ | ✅ |
| Manual (`workflow_dispatch`) | ✅ | ❌ | ❌ |

---

## Docker Images

### Backend (`backend/Dockerfile`)

Multi-stage build:
1. `build` — `mcr.microsoft.com/dotnet/sdk:8.0-alpine`, runs `dotnet publish`
2. `final` — `mcr.microsoft.com/dotnet/aspnet:8.0-alpine`, minimal runtime

Port: **8080** inside the container.

### Frontend (`frontend/HWSMS_UI/Dockerfile`)

Multi-stage build:
1. `deps` — `node:20-alpine`, installs production dependencies
2. `build` — `node:20-alpine`, runs `tsc + vite build` (accepts `VITE_API_BASE_URL` build arg)
3. `final` — `nginx:1.27-alpine`, serves `dist/` on port **80** with SPA routing

Port: **80** inside the container.

> **Important**: `VITE_API_BASE_URL` must be supplied at image build time because Vite bakes
> environment variables into the JavaScript bundle. In CI this is passed via `vars.VITE_API_BASE_URL`.

---

## Local Development (Docker Compose)

The root [`docker-compose.yml`](./docker-compose.yml) brings up the full stack locally:

```bash
# 1. Copy and fill in local env values
cp .env.example .env   # see table below

# 2. Start all three services
docker compose up --build

# Services:
#   Frontend  → http://localhost:80
#   Backend   → http://localhost:8080
#   MySQL     → localhost:3306 (local dev DB only)
```

### Local `.env` variables

| Variable | Example | Purpose |
|---|---|---|
| `DB_ROOT_PASSWORD` | `rootpass` | MySQL root password (local only) |
| `DB__NAME` | `hsms` | Database name |
| `DB__USER` | `hsmsuser` | DB username |
| `DB__PASSWORD` | `hsmspass` | DB password |
| `JWT__SECRET` | _(32+ chars)_ | JWT signing secret |
| `JWT__ISSUER` | `hsms` | JWT issuer |
| `JWT__AUDIENCE` | `hsms-clients` | JWT audience |
| `VITE_API_BASE_URL` | `http://localhost:8080` | Backend URL baked into frontend |
| `URL__BACKEND` | `http://localhost:8080` | Backend origin for CORS |
| `URL__FRONTEND` | `http://localhost:80` | Frontend origin for CORS |
| `Password__Admin` | _(strong)_ | Initial admin seed password |
| `Password__Manager` | _(strong)_ | Initial manager seed password |
| `Password__Cashier` | _(strong)_ | Initial cashier seed password |

---

## GitHub Secrets and Variables

### Variables (Repository → Settings → Variables → Actions)

| Name | Example value | Used by |
|---|---|---|
| `ACR_LOGIN_SERVER` | `hsmsdocker.azurecr.io` | All build/push jobs |
| `VITE_API_BASE_URL` | `https://your-api.azurewebsites.net` | Frontend build arg |

### Secrets (Repository → Settings → Secrets → Actions)

| Name | Purpose |
|---|---|
| `ACR_USERNAME` | ACR admin username |
| `ACR_PASSWORD` | ACR admin password |
| `JWT__SECRET` | JWT signing secret (≥ 32 bytes) |
| `Password__Admin` | Seed admin password |
| `Password__Manager` | Seed manager password |
| `Password__Cashier` | Seed cashier password |
| `Url__Backend` | Live backend URL for smoke test |
| `Url__Frontend` | Live frontend URL for smoke test |
| `SLACK_WEBHOOK_URL` | _(optional)_ Slack failure notifications |

### Secrets that are now unused (safe to remove)

| Name | Reason |
|---|---|
| `AZURE_WEBAPP_PUBLISH_PROFILE` | Replaced by Docker image push |
| `AZURE_STATIC_WEB_APP_TOKEN` | Static Web Apps deployment removed |

---

## Backend Runtime Configuration

The backend container reads all configuration from environment variables.

| Variable | Purpose |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | Use `Production` in production |
| `ASPNETCORE_URLS` | Bind address, e.g. `http://+:8080` |
| `DB__HOST` | MySQL server host |
| `DB__PORT` | MySQL port (default `3306`) |
| `DB__NAME` | Database name |
| `DB__USER` | Database username |
| `DB__PASSWORD` | Database password |
| `JWT__SECRET` | JWT signing secret (≥ 32 bytes) |
| `JWT__ISSUER` | JWT issuer |
| `JWT__AUDIENCE` | JWT audience |
| `JWT__ACCESS_TOKEN_EXPIRY_MINUTES` | Token lifetime |
| `URL__FRONTEND` | Allowed CORS origins (frontend URL) |
| `URL__BACKEND` | Backend origin |
| `Password__Admin` | Admin seed password |
| `Password__Manager` | Manager seed password |
| `Password__Cashier` | Cashier seed password |

---

## Deploying the Containers (Manual / Post-CI)

After the CI/CD pipeline pushes images to ACR, pull and run them on your target host.

### Pull from ACR

```bash
docker login hsmsdocker.azurecr.io \
  --username <ACR_USERNAME> \
  --password <ACR_PASSWORD>

docker pull hsmsdocker.azurecr.io/hsms-backend:latest
docker pull hsmsdocker.azurecr.io/hsms-frontend:latest
```

### Run backend

```bash
docker run -d \
  --name hsms_backend \
  -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ASPNETCORE_URLS="http://+:8080" \
  -e DB__HOST=<your-mysql-host> \
  -e DB__PORT=3306 \
  -e DB__NAME=<db-name> \
  -e DB__USER=<db-user> \
  -e DB__PASSWORD=<db-password> \
  -e JWT__SECRET=<jwt-secret> \
  -e JWT__ISSUER=hsms \
  -e JWT__AUDIENCE=hsms-clients \
  -e JWT__ACCESS_TOKEN_EXPIRY_MINUTES=60 \
  -e URL__FRONTEND=https://<your-frontend-url> \
  -e URL__BACKEND=https://<your-backend-url> \
  hsmsdocker.azurecr.io/hsms-backend:latest
```

### Run frontend

```bash
# Note: VITE_API_BASE_URL is baked in at build time.
# The image already contains the correct backend URL.
docker run -d \
  --name hsms_frontend \
  -p 80:80 \
  hsmsdocker.azurecr.io/hsms-frontend:latest
```

---

## Smoke Verification

```bash
# Backend health (expects {"status":"healthy","mysql":{"status":"healthy"}})
curl https://your-api.example.com/api/health

# Frontend (expects HTML containing <div id="root">)
curl https://your-frontend.example.com/
```

---

## Troubleshooting

### Backend container exits immediately

Check logs: `docker logs hsms_backend`
- Missing required env vars (`DB__HOST`, `JWT__SECRET`, etc.)
- MySQL not reachable from the container network

### Frontend shows blank page or 404

- Check that `VITE_API_BASE_URL` was set correctly at **image build time** (it's a build arg, not a runtime env var)
- Confirm Nginx config has `try_files $uri /index.html` for SPA routing

### CORS errors in browser

Check:
- `URL__FRONTEND` on the backend matches the exact frontend origin (no trailing slash)
- The frontend is calling the correct backend URL

### ACR push fails in CI

- Confirm `ACR_LOGIN_SERVER` variable and `ACR_USERNAME`/`ACR_PASSWORD` secrets are set in the GitHub repository
- Confirm the ACR admin account is enabled: Azure Portal → Container Registry → Access keys → Admin user = enabled

---

## Related Docs

- [README.md](./README.md)
- [QUICK_START.md](./QUICK_START.md)
- [CONFIGURATION_INDEX.md](./CONFIGURATION_INDEX.md)
- [ENVIRONMENT_VARIABLES_SUMMARY.md](./ENVIRONMENT_VARIABLES_SUMMARY.md)
- [ENV_VARIABLES_CHECKLIST.md](./ENV_VARIABLES_CHECKLIST.md)
