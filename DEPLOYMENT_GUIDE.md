# HWSMS Deployment Guide

This guide describes the current deployment model represented in this repository.

## Deployment Model

The project is deployed as two parts:

1. ASP.NET Core backend API hosted separately
2. React/Vite frontend built to static files and deployed separately

The repo currently does not use root-level Docker Compose deployment files. CI/CD is handled through GitHub Actions.

## CI/CD Workflow

The active pipeline is [`.github/workflows/ci-cd.yml`](./.github/workflows/ci-cd.yml).

It currently performs:

- backend restore, build, and test execution
- backend API startup and API test execution
- frontend dependency install, test run, and production build
- backend deployment to Azure App Service on push to `main`
- frontend deployment to Azure Static Web Apps on push to `main`
- post-deploy smoke checks
- optional Slack failure notification when `SLACK_WEBHOOK_URL` is configured

## Backend Deployment

### Required runtime settings

| Setting | Purpose |
|---|---|
| `ConnectionStrings__DefaultConnection` | MySQL connection string (or use individual Db settings below) |
| `Jwt__Secret` | JWT signing secret (minimum 32 bytes) |
| `Jwt__Issuer` | JWT issuer |
| `Jwt__Audience` | JWT audience |
| `Jwt__AccessTokenExpiryMinutes` | Token expiry lifetime in minutes |
| `Url__Frontend` | Allowed browser frontend origin(s) for CORS |
| `Url__Backend` | Backend API origin(s) |
| `ASPNETCORE_ENVIRONMENT` | Use `Production` in production |

Useful optional individual database credentials settings (fallback when `ConnectionStrings__DefaultConnection` is not set):

| Setting | Purpose |
|---|---|
| `Db__Host` | Database server host |
| `Db__Port` | Database server port (default: 3306) |
| `Db__Name` | Database name |
| `Db__User` | Database user username |
| `Db__Password` | Database user password |
| `LOW_STOCK_THRESHOLD` | Reporting threshold |
| `ASPNETCORE_URLS` | Explicit bind address |

### Publish

```bash
cd backend
dotnet restore
dotnet publish HSMS.API/HSMS.API.csproj -c Release -o ./publish
```

### Run

```bash
cd backend/publish
ASPNETCORE_ENVIRONMENT=Production dotnet HSMS.API.dll
```

### Production notes

- Use a strong JWT secret of at least 32 bytes.
- Do not rely on development user seeding in production.
- Store secrets in the host platform configuration, not in source control.
- Set `CORS_ORIGINS` explicitly to real frontend origins.

## Frontend Deployment

### Configure the production API URL

```bash
cd frontend/HWSMS_UI
cp .env.example .env.production
```

Example:

```env
VITE_API_BASE_URL=https://your-api.example.com
VITE_DEBUG=false
```

### Build

```bash
cd frontend/HWSMS_UI
npm install
npm run build
```

Deployable output:

- `frontend/HWSMS_UI/dist`

### Supported hosting pattern

The built app is suitable for:

- Azure Static Web Apps
- Vercel
- Netlify
- Nginx / Apache
- other static hosts with SPA fallback support

## GitHub Secrets and Variables

### Secrets

- `AZURE_WEBAPP_PUBLISH_PROFILE`
- `AZURE_STATIC_WEB_APP_TOKEN`
- `SLACK_WEBHOOK_URL` optional

### Variables

- `AZURE_BACKEND_APP_NAME`
- `BACKEND_PUBLIC_URL`
- `FRONTEND_PUBLIC_URL`
- `VITE_API_BASE_URL`

## Smoke Verification

Backend:

```bash
curl https://your-api.example.com/api/health
```

Frontend:

- confirm the site loads successfully
- confirm the app root renders
- confirm login requests target the correct backend URL

## Troubleshooting

### Backend starts locally but fails in production

Check:

- `ConnectionStrings__DefaultConnection` or individual database parameters (`Db__Host`, `Db__Port`, etc.)
- JWT settings (`Jwt__Issuer`, `Jwt__Audience`, `Jwt__Secret`)
- host bind settings
- MySQL reachability from the host platform

### Browser requests fail with CORS errors

Check:

- `Url__Frontend`
- `Url__Backend`
- correct deployed frontend origin
- trailing slash mismatches

### Login works but protected API requests fail

Check:

- JWT issuer and audience consistency
- signing secret consistency
- system clock drift if tokens appear expired immediately

## Related Docs

- [README.md](./README.md)
- [QUICK_START.md](./QUICK_START.md)
- [CONFIGURATION_INDEX.md](./CONFIGURATION_INDEX.md)
- [ENVIRONMENT_VARIABLES_SUMMARY.md](./ENVIRONMENT_VARIABLES_SUMMARY.md)
- [ENV_VARIABLES_CHECKLIST.md](./ENV_VARIABLES_CHECKLIST.md)
