# Environment Variables Summary

This document is the concise reference for runtime configuration in the current repository.

## Backend Configuration Model

The backend uses this precedence order:

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. environment variables

Environment variables override JSON values.

The backend checks configurations at startup.

### Common backend variables

| Variable | Purpose |
|---|---|
| `ConnectionStrings__DefaultConnection` | Main MySQL connection string |
| `Db__Host` | Database server host (used if connection string is not set) |
| `Db__Port` | Database server port (default: 3306) |
| `Db__Name` | Database name |
| `Db__User` | Database user username |
| `Db__Password` | Database user password |
| `Jwt__Secret` | JWT signing secret (minimum 32 bytes) |
| `Jwt__Issuer` | JWT issuer string |
| `Jwt__Audience` | JWT audience string |
| `Jwt__AccessTokenExpiryMinutes` | Token expiry lifetime in minutes |
| `Url__Frontend` | Allowed browser origin URL(s) for CORS |
| `Url__Backend` | Backend API URL(s) for CORS |
| `LOW_STOCK_THRESHOLD` | Low-stock reporting threshold |
| `ASPNETCORE_ENVIRONMENT` | Environment name (e.g., `Development`, `Integration`, `Production`) |
| `ASPNETCORE_URLS` | API bind URLs |

### Seed variables (Development and Integration environments only)

- `Password__Admin`
- `Password__Manager`
- `Password__Cashier`

Seed users are created only when all three password variables are present in `Development` or `Integration` environments.

## Frontend Configuration Model

The frontend uses Vite environment loading.

API base URL resolution currently follows:

1. `VITE_API_BASE_URL`
2. legacy `VITE_API_URL`
3. local default in development
4. deployed default in production

### Common frontend variables

| Variable | Purpose |
|---|---|
| `VITE_API_BASE_URL` | Backend API base URL |
| `VITE_DEBUG` | Optional debug logging |

## Important Behavior Notes

- The backend does not auto-load a `.env` file; environment variables must be provided via the host platform, system environment, or launch configuration.
- `frontend/HWSMS_UI/.env.example` can be copied to `.env.development` or `.env.production`.
- Swagger is only available in `Development`.
- Backend CORS allow-lists are built from configured origins in `Url:Frontend` and `Url:Backend`.

## Related Docs

- [CONFIGURATION_INDEX.md](./CONFIGURATION_INDEX.md)
- [QUICK_START.md](./QUICK_START.md)
- [DEPLOYMENT_GUIDE.md](./DEPLOYMENT_GUIDE.md)
- [ENV_VARIABLES_CHECKLIST.md](./ENV_VARIABLES_CHECKLIST.md)
