# Environment Variables Checklist

Use this checklist before local demos, staging validation, or production deployment.

## Backend

- [ ] `ConnectionStrings__DefaultConnection` is set correctly
- [ ] `JWT_SECRET` or `Jwt__Secret` is present and strong
- [ ] `JWT_ISSUER` or `Jwt__Issuer` is set
- [ ] `JWT_AUDIENCE` or `Jwt__Audience` is set
- [ ] `CORS_ORIGINS` includes the real frontend origin
- [ ] `ASPNETCORE_ENVIRONMENT` matches the target environment
- [ ] `ASPNETCORE_URLS` is set if the host requires an explicit bind address

## Backend development seeding

Only for local development:

- [ ] `ADMIN_PASSWORD` is set if admin seeding is required
- [ ] `MANAGER_PASSWORD` is set if manager seeding is required
- [ ] `CASHIER_PASSWORD` is set if cashier seeding is required

Seed users are created only when:

- [ ] `ASPNETCORE_ENVIRONMENT=Development`
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
