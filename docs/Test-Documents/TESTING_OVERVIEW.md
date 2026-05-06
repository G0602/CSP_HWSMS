# Testing Overview

This is the canonical landing page for active testing documentation in the repository.

## Active Test Suites

| Suite | Location | Current validated result |
|---|---|---|
| Backend unit/integration/security | `backend/HSMS.Tests` | `296` passing tests |
| Backend API tests | `backend/HSMS.ApiTests` | `194` passing tests |
| Frontend tests | `frontend/HWSMS_UI` | `17` passing tests |
| Browser E2E | `backend/HSMS.E2E` | Optional, environment-driven |

## Core Test Documents

- [HSMS_MASTER_TEST_PLAN.md](./HSMS_MASTER_TEST_PLAN.md)
- [API_TEST_PLAN.md](./API_TEST_PLAN.md)
- [INTEGRATION_TEST_ISSUES.md](./INTEGRATION_TEST_ISSUES.md)
- [JMETER_LOAD_TEST.md](./JMETER_LOAD_TEST.md)

## Execution Guides

- [Guides/TEST_EXECUTION_GUIDE.md](./Guides/TEST_EXECUTION_GUIDE.md)
- [Guides/DELIVERY_CHECKLIST.md](./Guides/DELIVERY_CHECKLIST.md)
- [Guides/HSMS_TESTS_README.md](./Guides/HSMS_TESTS_README.md)
- [Guides/HSMS_APITESTS_README.md](./Guides/HSMS_APITESTS_README.md)

## Historical Material

Superseded test reports, sprint-era summaries, and archival audits live under:

- [Archive](./Archive)

Those files are retained for historical reference only and should not be treated as the current source of truth.

## Standard Commands

Backend test projects:

```bash
dotnet test backend/HSMS.Tests/HSMS.Tests.csproj --no-restore
dotnet test backend/HSMS.ApiTests/HSMS.ApiTests.csproj --no-restore
```

Frontend test and build:

```bash
cd frontend/HWSMS_UI
npm test
npm run build
```

## Current Test Tooling

- backend unit/integration/security: xUnit, Moq, coverlet
- API tests: xUnit + RestSharp
- frontend tests: Vitest + Testing Library
- load tests: JMeter
- collection-based API coverage: Postman

## Notes

- `backend/HSMS.Tests` uses `HSMS_TEST_CONNECTION_STRING` for DB-backed integration coverage.
- `backend/HSMS.E2E` is opt-in and depends on environment variables and a reachable app instance.
- Postman and JMeter artifacts are generated from current source-aware scripts in `scripts/`.
