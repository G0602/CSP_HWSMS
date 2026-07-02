# HWSMS Backend

Backend overview for the current ASP.NET Core 8 solution.

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

## Responsibilities

| Project | Responsibility |
|---|---|
| `HSMS.API` | Controllers, auth, startup, DI, HTTP pipeline |
| `HSMS.Application` | DTOs and service/repository interfaces |
| `HSMS.Domain` | Core entity models |
| `HSMS.Infrastructure` | DB initialization, repositories, persistence |
| `HSMS.Tests` | Unit, integration, and security tests |
| `HSMS.ApiTests` | API endpoint tests |
| `HSMS.E2E` | Selenium E2E coverage |

## Configuration

The backend reads configuration from:

1. `HSMS.API/appsettings.json`
2. `HSMS.API/appsettings.{Environment}.json`
3. environment variables

Important variables:

- `ConnectionStrings__DefaultConnection` (or individual database parameters `Db__Host`, `Db__Port`, `Db__Name`, `Db__User`, `Db__Password`)
- `Jwt__Secret`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Jwt__AccessTokenExpiryMinutes`
- `Url__Frontend`
- `Url__Backend`
- `Password__Admin`
- `Password__Manager`
- `Password__Cashier`

Note: The previously checked-in `backend/.env.example` template has been removed, and the backend does not load any `.env` file at runtime. All configurations must be supplied via host environment variables or appsettings JSON files.

## Run locally

```bash
cd backend
dotnet restore
dotnet run --project HSMS.API
```

Default local URLs:

- `http://localhost:5162`
- `https://localhost:7111`

Useful endpoints:

- Swagger: `http://localhost:5162/swagger`
- Health: `http://localhost:5162/api/health`

## API areas

- authentication
- products and inventory
- suppliers
- sales
- reports
- users and roles

Notable current behavior:

- self-registration is disabled and `POST /api/auth/register` returns `403`
- admin-only password reset is available through `PUT /api/users/{id}/password`

## Test layout

Current backend suite structure:

```text
backend/
├── HSMS.Tests/
│   ├── Unit/
│   ├── Integration/
│   └── Security/
├── HSMS.ApiTests/
│   ├── Auth/
│   ├── Products/
│   ├── Reports/
│   ├── Sales/
│   ├── Suppliers/
│   └── Users/
└── HSMS.E2E/
```

Validated counts from the current repo:

- `HSMS.Tests`: `296` passing tests
- `HSMS.ApiTests`: `194` passing tests

Run them with:

```bash
dotnet test HSMS.Tests/HSMS.Tests.csproj --no-restore
dotnet test HSMS.ApiTests/HSMS.ApiTests.csproj --no-restore
```

## Related Docs

- [../README.md](../README.md)
- [../QUICK_START.md](../QUICK_START.md)
- [../DEPLOYMENT_GUIDE.md](../DEPLOYMENT_GUIDE.md)
- [../docs/Test-Documents/TESTING_OVERVIEW.md](../docs/Test-Documents/TESTING_OVERVIEW.md)
- [HSMS.Tests/README.md](./HSMS.Tests/README.md)
- [HSMS.ApiTests/README.md](./HSMS.ApiTests/README.md)
