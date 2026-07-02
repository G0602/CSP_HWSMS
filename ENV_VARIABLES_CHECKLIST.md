# Environment Variables Checklist

Use this checklist before local demos, staging validation, or production deployment.

## Backend

- [ ] `ConnectionStrings__DefaultConnection` is set (or individual database parameters below are set)
- [ ] `Db__Host` is set (if not using a full connection string)
- [ ] `Db__Port` is set (default: 3306)
- [ ] `Db__Name` is set
- [ ] `Db__User` is set
- [ ] `Db__Password` is set
- [ ] `Jwt__Secret` is present and strong (minimum 32 bytes)
- [ ] `Jwt__Issuer` is set
- [ ] `Jwt__Audience` is set
- [ ] `Jwt__AccessTokenExpiryMinutes` is set
- [ ] `Url__Frontend` includes the real frontend origin(s)
- [ ] `Url__Backend` includes the backend API origin(s)
- [ ] `ASPNETCORE_ENVIRONMENT` matches the target environment (`Development`, `Integration`, or `Production`)
- [ ] `ASPNETCORE_URLS` is set if the host requires an explicit bind address

## Backend development & integration seeding

- [ ] `Password__Admin` is set if admin seeding is required
- [ ] `Password__Manager` is set if manager seeding is required
- [ ] `Password__Cashier` is set if cashier seeding is required

Seed users are created only when:

- [ ] `ASPNETCORE_ENVIRONMENT=Development` or `ASPNETCORE_ENVIRONMENT=Integration`
- [ ] all three password variables are present

## Frontend

- [ ] `VITE_API_BASE_URL` points to the intended backend
- [ ] `VITE_DEBUG` is disabled for production unless explicitly needed

## Verification commands

Backend health:

```bash
curl http://localhost:5162/api/health
```

Backend tests:

```bash
dotnet test backend/HSMS.Tests/HSMS.Tests.csproj --no-restore
dotnet test backend/HSMS.ApiTests/HSMS.ApiTests.csproj --no-restore
```

Frontend validation:

```bash
cd frontend/HWSMS_UI
npm test
npm run build
```

## Production safety checks

- [ ] no secrets are committed to git
- [ ] JWT secrets are not reused from development
- [ ] production DB credentials are not local defaults
- [ ] seed users are not treated as the production user-management strategy
- [ ] CORS includes only intended production origins
- [ ] HTTPS is enabled in the final hosting environment

## Related Docs

- [ENVIRONMENT_VARIABLES_SUMMARY.md](./ENVIRONMENT_VARIABLES_SUMMARY.md)
- [CONFIGURATION_INDEX.md](./CONFIGURATION_INDEX.md)
- [QUICK_START.md](./QUICK_START.md)
- [DEPLOYMENT_GUIDE.md](./DEPLOYMENT_GUIDE.md)
