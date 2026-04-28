# HWSMS Quick Start

This guide gets the project running locally with the current repository layout and runtime behavior.

## Prerequisites

- .NET SDK 8.0+
- Node.js 18+
- npm 9+
- MySQL 8.0+

## Backend

The backend does not auto-load `.env` files by itself. Use `backend/.env.example` as a reference, then either:

- export environment variables in your shell
- add values to `backend/HSMS.API/appsettings.Development.json`
- configure environment variables in your IDE run profile

Minimum required backend values:

- `ConnectionStrings__DefaultConnection`
- `JWT_SECRET` or `Jwt__Secret`
- `JWT_ISSUER` or `Jwt__Issuer`
- `JWT_AUDIENCE` or `Jwt__Audience`

Optional development seed values:

- `ADMIN_PASSWORD`
- `MANAGER_PASSWORD`
- `CASHIER_PASSWORD`

Run the API:

```bash
cd backend
dotnet restore
dotnet run --project HSMS.API
```

Local backend URLs:

- `http://localhost:5162`
- `https://localhost:7111`

Useful endpoints:

- Swagger: `http://localhost:5162/swagger`
- Health check: `http://localhost:5162/api/health`

## Frontend

The frontend does use Vite environment files.

```bash
cd frontend/HWSMS_UI
cp .env.example .env.development
npm install
npm run dev
```

Default frontend local URL:

- `http://localhost:5173`

Default frontend API variable:

```env
VITE_API_BASE_URL=http://localhost:5162
```

## Local Development Flow

Run these in separate terminals:

```bash
cd backend
dotnet run --project HSMS.API
```

```bash
cd frontend/HWSMS_UI
npm install
npm run dev
```

## Development Seed Users

Seed users are created only when:

- `ASPNETCORE_ENVIRONMENT=Development`
- `ADMIN_PASSWORD` is set
- `MANAGER_PASSWORD` is set
- `CASHIER_PASSWORD` is set

There are no fixed default credentials in the current code. The passwords come from your environment configuration.

## Verification

Check the backend:

```bash
curl http://localhost:5162/api/health
```

Build the frontend:

```bash
cd frontend/HWSMS_UI
npm run build
```

Run backend tests:

```bash
cd backend
dotnet test
```

## Common Issues

| Issue | Check |
|---|---|
| Backend fails on startup | Connection string and JWT settings |
| Frontend cannot reach backend | `VITE_API_BASE_URL` and CORS origins |
| Swagger not visible | `ASPNETCORE_ENVIRONMENT` should be `Development` |
| Seed users missing | Development environment and all three password variables |

## Related Docs

- [README.md](./README.md)
- [DEPLOYMENT_GUIDE.md](./DEPLOYMENT_GUIDE.md)
- [ENV_VARIABLES_CHECKLIST.md](./ENV_VARIABLES_CHECKLIST.md)
- [CONFIGURATION_INDEX.md](./CONFIGURATION_INDEX.md)
