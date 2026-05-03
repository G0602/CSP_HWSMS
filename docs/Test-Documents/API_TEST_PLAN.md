# HSMS API Test Plan

## 1. Purpose

This plan defines how the Hardware Store Management System API will be tested across authentication, inventory, suppliers, sales, reports, health monitoring, and user administration. It is aligned to the current backend controllers, DTOs, authorization policies, and frontend API usage found in this repository.

## 2. Scope

Included API areas:

- `AuthController`
- `ProductController`
- `SuppliersController`
- `SalesController`
- `ReportsController`
- `UsersController`
- `/api/health` health check endpoint

Excluded from this plan:

- Frontend visual/UI testing
- Browser compatibility testing
- Infrastructure provisioning outside API reachability

## 3. Test Objectives

- Verify every exposed API endpoint returns the correct status code, payload shape, and validation behavior.
- Confirm role-based authorization works for `Admin`, `Manager`, and `Cashier`.
- Confirm business-critical flows work end-to-end: login, product management, supplier management, sale creation, reporting, and user administration.
- Confirm error handling for invalid input, missing records, duplicate data, and unsupported actions.
- Confirm health monitoring endpoint reflects service availability.

## 4. API Modules Under Test

| Module | Endpoints |
|---|---|
| Authentication | `POST /api/Auth/register`, `POST /api/Auth/login` |
| Products | `GET/POST /api/Product`, `GET /api/Product/inventory`, `GET /api/Product/low-stock`, `GET /api/Product/search`, `GET/PUT/DELETE /api/Product/{id}`, `PUT /api/Product/{id}/stock` |
| Suppliers | `GET/POST /api/suppliers`, `PUT/DELETE /api/suppliers/{id}` |
| Sales | `POST /api/Sales`, `GET /api/Sales/history`, `GET /api/Sales/{saleId}`, `GET /api/Sales/{saleId}/invoice` |
| Reports | `GET /api/reports/daily`, `GET /api/reports/monthly`, `GET /api/reports/low-stock`, `GET /api/reports/export?type=` |
| Users | `GET/POST /api/users`, `PUT /api/users/{id}/role`, `PUT /api/users/{id}/password`, `DELETE /api/users/{id}` |
| Health | `GET /api/health` |

## 5. Test Types

### 5.1 Functional Testing

- Request and response validation
- CRUD behavior
- Search and filtering
- CSV export behavior
- Auth token generation and reuse

### 5.2 Negative Testing

- Empty or malformed payloads
- Invalid route parameters
- Invalid query parameter values
- Missing token
- Wrong role for protected endpoint
- Duplicate username or supplier conflicts
- Not-found resource access

### 5.3 Security and Authorization Testing

- Anonymous access allowed only for login, register, and health
- `Cashier` access limited from manager/admin-only endpoints
- `Manager` blocked from admin-only user and delete operations
- Token refresh response after self role update

### 5.4 Integration and Data Consistency Testing

- Product and supplier linkage
- Sale creation affects inventory expectations
- Reports reflect created sales data
- Invoice retrieval matches stored sale

### 5.5 Reliability Checks

- Health endpoint returns structured status
- Repeated requests remain stable
- Concurrent or rapid stock update execution does not corrupt data

## 6. Role Access Matrix

| Policy / Endpoint Group | Admin | Manager | Cashier |
|---|---|---|---|
| `InventoryRead` | Yes | Yes | Yes |
| `InventoryManagerRead` | Yes | Yes | No |
| `InventoryWrite` | Yes | Yes | No |
| `InventoryDelete` | Yes | No | No |
| `SalesCreate` | Yes | Yes | Yes |
| `SalesRead` | Yes | Yes | No |
| `UsersManage` | Yes | No | No |
| Anonymous endpoints | Yes | Yes | Yes |

## 7. Test Environment

Recommended environments:

- Local API base URL: `http://localhost:5162`
- Optional deployed API base URL via Postman environment override
- MySQL database with test-safe seed data

Required configuration:

- Valid database connection string
- JWT secret, issuer, and audience configured
- At least one user per role for authorization tests

Recommended test accounts:

- `admin_user / Password@123`
- `manager_user / Password@123`
- `cashier_user / Password@123`

## 8. Test Data Strategy

Use dedicated test records with unique suffixes where possible:

- Product SKU: `PT-{{timestamp}}`
- Supplier name: `Supplier {{timestamp}}`
- Username: `user_{{timestamp}}`

Keep the following reusable IDs in Postman environment variables:

- `productId`
- `saleId`
- `supplierId`
- `userId`

## 9. Entry Criteria

- Backend builds successfully
- API is reachable
- Database connection is healthy
- Swagger or controller routes match current implementation
- Test users and minimum seed data are available

## 10. Exit Criteria

- All critical and high-priority endpoint tests pass
- Authorization matrix is verified
- No unresolved blocker on login, product, supplier, sales, reports, or users flows
- CSV export and health checks behave as expected

## 11. Endpoint Test Scenarios

### 11.1 Authentication

`POST /api/Auth/register`

- Register user with valid username, password, and role
- Register user with empty username
- Register user with password shorter than 8 characters
- Register user with unsupported role
- Register duplicate username
- Verify token and role are returned on success

