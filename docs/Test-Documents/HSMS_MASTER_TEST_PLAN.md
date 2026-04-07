# HSMS Master Test Plan

## 1. Document Purpose

This document defines a professional end-to-end testing strategy for the Hardware Store Management System (HSMS) across both frontend and backend. It covers the current application modules implemented in this repository and provides a structured plan for unit, integration, API, UI, system, regression, security, and load testing.

The goal is to ensure HSMS is functionally correct, secure, reliable, and production-ready for real store operations such as authentication, inventory management, supplier management, sales processing, reporting, invoice generation, and user administration.

## 2. Project Overview

HSMS is a full-stack web application with:

- Frontend: React 19, TypeScript, Vite
- Backend: ASP.NET Core 8 REST API
- Database: MySQL
- Auth model: JWT with role-based authorization
- User roles: `Admin`, `Manager`, `Cashier`

## 3. System Modules In Scope

### 3.1 Frontend modules

- Authentication pages: Login, Register
- Access control pages: Protected route handling, Access Denied
- Dashboard: Product dashboard
- Inventory: inventory view, low-stock awareness, manual stock update
- Product management: create, edit, delete, search
- Supplier management: create, update, delete, list
- Sales processing: create sale, add sale items, validate stock availability
- Transaction history: filter and view details
- Invoice preview: invoice retrieval and printing workflow
- Reports: daily sales, monthly sales, low-stock report, CSV export
- User management: create users, update roles, delete users
- Backend health awareness: frontend health banner and polling hook

### 3.2 Backend modules

- `AuthController`
- `ProductController`
- `SuppliersController`
- `SalesController`
- `ReportsController`
- `UsersController`
- Health check endpoint
- Authentication services
- JWT generation and password hashing
- Repository layer for products, suppliers, sales, and users

### 3.3 Out of scope

- Mobile app testing
- Native desktop testing
- Payment gateway testing
- Infrastructure provisioning tests outside application behavior
- Penetration testing by external security vendors

## 4. Test Objectives

- Verify all critical business workflows work correctly from UI to database.
- Verify role-based access restrictions are enforced in both frontend and backend.
- Verify business data integrity for products, suppliers, sales, reports, and users.
- Verify the system handles invalid input, edge cases, and failures safely.
- Verify the UI behaves correctly for success, error, loading, and empty states.
- Verify integrations between frontend services, API endpoints, and MySQL persistence.
- Verify acceptable performance under realistic concurrent user activity.
- Build a repeatable regression suite for future releases.

## 5. Quality Risks

### 5.1 Business-critical risks

- Incorrect stock quantities after sales or manual stock updates
- Unauthorized users accessing restricted features
- Invalid totals in sales, invoices, or reports
- Data loss or corruption during supplier, product, or user management
- Failed or misleading CSV exports
- Session handling issues after login, logout, expiry, or self-role update

### 5.2 Technical risks

- Frontend and backend role rules becoming inconsistent
- Repository/database behavior differing from controller expectations
- Concurrency issues during stock updates or simultaneous sales
- Insufficient validation for malformed route, query, or body inputs
- Health checks or startup configuration failures going undetected
- Performance degradation under concurrent read/write load

## 6. Test Levels and Strategy

### 6.1 Unit testing

Purpose:

- Validate isolated business logic, controller validation, auth helpers, route guards, and frontend utility behavior.

Backend unit test targets:

- `SaleCalculator`
- `PasswordHasher`
- `JwtTokenService`
- controller validation branches
- role normalization and authorization helper behavior
- report export formatting logic

Frontend unit test targets:

- auth/session helpers in `authService.ts`
- route protection logic in `ProtectedRoute.tsx` and `PublicOnlyRoute.tsx`
- role helper functions in `roles.ts`
- API base URL resolution in `config/api.ts`
- component-level rendering/behavior for forms, tables, banners, modals, and empty/error states

Recommended tools:

- Backend: xUnit, Moq, coverlet
- Frontend: Vitest, React Testing Library, jsdom

### 6.2 Integration testing

Purpose:

- Validate cooperation between application layers and persistence with realistic test data.

Backend integration scope:

