# HWSMS Backend

ASP.NET Core 8 backend for the Hardware Store Management System.

## Projects

```text
backend/
├── HSMS.sln
├── HSMS.API/
├── HSMS.Application/
├── HSMS.Domain/
├── HSMS.Infrastructure/
├── HSMS.Tests/
├── HSMS.ApiTests/
└── HSMS.E2E/
```

## Main Responsibilities

| Project | Responsibility |
|---|---|
| `HSMS.API` | Controllers, auth, startup, DI, HTTP pipeline |
| `HSMS.Application` | DTOs and interfaces |
| `HSMS.Domain` | Entities |
| `HSMS.Infrastructure` | Data access and database initialization |
| `HSMS.Tests` | Unit and integration-style tests |
| `HSMS.ApiTests` | API test suite |
| `HSMS.E2E` | Browser/E2E test project |

## Configuration

The backend configuration comes from:

1. `HSMS.API/appsettings.json`
2. `HSMS.API/appsettings.{Environment}.json`
3. environment variables

Important runtime variables:

- `ConnectionStrings__DefaultConnection`
- `JWT_SECRET` or `Jwt__Secret`
- `JWT_ISSUER` or `Jwt__Issuer`
- `JWT_AUDIENCE` or `Jwt__Audience`
- `CORS_ORIGINS`
- `FRONTEND_URL`
- `LOW_STOCK_THRESHOLD`

The file `backend/.env.example` is a template/reference. The API does not automatically load it by itself.

## Run Locally

```bash
cd backend
dotnet restore
dotnet run --project HSMS.API
```

Local development URLs:

- `http://localhost:5162`
- `https://localhost:7111`

Useful endpoints:

- Swagger: `http://localhost:5162/swagger`
- Health: `http://localhost:5162/api/health`

Swagger is only available in `Development`.

## API Areas

- authentication
- products and inventory
- suppliers
- sales
- reports
- users and roles

## Run Tests

```bash
cd backend
dotnet test
```

## Test Order

Use this order when reading or running backend tests:

1. `HSMS.Tests/Unit` - fast unit tests for controllers, services, configuration, and validation rules
2. `HSMS.Tests/Integration` - database and repository integration tests
3. `HSMS.Tests/Security` - authorization, token, and security-focused tests
4. `HSMS.ApiTests` - REST API endpoint tests grouped by feature area
5. `HSMS.E2E` - browser-level end-to-end tests

Current backend test layout:

```text
backend/
├── HSMS.Tests/
│   ├── Unit/
│   ├── Integration/
│   └── Security/
├── HSMS.ApiTests/
│   ├── Auth/
│   ├── Products/
│   ├── Sales/
│   ├── Reports/
│   ├── Users/
│   └── Suppliers/
└── HSMS.E2E/
```

Or run specific projects:

```bash
dotnet test HSMS.Tests/HSMS.Tests.csproj
dotnet test HSMS.ApiTests/HSMS.ApiTests.csproj
dotnet test HSMS.E2E/HSMS.E2E.csproj
```

## Notes

- The database initializer runs at application startup.
- Development seed users are created only when the environment is `Development` and all three seed password variables are provided.
- Backend authorization is policy-based and remains the source of truth even if the frontend hides routes.

## Related Docs

- [../README.md](../README.md)
- [../QUICK_START.md](../QUICK_START.md)
- [../DEPLOYMENT_GUIDE.md](../DEPLOYMENT_GUIDE.md)
- [HSMS.Tests/README.md](./HSMS.Tests/README.md)
- [HSMS.ApiTests/README.md](./HSMS.ApiTests/README.md)
