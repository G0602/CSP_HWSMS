# EPIC 4.4 - DevOps and Deployment

## Current CI/CD Setup

The GitHub Actions workflow lives at `.github/workflows/ci-cd.yml`.

It now runs:

- Backend restore, build, tests, and coverage collection.
- Frontend dependency install, Vitest tests, and production build.
- Backend deployment to Azure App Service on push to `main`.
- Frontend deployment to Azure Static Web Apps on push to `main`.
- Post-deploy smoke checks for backend health and frontend availability.
- Optional Slack notification when `SLACK_WEBHOOK_URL` is configured.

## Required GitHub Secrets

- `AZURE_WEBAPP_PUBLISH_PROFILE`: Azure App Service publish profile for the backend.
- `AZURE_STATIC_WEB_APP_TOKEN`: Azure Static Web Apps deployment token.
- `SLACK_WEBHOOK_URL`: optional failure notification webhook.

## Required GitHub Variables

- `AZURE_BACKEND_APP_NAME`: Azure App Service name.
- `BACKEND_PUBLIC_URL`: deployed backend URL, for example `https://hsmsbackend-e9acfpeff8bycuax.indonesiacentral-01.azurewebsites.net`.
- `FRONTEND_PUBLIC_URL`: deployed frontend URL, for example `https://delightful-tree-0e4ad5000.7.azurestaticapps.net`.
- `VITE_API_BASE_URL`: backend URL injected into the frontend build.

## Required Azure App Service Settings

Set these in Azure App Service Configuration for the backend:

- `ASPNETCORE_ENVIRONMENT=Production`
- `ConnectionStrings__DefaultConnection=<mysql-connection-string>`
- `Jwt__Secret=<strong-random-secret>`
- `Jwt__Issuer=HSMS.API`
- `Jwt__Audience=HSMS.Client`
- `CORS_ORIGINS=<frontend-origin-list>`
- `FRONTEND_URL=<frontend-url>`
- `BACKEND_PUBLIC_URL=<backend-url>`

`CORS_ORIGINS` should include every browser origin that will call the API. Example:

```text
https://delightful-tree-0e4ad5000.7.azurestaticapps.net,https://csp-hwsms-hnqk.vercel.app
```

## CORS Fix

The frontend was being blocked because the backend only allowed one exact production frontend URL. If the UI is served from Vercel while the backend only allows the Azure Static Web Apps URL, the browser sends an `Origin` header that the backend rejects.

The backend now:

- Reads `CORS_ORIGINS` and `FRONTEND_URL`.
- Includes the known Azure Static Web Apps and Vercel URLs in production defaults.
- Accepts HTTPS `csp-hwsms*.vercel.app` and `*.azurestaticapps.net` origins.

For strongest production security, keep `CORS_ORIGINS` set explicitly in Azure App Service.

## Security Actions Still Required

The old `deploy-cred.env` file contained a real Azure MySQL password. It has been removed and ignored locally, but you must still:

- Rotate the Azure MySQL password.
- Update Azure App Service settings with the new connection string.
- Remove the secret from git history if it was pushed to a remote repository.
- Invalidate old publish profiles and regenerate Azure deployment credentials if they were exposed elsewhere.