- repository operations against test MySQL database
- sale creation affecting stock records correctly
- supplier-product relationships
- report queries reflecting inserted sales data
- user CRUD and role updates persisted correctly

Frontend integration scope:

- page + service + router interaction with mocked API
- login flow storing session and redirecting correctly
- pages handling `401`, `403`, `404`, and validation messages
- CSV export flow and file download behavior
- health polling and disconnected-state behavior

Recommended tools:

- Backend: xUnit with dedicated test database
- Frontend: Vitest + React Testing Library + mocked Axios or MSW

### 6.3 API testing

Purpose:

- Validate endpoint contracts, status codes, payloads, auth, and negative scenarios independent of the UI.

Scope:

- all controllers and health endpoint
- happy paths, validation errors, authorization failures, not-found cases, and conflict cases
- request/response schema validation
- token handling and role matrix validation

Recommended tools:

- Postman collection and environments already supported by the repository
- Newman for CI execution

### 6.4 End-to-end system testing

Purpose:

- Validate complete workflows through the browser and full stack.

Scope:

- login to task completion flows
- inventory, supplier, sales, reports, transaction history, invoice, and user administration journeys
- route protection and access denied behavior

Recommended tools:

- Playwright or Cypress

### 6.5 Regression testing

Purpose:

- Re-run stable high-value tests before release and after major changes.

Regression suite should include:

- login/logout/session checks
- role access checks
- product CRUD
- inventory stock updates
- supplier CRUD
- sale creation
- transaction details and invoice
- reports and CSV export
- user administration
- API health endpoint

### 6.6 Security testing

Purpose:

- Validate authentication, authorization, session handling, and misuse resistance.

Scope:

- anonymous access restrictions
- role-based access enforcement
- token expiry behavior
- invalid or tampered JWT
- password rules and password storage
- over-privileged UI navigation attempts
- direct API access to restricted endpoints
- duplicate username/supplier conflict behavior

### 6.7 Performance and load testing

Purpose:

- Measure API behavior and platform stability under concurrent user demand.

Scope:

- login throughput
- inventory and product read endpoints
- transaction history and reports
- controlled write tests for sale creation and stock updates
- health endpoint responsiveness under load

Recommended tools:

- JMeter plan generation already exists in this repository

## 7. Test Environment Strategy

### 7.1 Environments

- Local development environment
- Shared QA / staging environment
- Optional production-smoke environment for post-deploy sanity checks

### 7.2 Required configuration

- valid MySQL test database
- JWT secret, issuer, and audience configured
- backend API reachable
- frontend configured against intended API base URL
- deterministic seed data for repeatable execution

### 7.3 Recommended test accounts

- `admin_user / Password@123`
- `manager_user / Password@123`
- `cashier_user / Password@123`

## 8. Test Data Strategy

- Use dedicated test-safe database or schema.
- Use unique values for mutable entities: product SKU `PT-{timestamp}`, supplier name `SUP-{timestamp}`, username `user_{timestamp}`.
- Keep reusable environment variables for `productId`, `supplierId`, `saleId`, and `userId`.
- Reset and seed test data before repeatable integration, API, and E2E execution.
- Never run destructive load tests against production data.

## 9. Role Access Coverage Matrix

| Feature Area | Admin | Manager | Cashier |
|---|---|---|---|
| Login / Register | Yes | Yes | Yes |
| Product list/search | Yes | Yes | Yes |
| Inventory page | Yes | Yes | No |
| Product create/update | Yes | Yes | No |
| Product delete | Yes | No | No |
| Supplier list | Yes | Yes | Yes |
| Supplier create/update/delete | Yes | Yes | No |
| Sales create | Yes | Yes | Yes |
| Transaction history/details/invoice | Yes | Yes | No |
| Reports view/export | Yes | Yes | No |
| User administration | Yes | No | No |
| Access denied route | Yes | Yes | Yes |

## 10. Functional Coverage by Module

### 10.1 Authentication

Core scenarios:

- register with valid username, password, and role
- register with empty username
- register with short password
- register with invalid role
- register duplicate username
- login with valid credentials
- login with invalid username or password
- logout clears session and blocks protected routes
- expired session denies protected access
- self-role update refreshes session token correctly

### 10.2 Route protection and access control

