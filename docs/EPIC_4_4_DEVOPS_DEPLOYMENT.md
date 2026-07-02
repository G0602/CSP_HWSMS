# EPIC 4.4 DevOps and Deployment

This note summarizes the current DevOps implementation reflected in the repository.

## Current Workflow

The active CI/CD workflow is:

- [../.github/workflows/ci-cd.yml](../.github/workflows/ci-cd.yml)

It currently covers:

- backend restore, build, and tests
- backend API startup plus API test execution
- frontend dependency install, tests, and build
- Azure App Service backend deployment on push to `main`
- Azure Static Web Apps frontend deployment on push to `main`
- post-deploy smoke checks
- optional Slack failure notification

## Required GitHub Configuration

### Secrets

- `AZURE_WEBAPP_PUBLISH_PROFILE`
- `AZURE_STATIC_WEB_APP_TOKEN`
- `SLACK_WEBHOOK_URL` optional

### Variables

- `AZURE_BACKEND_APP_NAME`
- `BACKEND_PUBLIC_URL`
- `FRONTEND_PUBLIC_URL`
- `VITE_API_BASE_URL`

## Required Backend App Settings

- `ASPNETCORE_ENVIRONMENT=Production`
- `ConnectionStrings__DefaultConnection=<mysql-connection-string>` (or individual `Db__Host`, `Db__Port`, `Db__Name`, `Db__User`, `Db__Password` variables)
- `Jwt__Secret=<strong-secret>`
- `Jwt__Issuer=HSMS.API`
- `Jwt__Audience=HSMS.Client`
- `Jwt__AccessTokenExpiryMinutes=60`
- `Url__Frontend=<frontend-urls>`
- `Url__Backend=<backend-urls>`

## Operational Notes

- CORS should be set explicitly in production
- backend and frontend are deployed independently
- smoke testing depends on configured public URLs

## Related Docs

- [../DEPLOYMENT_GUIDE.md](../DEPLOYMENT_GUIDE.md)
- [../CONFIGURATION_INDEX.md](../CONFIGURATION_INDEX.md)
