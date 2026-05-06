# HWSMS Configuration Index

This document is the navigation page for current configuration and deployment references.

## Start Here

- [README.md](./README.md)
- [QUICK_START.md](./QUICK_START.md)
- [DEPLOYMENT_GUIDE.md](./DEPLOYMENT_GUIDE.md)
- [ENVIRONMENT_VARIABLES_SUMMARY.md](./ENVIRONMENT_VARIABLES_SUMMARY.md)
- [ENV_VARIABLES_CHECKLIST.md](./ENV_VARIABLES_CHECKLIST.md)

## Configuration Sources

### Backend

The backend uses ASP.NET Core configuration layering:

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. environment variables

Important current behavior:

- the backend does not auto-load `backend/.env.example`
- alias environment variables are applied in `backend/HSMS.API/Program.cs`

Primary backend files:

- `backend/.env.example`
- `backend/HSMS.API/appsettings.json`
- `backend/HSMS.API/appsettings.Development.json`
- `backend/HSMS.API/appsettings.Production.json`
- `backend/HSMS.API/Program.cs`

### Frontend

The frontend uses Vite environment loading and does read `.env.*` files automatically.

Primary frontend files:

- `frontend/HWSMS_UI/.env.example`
- `frontend/HWSMS_UI/src/config/api.ts`
- `frontend/HWSMS_UI/package.json`

## Runtime Variable Families

### Backend

- database: `ConnectionStrings__DefaultConnection`
- JWT aliases: `JWT_SECRET`, `JWT_ISSUER`, `JWT_AUDIENCE`, `JWT_EXPIRY_MINUTES`
- JWT config keys: `Jwt__Secret`, `Jwt__Issuer`, `Jwt__Audience`, `Jwt__AccessTokenExpiryMinutes`
- deployment aliases: `AZURE_MYSQL_CONNECTIONSTRING`, `MYSQLCONNSTR_DefaultConnection`
- CORS: `CORS_ORIGINS`, `FRONTEND_URL`
- development seeding: `ADMIN_PASSWORD`, `MANAGER_PASSWORD`, `CASHIER_PASSWORD`

### Frontend

- `VITE_API_BASE_URL`
- legacy `VITE_API_URL`
- `VITE_DEBUG`

## Current Repo Reality

- backend API startup validates required configuration
- Swagger is only exposed in `Development`
- seed users are only attempted in `Development`
- seed users require all three seed password variables
- CI/CD is handled by GitHub Actions, not by a checked-in root deployment compose file

## By Need

| Need | Document |
|---|---|
| Run locally | [QUICK_START.md](./QUICK_START.md) |
| Deploy | [DEPLOYMENT_GUIDE.md](./DEPLOYMENT_GUIDE.md) |
| Understand variables | [ENVIRONMENT_VARIABLES_SUMMARY.md](./ENVIRONMENT_VARIABLES_SUMMARY.md) |
| Preflight a demo or deploy | [ENV_VARIABLES_CHECKLIST.md](./ENV_VARIABLES_CHECKLIST.md) |
| Backend-specific runtime notes | [backend/README.md](./backend/README.md) |
| Frontend-specific runtime notes | [frontend/HWSMS_UI/README.md](./frontend/HWSMS_UI/README.md) |
