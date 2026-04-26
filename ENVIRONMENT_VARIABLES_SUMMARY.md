# Environment Variables Summary

This document summarizes how configuration currently works in the repository.

## What Changed in the Documentation

The documentation set has been aligned to the current codebase:

- removed instructions that assumed backend `.env` files auto-load automatically
- removed references to root Docker Compose files that are not present in this repository
- removed fixed default passwords that are no longer guaranteed by the code
- aligned backend and frontend run commands with the real project structure
- aligned Swagger guidance with the current `Development`-only behavior

## Current Configuration Model

### Backend

The backend uses standard ASP.NET Core configuration precedence:

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. environment variables

Environment variables win over JSON values.

Examples:

- `ConnectionStrings__DefaultConnection`
- `Jwt__Secret`
- `Jwt__Issuer`
- `Jwt__Audience`
- `CORS_ORIGINS`
- `FRONTEND_URL`

The backend also supports alias environment variables in `Program.cs`, including:

- `JWT_SECRET`
- `JWT_ISSUER`
- `JWT_AUDIENCE`
- `JWT_EXPIRY_MINUTES`
- `AZURE_MYSQL_CONNECTIONSTRING`
- `MYSQLCONNSTR_DefaultConnection`

### Frontend

The frontend uses Vite environment loading. It resolves the API base URL in this order:

1. `VITE_API_BASE_URL`
2. legacy `VITE_API_URL`
3. local default in development
4. deployed default in production

## Operational Notes

- Backend `.env.example` is a template, not an automatically loaded runtime source.
- Frontend `.env.example` can be copied to `.env.development` or `.env.production`.
- Seed users are only attempted in `Development`.
- Seed users are skipped unless all three password variables are set.
- Swagger is exposed only in `Development`.

## Recommended Backend Variables

| Variable | Purpose |
|---|---|
| `ConnectionStrings__DefaultConnection` | Database connection string |
| `JWT_SECRET` or `Jwt__Secret` | JWT signing secret |
| `JWT_ISSUER` or `Jwt__Issuer` | JWT issuer |
| `JWT_AUDIENCE` or `Jwt__Audience` | JWT audience |
| `CORS_ORIGINS` | Allowed frontend origins |
| `ASPNETCORE_ENVIRONMENT` | Environment selection |

## Recommended Frontend Variables

| Variable | Purpose |
|---|---|
| `VITE_API_BASE_URL` | Backend API URL |
| `VITE_DEBUG` | Optional debug flag |

## Practical Setup Summary

### Local backend

```bash
cd backend
dotnet run --project HSMS.API
```

Before running, set environment variables in your shell or local appsettings override.

### Local frontend

```bash
cd frontend/HWSMS_UI
cp .env.example .env.development
npm install
npm run dev
```

## Where to Look Next

- [README.md](./README.md)
- [QUICK_START.md](./QUICK_START.md)
- [DEPLOYMENT_GUIDE.md](./DEPLOYMENT_GUIDE.md)
- [CONFIGURATION_INDEX.md](./CONFIGURATION_INDEX.md)
