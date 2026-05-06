# Testing Overview

This is the canonical testing summary for HWSMS. Use this file first, then follow links for detailed plans, execution guides, and reports.

## What This Covers

- How tests are organized in the repository
- Where to find test plans and execution guides
- Where to find test reports and audits
- How to run the main test suites

## Test Structure (Quick Map)

- Backend unit/integration tests: `backend/HSMS.Tests`
- API tests: `backend/HSMS.ApiTests`
- E2E tests (Selenium): `backend/HSMS.E2E` (optional / not required for some submissions)

## Core Test Documents

- Master test plan: [HSMS_MASTER_TEST_PLAN.md](./HSMS_MASTER_TEST_PLAN.md)
- API test plan: [API_TEST_PLAN.md](./API_TEST_PLAN.md)
- Integration test notes: [INTEGRATION_TEST_ISSUES.md](./INTEGRATION_TEST_ISSUES.md)
- JMeter plan: [JMETER_LOAD_TEST.md](./JMETER_LOAD_TEST.md)

## Execution Guides

- Test execution guide: [Guides/TEST_EXECUTION_GUIDE.md](./Guides/TEST_EXECUTION_GUIDE.md)
- Delivery checklist: [Guides/DELIVERY_CHECKLIST.md](./Guides/DELIVERY_CHECKLIST.md)
- HSMS.Tests README: [Guides/HSMS_TESTS_README.md](./Guides/HSMS_TESTS_README.md)
- HSMS.ApiTests README: [Guides/HSMS_APITESTS_README.md](./Guides/HSMS_APITESTS_README.md)

## Reports and Audits (Archived)

These are kept for historical reference:

- [Archive/README_Tests.md](./Archive/README_Tests.md)
- [Archive/TEST_QUICK_REFERENCE.md](./Archive/TEST_QUICK_REFERENCE.md)
- [Archive/TEST_INVENTORY_ANALYSIS.md](./Archive/TEST_INVENTORY_ANALYSIS.md)
- [Archive/TEST_COVERAGE_UPDATE_REPORT.md](./Archive/TEST_COVERAGE_UPDATE_REPORT.md)
- [Archive/API_TEST_COVERAGE_AUDIT.md](./Archive/API_TEST_COVERAGE_AUDIT.md)
- [Archive/TEST_SUITE_COMPLETION_REPORT.md](./Archive/TEST_SUITE_COMPLETION_REPORT.md)

## How To Run Tests (Short)

Backend tests:

```bash
cd backend
dotnet test HSMS.Tests/HSMS.Tests.csproj
```

API tests (start API first):

```bash
cd backend
dotnet run --project HSMS.API
```

```bash
cd backend
dotnet test HSMS.ApiTests/HSMS.ApiTests.csproj
```

## Test Credentials

Use dedicated test accounts for all test runs. Do not commit real passwords to the repository.

### Development and Local Testing

Seeded accounts are created only in Development when the seed password environment variables are set. If you use the API test suite, align the credentials with what you configured in your environment.

Common local placeholders used in docs:

| Role | Username | Password |
|---|---|---|
| Admin | admin | Admin@123 |
| Manager | manager | Manager@123 |
| Cashier | cashier | Cashier@123 |

### Deployment Testing (Staging or Production)

Create separate test-only accounts in the deployed environment and store the credentials in your secret manager or CI/CD variables. Do not reuse development passwords.

Recommended format:

| Role | Username | Password |
|---|---|---|
| Admin | admin | change-admin-password |
| Manager | manager | change-manager-password |
| Cashier | cashier | change-cashier-password |