Core scenarios:

- unauthenticated users redirected to login
- authenticated users blocked from public-only pages when session exists
- users without required role redirected to access denied page
- UI menu visibility aligns with role capabilities
- direct URL access to restricted routes is blocked
- backend still blocks forbidden API calls even if frontend checks are bypassed

### 10.3 Product management

Core scenarios:

- list products successfully
- create product with valid supplier/no supplier
- reject product with invalid price, quantity, or supplier
- update product successfully
- delete product only as admin
- fetch product by id
- search by name/category/SKU
- search with empty query returns error

Edge cases:

- long names
- duplicate or similar SKUs if business rule later enforces uniqueness
- category casing/spacing variations

### 10.4 Inventory management

Core scenarios:

- inventory page loads products with `isLowStock`
- low-stock badge and counts are correct
- critical-stock display is correct
- manual stock increase works
- manual stock decrease works
- negative resulting stock is blocked
- stock update errors shown correctly
- low-stock popup appears only when applicable

Edge cases:

- quantity exactly at threshold
- quantity zero
- concurrent stock updates

### 10.5 Supplier management

Core scenarios:

- list suppliers
- add supplier with valid name and contact info
- reject blank name
- update supplier successfully
- delete supplier successfully when unlinked
- reject delete with linked records
- duplicate supplier conflict handled and shown correctly

### 10.6 Sales processing

Core scenarios:

- create sale with one item
- create sale with multiple items
- reject empty item list
- reject invalid product id
- reject out-of-stock quantity
- verify total amount and line subtotals
- verify stock is reduced after successful sale
- verify `soldBy` uses authenticated user

Edge cases:

- repeated same product in payload
- quantity of one
- high-volume basket

### 10.7 Transaction history and details

Core scenarios:

- load history with default limit
- filter by transaction id
- filter by date range
- reject invalid limit
- view transaction details
- show no-data state when filters return nothing
- open invoice from transaction history

### 10.8 Invoice preview

Core scenarios:

- load invoice for valid transaction
- reject invalid transaction id in route
- show not-found state for unknown invoice
- render totals and all items correctly
- print action triggers browser print flow

### 10.9 Reports and export

Core scenarios:

- daily report loads
- monthly report loads
- low-stock report loads
- grand totals are computed correctly in UI
- monthly chart renders expected bars
- daily/monthly/low-stock CSV export works
- exported file name and content type are correct
- unsupported export type returns `400`

### 10.10 User administration

Core scenarios:

- admin loads users list
- create admin/manager/cashier user
- reject empty username
- reject short password
- reject invalid role
- reject duplicate username
- update another user's role
- update own role and receive refreshed session token
- delete user
- delete unknown user returns not found

### 10.11 Health monitoring

Core scenarios:

- health endpoint returns healthy when database is reachable
- health endpoint returns unhealthy when database is unavailable
- frontend health banner reflects outage
- polling stops or recovers correctly after backend recovery

## 11. Non-Functional Test Coverage

### 11.1 Performance

- API average and percentile response times for login, reads, reports, and transaction retrieval
- throughput under 50, 100, and 250 virtual users
- database response stability under concurrent reads
- controlled write-load behavior for sales and stock updates

### 11.2 Reliability

- repeated request stability
- restart and reconnect behavior
- graceful handling of backend unavailability
- resilience of UI loading and error states

### 11.3 Usability

- clear validation messages
- intuitive empty-state and error-state messaging
- route redirects make sense for unauthorized users
- forms preserve or reset values appropriately after success/failure

### 11.4 Compatibility

- desktop browsers: Chrome, Edge, Firefox
- responsive verification for typical laptop and tablet widths

## 12. Recommended Automation Split

| Test Layer | Recommended Automation Focus |
|---|---|
| Backend unit | Controllers, services, business logic, auth helpers |
| Backend integration | Repository + DB persistence |
| API | Postman/Newman contract and auth suite |
| Frontend unit | Services, route guards, helpers, isolated components |
| Frontend integration | Page flows with mocked API |
| E2E | Cross-role critical business journeys |
| Load | JMeter API load profiles |

Suggested release gate:

