# Environment Variables Summary

This document is the concise reference for runtime configuration in the current repository.

## Backend Configuration Model

The backend uses this precedence order:

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. environment variables

Environment variables override JSON values.

The backend also maps alias variables in `backend/HSMS.API/Program.cs`.

### Common backend variables

| Variable | Purpose |
|---|---|
| `ConnectionStrings__DefaultConnection` | Main MySQL connection string |
| `JWT_SECRET` or `Jwt__Secret` | JWT signing secret |
| `JWT_ISSUER` or `Jwt__Issuer` | JWT issuer |
| `JWT_AUDIENCE` or `Jwt__Audience` | JWT audience |
| `JWT_EXPIRY_MINUTES` or `Jwt__AccessTokenExpiryMinutes` | Access token lifetime |
| `CORS_ORIGINS` | Allowed browser origins |
| `FRONTEND_URL` | Additional CORS origin source |
| `LOW_STOCK_THRESHOLD` | Low-stock reporting threshold |
| `ASPNETCORE_ENVIRONMENT` | Environment name |
| `ASPNETCORE_URLS` | API bind URLs |

### Supported alias variables

- `JWT_SECRET`
- `JWT_ISSUER`
- `JWT_AUDIENCE`
- `JWT_EXPIRY_MINUTES`
- `AZURE_MYSQL_CONNECTIONSTRING`
- `MYSQLCONNSTR_DefaultConnection`

### Development-only seed variables

- `ADMIN_PASSWORD`
- `MANAGER_PASSWORD`
- `CASHIER_PASSWORD`

Seed users are created only in `Development` and only when all three seed password variables are present.

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

- `backend/.env.example` is a template, not an auto-loaded runtime source.
- `frontend/HWSMS_UI/.env.example` can be copied to `.env.development` or `.env.production`.
- Swagger is only available in `Development`.
- Backend CORS allow-lists are built from configured origins plus current code defaults.

## Related Docs

- [CONFIGURATION_INDEX.md](./CONFIGURATION_INDEX.md)
- [QUICK_START.md](./QUICK_START.md)
- [DEPLOYMENT_GUIDE.md](./DEPLOYMENT_GUIDE.md)
- [ENV_VARIABLES_CHECKLIST.md](./ENV_VARIABLES_CHECKLIST.md)
