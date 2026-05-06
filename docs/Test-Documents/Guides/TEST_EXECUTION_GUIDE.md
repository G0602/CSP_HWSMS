# Test Execution Guide

This guide explains how to run the current HSMS automated test suites and supporting generated artifacts.

## Current Verified Results

- backend tests: `296` passing
- API tests: `194` passing
- frontend tests: `17` passing

## 1. Backend test suite

Run:

```bash
dotnet test backend/HSMS.Tests/HSMS.Tests.csproj --no-restore
```

This project contains:

- `Unit/`
- `Integration/`
- `Security/`

DB-backed integration coverage depends on:

- `HSMS_TEST_CONNECTION_STRING`

## 2. API test suite

Start the backend first:

```bash
cd backend
dotnet run --project HSMS.API
```

Then run:

```bash
dotnet test backend/HSMS.ApiTests/HSMS.ApiTests.csproj --no-restore
```

## 3. Frontend tests and build

```bash
cd frontend/HWSMS_UI
npm test
npm run build
```

## 4. Optional E2E execution

`backend/HSMS.E2E` is environment-driven and not required for every submission or local pass.

Run it only when the needed app instance and credentials are configured.

## 5. Supporting artifact generation

Regenerate Postman artifacts:

```bash
node scripts/generate-postman-collection.js
```

Regenerate JMeter artifacts:

```bash
node scripts/generate-jmeter-test-plan.js
```

## 6. Troubleshooting

### Backend integration tests are skipped or ineffective

Check:

- `HSMS_TEST_CONNECTION_STRING`
- reachable MySQL instance
- expected schema/data state

### API tests fail immediately

Check:

- backend API is running
- API is reachable at the expected base URL
- test credentials align with your configured seed users

### Frontend tests pass but the app cannot call the API

Check:

- `VITE_API_BASE_URL`
- backend CORS configuration
- backend health endpoint

### Swagger is unavailable

Check:

- `ASPNETCORE_ENVIRONMENT=Development`

## Related Docs

- [../TESTING_OVERVIEW.md](../TESTING_OVERVIEW.md)
- [HSMS_TESTS_README.md](./HSMS_TESTS_README.md)
- [HSMS_APITESTS_README.md](./HSMS_APITESTS_README.md)
- [DELIVERY_CHECKLIST.md](./DELIVERY_CHECKLIST.md)
