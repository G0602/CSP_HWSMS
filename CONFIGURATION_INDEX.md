# HWSMS Configuration Index

This document is the navigation page for project configuration and deployment references.

## Start Here

- [README.md](./README.md): full project overview
- [QUICK_START.md](./QUICK_START.md): fastest local setup path
- [DEPLOYMENT_GUIDE.md](./DEPLOYMENT_GUIDE.md): production-focused deployment steps
- [ENV_VARIABLES_CHECKLIST.md](./ENV_VARIABLES_CHECKLIST.md): deployment verification checklist
- [ENVIRONMENT_VARIABLES_SUMMARY.md](./ENVIRONMENT_VARIABLES_SUMMARY.md): summary of how configuration works

## Configuration Sources

### Backend

The backend uses ASP.NET Core configuration layering:

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. environment variables

Important: the current backend does not auto-load a `.env` file on its own. Use `.env.example` only as a template/reference.

Primary backend files:

- `backend/.env.example`
- `backend/HSMS.API/appsettings.json`
- `backend/HSMS.API/appsettings.Development.json`
- `backend/HSMS.API/appsettings.Production.json`
- `backend/HSMS.API/Program.cs`

### Frontend

The frontend uses Vite environment variables and does read `.env.*` files automatically.

Primary frontend files:

- `frontend/HWSMS_UI/.env.example`
- `frontend/HWSMS_UI/src/config/api.ts`
- `frontend/HWSMS_UI/package.json`

## Key Configuration Values

### Backend

| Setting | Purpose |
|---|---|
| `ConnectionStrings__DefaultConnection` | Database connection |
| `JWT_SECRET` / `Jwt__Secret` | JWT signing secret |
| `JWT_ISSUER` / `Jwt__Issuer` | Token issuer |
| `JWT_AUDIENCE` / `Jwt__Audience` | Token audience |
| `CORS_ORIGINS` | Allowed frontend origins |
| `FRONTEND_URL` | Additional frontend origin source |
| `LOW_STOCK_THRESHOLD` | Low-stock threshold |
| `ADMIN_PASSWORD` | Dev seed password |
| `MANAGER_PASSWORD` | Dev seed password |
| `CASHIER_PASSWORD` | Dev seed password |

### Frontend

| Setting | Purpose |
|---|---|
| `VITE_API_BASE_URL` | Base backend URL |
| `VITE_DEBUG` | Optional debug logging flag |

## Current Repository Reality

- Root Docker Compose deployment files are not present in this repository.
- Swagger is only enabled in `Development` by current API startup code.
- Seed users are created only in `Development`, and only when all three seed password variables are present.
- The frontend route protection mirrors backend role-based access control but does not replace backend authorization.

## Suggested Local Setup

Backend:

```bash
cd backend
dotnet restore
dotnet run --project HSMS.API
```

Frontend:

```bash
cd frontend/HWSMS_UI
cp .env.example .env.development
npm install
npm run dev
```

## Suggested Production Setup

- publish backend with `dotnet publish`
- provide backend secrets and connection string via environment variables
- build frontend with `npm run build`
- host frontend `dist` output on static hosting

## Related Docs by Need

| Need | Document |
|---|---|
| Understand the whole system | [README.md](./README.md) |
| Run the app locally | [QUICK_START.md](./QUICK_START.md) |
| Deploy to production | [DEPLOYMENT_GUIDE.md](./DEPLOYMENT_GUIDE.md) |
| Verify deployment settings | [ENV_VARIABLES_CHECKLIST.md](./ENV_VARIABLES_CHECKLIST.md) |
| Review backend details | [backend/README.md](./backend/README.md) |
| Review frontend details | [frontend/HWSMS_UI/README.md](./frontend/HWSMS_UI/README.md) |