`POST /api/Auth/login`

- Login with valid credentials
- Login with wrong password
- Login with unknown username
- Login with empty username or password
- Verify JWT token, expiry, and role are returned

### 11.2 Products

`GET /api/Product`

- Admin, Manager, Cashier can retrieve products
- Unauthorized request returns `401`

`GET /api/Product/inventory`

- Admin and Manager can retrieve inventory metadata
- Cashier is denied
- Verify `isLowStock` is populated correctly

`GET /api/Product/low-stock`

- Returns only items below configured threshold
- Verify boundary behavior when quantity equals threshold

`GET /api/Product/search`

- Query returns matching records
- Empty query returns `400`
- Limit handling is sane

`POST /api/Product`

- Create product with valid payload
- Reject zero or negative price
- Reject negative quantity
- Reject invalid supplier id
- Reject caller without write permission

`GET /api/Product/{id}`

- Existing product returns `200`
- Missing product returns `404`

`PUT /api/Product/{id}`

- Update valid product
- Reject invalid supplier id
- Missing product returns `404`

`PUT /api/Product/{id}/stock`

- Update stock with valid quantity
- Reject negative quantity
- Missing product returns `404`

`DELETE /api/Product/{id}`

- Admin can delete existing product
- Manager and Cashier are denied
- Missing product returns `404`

### 11.3 Suppliers

`GET /api/suppliers`

- Admin, Manager, Cashier can list suppliers

`POST /api/suppliers`

- Create supplier with valid name
- Reject blank name
- Handle duplicate/invalid-operation conflict

`PUT /api/suppliers/{id}`

- Update valid supplier
- Reject blank name
- Missing supplier returns `404`

`DELETE /api/suppliers/{id}`

- Delete unlinked supplier
- Reject supplier linked to records with `409`
- Missing supplier returns `404`

### 11.4 Sales

`POST /api/Sales`

- Create sale with at least one item
- Reject empty items list
- Reject invalid or out-of-stock product according to repository behavior
- Verify `soldBy`, totals, and line items

`GET /api/Sales/history`

- Admin and Manager can query history
- Cashier denied
- Invalid `limit <= 0` returns `400`
- Validate filtering by transaction id and date range

`GET /api/Sales/{saleId}`

- Existing sale returns details
- Missing sale returns `404`

`GET /api/Sales/{saleId}/invoice`

- Existing sale returns invoice
- Missing sale returns `404`

### 11.5 Reports

`GET /api/reports/daily`

- Returns daily totals for Admin and Manager

`GET /api/reports/monthly`

- Returns monthly totals for Admin and Manager

`GET /api/reports/low-stock`

- Returns low-stock inventory report for Admin and Manager
- Cashier denied

`GET /api/reports/export`

- Export `daily`, `monthly`, and `low-stock`
- Unsupported type returns `400`
- Verify CSV content type and filename

### 11.6 Users

`GET /api/users`

- Admin gets sanitized list
- Manager and Cashier denied

`POST /api/users`

- Admin creates users for all allowed roles
- Reject empty username
- Reject short password
- Reject invalid role
- Reject duplicate username

`PUT /api/users/{id}/role`

- Admin updates another user role
- Admin updates own role and receives refreshed auth payload
- Missing user returns `404`
- Invalid role returns `400`

`PUT /api/users/{id}/password`

- Admin resets user password with matching passwords
- Password and confirm password must match, else returns `400`
- Password must be at least 8 characters, else returns `400`
- Missing user returns `404`
- Empty password fields return `400`

`DELETE /api/users/{id}`

- Admin deletes user
- Missing user returns `404`

### 11.7 Health

`GET /api/health`

- Returns `healthy` when database is reachable
- Returns `unhealthy` when dependency check fails
- Response contains `status`, `timestamp`, and `checks`

## 12. Priority Matrix

| Priority | Areas |
|---|---|
| Critical | Login, token use, product read/write, create sale, reports access, user-role authorization |
| High | Supplier CRUD, inventory stock update, invoice retrieval, CSV export |
| Medium | Search, filtered history queries, low-stock threshold edge cases |
| Low | Repeated request resilience, optional contact info formatting |

## 13. Execution Approach in Postman

- Use the generated collection in `postman/HSMS_API.postman_collection.json`
- Use the environment in `postman/HSMS_Local.postman_environment.json`
- Run login first to populate `accessToken`
- Re-run folders with different role accounts to validate authorization
- Use Collection Runner for regression cycles

Recommended execution order:

1. Health
2. Auth
3. Suppliers
4. Products
5. Sales
6. Reports
7. Users
8. Negative authorization sweep

## 14. Automation Recommendations

- Run the Postman collection in CI with Newman after backend deployment or local startup.
- Maintain separate environments for local, staging, and production-like testing.
- Refresh the generated collection whenever controller routes or DTOs change by re-running the generator script.

## 15. Risks and Assumptions

- Some flows depend on seeded or pre-existing database data.
- Product creation currently returns the DTO body rather than a generated identifier, so follow-up tests may require a manually maintained `productId` or a lookup step.
- Integration behavior for sales validation depends on repository/database state, not only controller validation.