- backend unit tests pass
- frontend unit/integration tests pass
- critical API suite passes
- critical E2E smoke suite passes
- no open severity 1 or severity 2 defects

## 13. Recommended Priority Matrix

### Priority 1

- login/logout/session expiry
- role-based authorization
- product CRUD
- stock update
- supplier CRUD
- create sale
- invoice retrieval
- reports retrieval/export
- user administration

### Priority 2

- search and filtering
- UI empty/loading/error states
- health banner behavior
- route redirection logic
- concurrency checks

### Priority 3

- visual polish checks
- print formatting refinements
- extended browser/resolution coverage

## 14. Entry Criteria

- code builds successfully
- backend API starts successfully
- frontend starts successfully
- test database is reachable
- seed/reset scripts are available and verified
- required test accounts exist
- environments and secrets are configured

## 15. Exit Criteria

- all Priority 1 test cases pass
- agreed Priority 2 regression cases pass
- all critical defects are closed or formally accepted
- no unresolved auth, sales, stock, reporting, or data integrity blocker remains
- load test results are within agreed limits for the target environment

## 16. Defect Severity Guidance

- Severity 1: sales, stock, auth, or security failure causing major business impact
- Severity 2: important feature broken with workaround unavailable or risky
- Severity 3: partial function issue, UI defect, or low-risk validation issue
- Severity 4: cosmetic or documentation-only issue

## 17. Recommended Test Case Inventory

The following minimum inventory is recommended for a professional baseline:

- Unit tests: 60 to 100
- Integration tests: 25 to 40
- API tests: 60 to 90
- Frontend integration tests: 25 to 40
- E2E critical-path tests: 12 to 20
- Load/performance scenarios: 6 to 10
- Security/authorization scenarios: 20 to 30

## 18. Suggested Automation Backlog for This Repository

### 18.1 Backend

- expand existing `HSMS.Tests` coverage to all controller negative cases
- add health endpoint tests
- add report export CSV content assertions
- add deeper sales total and inventory deduction integration tests
- add auth/login controller tests and invalid token tests

### 18.2 Frontend

- introduce `Vitest` and `React Testing Library`
- test route guards and role helper functions
- test auth session persistence and expiry behavior
- test major pages with mocked API responses
- test CSV export behavior and invoice rendering
- test backend health banner and polling hook

### 18.3 End-to-end

- add Playwright suite for:
  - admin login to user creation
  - manager inventory and supplier flow
  - cashier sale flow
  - manager report export flow
  - forbidden route/access denied flow

### 18.4 Performance

- keep generated JMeter read-heavy suite
- add separate write-safe load profile for staging only
- add threshold assertions for response time and failure rate

## 19. Execution Plan by Phase

### Phase 1: Build confidence at code level

- run backend unit tests
- add missing frontend unit tests
- enforce coverage for critical business logic

### Phase 2: Validate persistence and contracts

- run repository integration tests
- run Postman/Newman API suite
- verify test data reset and seeding

### Phase 3: Validate business journeys

- run Playwright/Cypress E2E suite
- execute cross-role smoke tests

### Phase 4: Validate non-functional quality

- execute JMeter load tests
- review response times, errors, and DB behavior

## 20. Traceability to Current Repository Assets

This plan aligns with the current repository assets:

- backend automated tests: `backend/HSMS.Tests`
- API collection generation: `scripts/generate-postman-collection.js`
- load test generation: `scripts/generate-jmeter-test-plan.js`
- Postman assets: `postman/`
- JMeter assets: `jmeter/`
- current API and load notes: `docs/Test-Documents/API_TEST_PLAN.md`, `docs/Test-Documents/JMETER_LOAD_TEST.md`

## 21. Recommended Deliverables

- Master test plan document
- API test suite and execution report
- Backend unit/integration test report
- Frontend unit/integration test report
- E2E smoke/regression report
- Load test summary with metrics and observations
- Defect log with severity and retest status

## 22. Conclusion

This master plan gives HSMS a full professional testing structure instead of treating testing as only backend unit tests or only API checks. If followed, it will provide strong coverage for the most important operational risks in the current system: authentication, authorization, stock accuracy, sales integrity, reporting correctness, and role-based user workflows across both frontend and backend.
