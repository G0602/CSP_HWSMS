# Integration Test Issues Log

## Current Status

- Backend database integration tests are implemented in `backend/HSMS.Tests` and use `HSMS_TEST_CONNECTION_STRING`.
- Tests automatically return when `HSMS_TEST_CONNECTION_STRING` is not configured, so local unit-test runs remain fast and deterministic.
- Selenium E2E tests are implemented in `backend/HSMS.E2E` and use `HSMS_E2E_BASE_URL`, `HSMS_E2E_USERNAME`, and `HSMS_E2E_PASSWORD`.
- JMeter and Postman artifacts are generated from the controller/DTO source using the scripts in `scripts/`.

## Open Issues

- No live MySQL connection string was provided during this run, so database-backed integration tests were compiled and included but not executed against a real database.
- No live frontend URL or E2E credentials were provided during this run, so Selenium tests verified opt-in execution behavior but did not drive a real browser session through the deployed app.
- JMeter test execution requires a running API, seeded product IDs with stock, and valid credentials before the enabled sales sampler can succeed under load.

## Commands

```bash
dotnet test backend/HSMS.sln --collect:"XPlat Code Coverage" --settings backend/HSMS.Tests/coverlet.runsettings
npm test --prefix frontend/HWSMS_UI
node scripts/generate-postman-collection.js
node scripts/generate-jmeter-test-plan.js
```
