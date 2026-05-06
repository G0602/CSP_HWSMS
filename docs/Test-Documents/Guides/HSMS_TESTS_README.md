# HSMS.Tests Guide

Guide for the current `backend/HSMS.Tests` project.

## Purpose

`HSMS.Tests` is the main backend automation project covering:

- unit tests
- DB-backed integration tests
- security and authorization tests

## Current Layout

```text
backend/HSMS.Tests/
├── Unit/
│   ├── Configuration/
│   ├── Controllers/
│   ├── Services/
│   └── Validation/
├── Integration/
│   ├── Database/
│   └── Repositories/
└── Security/
```

## Current Validated Result

- `296` passing tests

## Run the Suite

```bash
dotnet test backend/HSMS.Tests/HSMS.Tests.csproj --no-restore
```

## Integration Test Requirement

DB-backed integration coverage relies on:

- `HSMS_TEST_CONNECTION_STRING`

Without that environment variable, integration-oriented execution will not represent a full DB-backed pass.

## Coverage Areas

- controller behavior
- service behavior
- validation and edge cases
- configuration logic
- repository persistence behavior
- transaction, rollback, and concurrency cases
- auth and authorization security checks

## Related Docs

- [../TESTING_OVERVIEW.md](../TESTING_OVERVIEW.md)
- [TEST_EXECUTION_GUIDE.md](./TEST_EXECUTION_GUIDE.md)
- [DELIVERY_CHECKLIST.md](./DELIVERY_CHECKLIST.md)
