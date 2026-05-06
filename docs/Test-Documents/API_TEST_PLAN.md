# HSMS API Test Plan

## Summary

This document defines the current API test scope for the HSMS backend. It is aligned to the controllers, routes, auth policies, and generated API artifacts that exist in the repository today.

## Controllers and Endpoints in Scope

| Area | Endpoint family |
|---|---|
| Auth | `POST /api/auth/login`, `POST /api/auth/register` |
| Products | `/api/product`, `/api/products`, inventory, low-stock, search, stock update |
| Suppliers | `/api/suppliers` |
| Sales | `/api/sales`, `/api/sales/history`, sale details, invoice |
| Reports | `/api/reports/*` |
| Users | `/api/users`, role update, password reset |
| Health | `GET /api/health` |

## Current Behavioral Notes

- self-registration is disabled and `POST /api/auth/register` should return `403`
- password reset is available through `PUT /api/users/{id}/password`
- role enforcement is policy-based in the backend and must be tested independently of the UI

## Authorization Matrix

| Policy / area | Admin | Manager | Cashier |
|---|---|---|---|
| Inventory read | Yes | Yes | Yes |
| Inventory manager read | Yes | Yes | No |
| Inventory write | Yes | Yes | No |
| Inventory delete | Yes | No | No |
| Sales create | Yes | Yes | Yes |
| Sales read | Yes | Yes | No |
| Users manage | Yes | No | No |

## Current Automated Suite

Location:

- `backend/HSMS.ApiTests`

Validated count:

- `194` passing tests

Coverage areas:

- `Auth/`
- `Products/`
- `Sales/`
- `Reports/`
- `Suppliers/`
- `Users/`

## Test Objectives

- verify status codes and payload behavior for active endpoints
- verify role-based authorization across Admin, Manager, and Cashier
- verify error handling for invalid input, duplicates, not-found cases, and unsupported operations
- verify health endpoint structure and dependency reporting
- verify business-critical flows across products, suppliers, sales, reports, and users

## Key Scenario Groups

### Authentication

- valid login
- invalid credentials
- malformed login payloads
- disabled self-registration returns `403`

### Products and inventory

- product list retrieval
- inventory and low-stock reads
- search behavior
- create/update/delete authorization
- stock update validation

### Suppliers

- supplier list
- create/update validation
- delete protection when linked data exists

### Sales

- valid sale creation
- invalid sale payloads
- history filtering
- sale details and invoice retrieval

### Reports

- daily and monthly reports
- analytics and summary endpoints
- low-stock reporting
- CSV export validation

### Users

- list users
- create user
- update role
- password reset
- delete user

### Health

- healthy response when DB is available
- dependency failure behavior when DB is unavailable

## Supporting Artifacts

- generated Postman collection: `postman/HSMS_API.postman_collection.json`
- local Postman environment: `postman/HSMS_Local.postman_environment.json`
- generator script: `scripts/generate-postman-collection.js`

## Execution Notes

- start the backend API before running the API test project
- use test-safe data and credentials
- re-generate the collection when controllers or DTOs change

## Related Docs

- [TESTING_OVERVIEW.md](./TESTING_OVERVIEW.md)
- [Guides/HSMS_APITESTS_README.md](./Guides/HSMS_APITESTS_README.md)
- [Guides/TEST_EXECUTION_GUIDE.md](./Guides/TEST_EXECUTION_GUIDE.md)
