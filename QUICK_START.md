# HWSMS Quick Start

This guide is the fastest path to running the current project locally.

## Prerequisites

- .NET SDK 8.0+
- Node.js 18+ and npm
- MySQL 8.0+

## 1. Backend setup

The backend does not auto-load `.env` files. Use `backend/.env.example` as a reference only, then provide values through environment variables, local appsettings overrides, or your IDE run profile.

Minimum backend settings:

- `ConnectionStrings__DefaultConnection`
- `JWT_SECRET` or `Jwt__Secret`
- `JWT_ISSUER` or `Jwt__Issuer`
- `JWT_AUDIENCE` or `Jwt__Audience`

Optional local development seed settings:

- `ADMIN_PASSWORD`
- `MANAGER_PASSWORD`
- `CASHIER_PASSWORD`

Run the API:

```bash
cd backend
dotnet restore
dotnet run --project HSMS.API
```

Default local backend URLs:

- `http://localhost:5162`
- `https://localhost:7111`

Useful endpoints:

- Swagger: `http://localhost:5162/swagger`
- Health: `http://localhost:5162/api/health`

## 2. Frontend setup

The frontend uses Vite environment loading.

```bash
cd frontend/HWSMS_UI
cp .env.example .env.development
npm install
npm run dev
```

Default local frontend URL:

- `http://localhost:5173`

Recommended development value:

```env
VITE_API_BASE_URL=http://localhost:5162
```

## 3. Typical local workflow

Run these in separate terminals:

```bash
cd backend
dotnet run --project HSMS.API
```

```bash
cd frontend/HWSMS_UI
npm run dev
```

## 4. Development seed users

Seed users are created only when all of these are true:

- `ASPNETCORE_ENVIRONMENT=Development`
- `ADMIN_PASSWORD` is set
- `MANAGER_PASSWORD` is set
- `CASHIER_PASSWORD` is set

There are no guaranteed hard-coded default passwords in the current implementation.

## 5. Verify the setup

Backend health:

```bash
curl http://localhost:5162/api/health
```

Backend tests:

```bash
dotnet test backend/HSMS.Tests/HSMS.Tests.csproj --no-restore
dotnet test backend/HSMS.ApiTests/HSMS.ApiTests.csproj --no-restore
```

Frontend tests and production build:

```bash
cd frontend/HWSMS_UI
npm test
npm run build
```

## Troubleshooting

| Issue | Check |
|---|---|
| Backend startup fails | Connection string and JWT settings |
| Frontend cannot reach backend | `VITE_API_BASE_URL` and CORS origins |
| Swagger is missing | `ASPNETCORE_ENVIRONMENT` should be `Development` |
| Seed users are missing | Development environment and all three password variables |

## Related Docs

- [README.md](./README.md)
- [CONFIGURATION_INDEX.md](./CONFIGURATION_INDEX.md)
- [ENVIRONMENT_VARIABLES_SUMMARY.md](./ENVIRONMENT_VARIABLES_SUMMARY.md)
- [ENV_VARIABLES_CHECKLIST.md](./ENV_VARIABLES_CHECKLIST.md)
- [DEPLOYMENT_GUIDE.md](./DEPLOYMENT_GUIDE.md)
