# Environment Variables Checklist

Use this checklist before local demos, staging, or production deployment.

## Backend

- [ ] `ConnectionStrings__DefaultConnection` is set and points to the correct database
- [ ] `JWT_SECRET` or `Jwt__Secret` is set and is at least 32 bytes
- [ ] `JWT_ISSUER` or `Jwt__Issuer` is set
- [ ] `JWT_AUDIENCE` or `Jwt__Audience` is set
- [ ] `CORS_ORIGINS` includes the real frontend origin
- [ ] `ASPNETCORE_ENVIRONMENT` is correct for the target environment
- [ ] `ASPNETCORE_URLS` is set if the host requires an explicit bind address

## Backend Development Seeding

Only for local development:

- [ ] `ADMIN_PASSWORD` is set if you want an admin seed user
- [ ] `MANAGER_PASSWORD` is set if you want a manager seed user
- [ ] `CASHIER_PASSWORD` is set if you want a cashier seed user

Seed users are created only when:

- [ ] `ASPNETCORE_ENVIRONMENT=Development`
- [ ] all three password variables are present

## Frontend

- [ ] `VITE_API_BASE_URL` points to the correct backend
- [ ] `VITE_DEBUG` is disabled for production builds unless intentionally needed

## Verification Commands

Backend health:

```bash
curl http://localhost:5162/api/health
```

Frontend production build:

```bash
cd frontend/HWSMS_UI
npm run build
```

Backend tests:

```bash
cd backend
dotnet test
```

## Production Safety Checks

- [ ] No secrets are committed to git
- [ ] Production JWT secret is unique and not reused from development
- [ ] Production database credentials are not local defaults
- [ ] Seed users are not relied on as the production user-management strategy
- [ ] CORS only includes intended production origins
- [ ] HTTPS is enabled in the final hosting environment

## Notes

- The backend does not auto-load `.env` files in the current implementation.
- The frontend does load Vite `.env.*` files automatically.
- Swagger is only available in `Development` with the current startup configuration.

## Related Docs

- [QUICK_START.md](./QUICK_START.md)
- [DEPLOYMENT_GUIDE.md](./DEPLOYMENT_GUIDE.md)
- [CONFIGURATION_INDEX.md](./CONFIGURATION_INDEX.md)
