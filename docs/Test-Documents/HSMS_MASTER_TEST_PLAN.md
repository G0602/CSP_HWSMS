# HSMS Master Test Plan

## Summary

This document defines the active high-level testing strategy for the current HSMS repository. It covers backend, frontend, API, E2E, and load testing for the implemented application surface rather than older sprint-only deliverables.

## Scope

### In scope

- authentication and session handling
- role-based authorization
- products and inventory
- suppliers
- sales, transactions, and invoices
- reports and CSV export
- user administration
- backend health monitoring
- frontend route protection and backend connectivity behavior

### Out of scope

- mobile apps
- external payment gateway testing
- infrastructure provisioning outside app behavior
- third-party penetration testing engagements

## System Areas Under Test

### Frontend

- login page
- protected/public-only route behavior
- dashboard
- inventory
- sales
- suppliers
- transaction history
- invoice preview
- daily report page
- users page
- backend health banner

### Backend

- `AuthController`
- `ProductController`
- `SuppliersController`
- `SalesController`
- `ReportsController`
- `UsersController`
- health endpoint
- auth services and JWT handling
- repository and persistence behavior

## Test Layers

### Backend unit / integration / security

Location:

- `backend/HSMS.Tests`

Current validated suite size:

- `296` passing tests

Covers:

- controllers
- services and validation rules
- configuration behavior
- repository/database behavior
- security and authorization checks

### API tests

Location:

- `backend/HSMS.ApiTests`

Current validated suite size:

- `194` passing tests

Covers:

- auth
- products
- sales
- reports
- suppliers
- users

### Frontend tests

Location:

- `frontend/HWSMS_UI`

Current validated suite size:

- `17` passing tests

Covers:

- role helpers
- auth service behavior
- backend health banner
- protected route behavior
- public-only route behavior

### E2E tests

Location:

- `backend/HSMS.E2E`

Status:

- optional and environment-driven

### Load and collection-based testing

- JMeter artifacts in `jmeter/`
- Postman artifacts in `postman/`
- source-aware generator scripts in `scripts/`

## Primary Quality Risks

- unauthorized access to admin or manager features
- incorrect stock changes after sales or manual updates
- incorrect totals in transactions, invoices, or reports
- failure to reflect backend outages or auth failures clearly in the UI
- data integrity issues across product, supplier, sale, and user flows

## Execution Priorities

### Priority 1

- login and token usage
- role access enforcement
- product CRUD and stock update
- supplier CRUD
- sale creation
- invoice retrieval
- report retrieval and export
- user management operations

### Priority 2

- filtering and search
- low-stock threshold boundaries
- frontend loading/error/empty states
- connectivity banner behavior
- concurrency-sensitive repository cases

### Priority 3

- extended browser coverage
- print-format refinements
- non-critical UX polish checks

## Environment Requirements

- reachable MySQL database for DB-backed tests
- valid backend JWT settings
- reachable backend API for API and E2E runs
- frontend configured with the intended API base URL
- deterministic seed or reset strategy when tests rely on mutable data

## Recommended Test Accounts

Use dedicated test-safe accounts aligned with your configured environment. For local development or integration testing, match the credentials you seed through:

- `Password__Admin`
- `Password__Manager`
- `Password__Cashier`

## Related Docs

- [TESTING_OVERVIEW.md](./TESTING_OVERVIEW.md)
- [API_TEST_PLAN.md](./API_TEST_PLAN.md)
- [Guides/TEST_EXECUTION_GUIDE.md](./Guides/TEST_EXECUTION_GUIDE.md)
- [Guides/DELIVERY_CHECKLIST.md](./Guides/DELIVERY_CHECKLIST.md)
