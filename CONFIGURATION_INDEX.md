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

- the backend does not auto-load a `.env` file (the previously checked-in template has been removed).
- configuration validation and connection string construction logic are implemented in `backend/HSMS.API/Program.cs` without alias variable mapping.

Primary backend files:

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

- database: `ConnectionStrings__DefaultConnection` or individual credentials keys (`Db__Host`, `Db__Port`, `Db__Name`, `Db__User`, `Db__Password`)
- JWT config keys: `Jwt__Secret`, `Jwt__Issuer`, `Jwt__Audience`, `Jwt__AccessTokenExpiryMinutes`
- CORS config keys: `Url__Frontend`, `Url__Backend`
- development seeding: `Password__Admin`, `Password__Manager`, `Password__Cashier`

### Frontend

- `VITE_API_BASE_URL`
- legacy `VITE_API_URL`
- `VITE_DEBUG`

## Current Repo Reality

- backend API startup validates required configuration
- Swagger is only exposed in `Development`
- seed users are attempted in `Development` and `Integration` environments
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
