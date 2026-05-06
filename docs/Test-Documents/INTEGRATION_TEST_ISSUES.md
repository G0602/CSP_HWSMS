# Integration Test Issues Log

This document records the current operational caveats for integration-style, API, E2E, and load-related test execution.

## Current State

- `backend/HSMS.Tests` includes DB-backed integration coverage under `Integration/`.
- those DB-backed tests depend on `HSMS_TEST_CONNECTION_STRING`
- `backend/HSMS.E2E` is opt-in and depends on environment variables plus a reachable running app
- Postman and JMeter artifacts are maintained through source-aware scripts in `scripts/`

## Known Execution Constraints

- database-backed integration tests need a reachable MySQL instance and appropriate schema/data
- API tests need the backend API running and reachable
- E2E tests need valid credentials and a reachable frontend/backend deployment target
- load tests need carefully prepared IDs, stock, and credentials before mutating flows can be enabled safely

## Current Commands

```bash
dotnet test backend/HSMS.Tests/HSMS.Tests.csproj --no-restore
dotnet test backend/HSMS.ApiTests/HSMS.ApiTests.csproj --no-restore
```

```bash
cd frontend/HWSMS_UI
npm test
npm run build
```

Optional generation commands:

```bash
node scripts/generate-postman-collection.js
node scripts/generate-jmeter-test-plan.js
```

## Follow-up Areas to Watch

- API test dependency vulnerability warning on `RestSharp` in `HSMS.ApiTests`
- keeping generated Postman and JMeter assets refreshed when routes or DTOs change
- keeping frontend test coverage broad enough as more pages gain dedicated tests

## Related Docs

- [TESTING_OVERVIEW.md](./TESTING_OVERVIEW.md)
- [Guides/TEST_EXECUTION_GUIDE.md](./Guides/TEST_EXECUTION_GUIDE.md)
- [JMETER_LOAD_TEST.md](./JMETER_LOAD_TEST.md)
