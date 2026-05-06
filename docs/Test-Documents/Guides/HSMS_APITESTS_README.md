# HSMS.ApiTests Guide

Guide for the current `backend/HSMS.ApiTests` project.

## Purpose

`HSMS.ApiTests` exercises the backend through HTTP-level API tests.

## Current Layout

```text
backend/HSMS.ApiTests/
├── Auth/
├── Products/
├── Reports/
├── Sales/
├── Suppliers/
└── Users/
```

## Current Validated Result

- `194` passing tests

## Run the Suite

Start the API first:

```bash
cd backend
dotnet run --project HSMS.API
```

Then run:

```bash
dotnet test backend/HSMS.ApiTests/HSMS.ApiTests.csproj --no-restore
```

## Coverage Areas

- login and disabled self-registration
- product and inventory endpoints
- supplier endpoints
- sales history and invoice endpoints
- reporting endpoints
- user creation, role updates, and password reset

## Notes

- the project currently emits a `RestSharp` vulnerability warning during restore/build
- use test-safe credentials aligned with your current configured environment

## Related Docs

- [../TESTING_OVERVIEW.md](../TESTING_OVERVIEW.md)
- [../API_TEST_PLAN.md](../API_TEST_PLAN.md)
- [TEST_EXECUTION_GUIDE.md](./TEST_EXECUTION_GUIDE.md)
