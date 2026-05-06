# README_Tests

This document is the testing submission README requested in `Group Activity-Testing tool evaluation.html`.

## 1. Project Context

**Project:** Hardware Store Management System (HWSMS)  
**Codebase Type:** Full-stack web application  
**Main Scope Tested:** Backend business logic, database-backed behavior, and REST API endpoints

The project manages core hardware store workflows such as authentication, products, suppliers, sales, users, and reports. Because the system contains both internal business rules and externally consumed HTTP endpoints, the testing work in this repository focuses on:

- backend unit and integration-style verification in `backend/HSMS.Tests`
- API-level automated testing in `backend/HSMS.ApiTests`

## 2. Testing Areas Used For This Submission

This repository currently contains multiple testing assets. For the group testing submission, the main code demonstrations should be taken from these two areas:

### Tool Area A: Backend automated tests
- **Project:** `backend/HSMS.Tests`
- **Primary stack:** xUnit + Moq
- **Why it fits this codebase:** The backend contains service logic, validation rules, controller behavior, authorization, and database interactions that benefit from automated assertions and mocked dependencies.

### Tool Area B: API automated tests
- **Project:** `backend/HSMS.ApiTests`
- **Primary stack:** RestSharp + xUnit
- **Why it fits this codebase:** The system exposes REST endpoints, so API tests are useful to verify request/response behavior, status codes, authentication, validation, and negative scenarios from a consumer point of view.

## 3. Important Scope Note

The repository also contains `backend/HSMS.E2E`, which uses Selenium. The assignment brief in `Group Activity-Testing tool evaluation.html` explicitly says **"No Selenium or Postman"**, so that E2E project should **not** be presented as part of the official submission unless the module staff approve an exception.

## 4. Group Member Ownership

| Student | Feature / Focus Area | Test Area | Main Files |
|---|---|---|---|
| M.Gowrishan | Controller and service behavior with mocks | Backend automated tests | `backend/HSMS.Tests/Unit/Controllers/AuthControllerTests.cs`, `backend/HSMS.Tests/Unit/Services/AuthenticationServiceTests.cs`, `backend/HSMS.Tests/Unit/Services/JwtTokenServiceTests.cs` |
| P.Shadhurshan | Validation, security, and database integrity scenarios | Backend automated tests | `backend/HSMS.Tests/Unit/Validation/ProductBasicTests.cs`, `backend/HSMS.Tests/Security/SqlInjectionProtectionTests.cs`, `backend/HSMS.Tests/Integration/Database/DataIntegrityTests.cs`, `backend/HSMS.Tests/Integration/Database/ConcurrentStockUpdateTests.cs` |
| Falil M.N.M | Authentication API workflows and negative cases | API automated tests | `backend/HSMS.ApiTests/Auth/AuthApiTests.cs`, `backend/HSMS.ApiTests/Auth/AuthApiNegativeTests.cs`, `backend/HSMS.ApiTests/Auth/AuthRegisterTests.cs` |
| Y.Shanujen | Product, sales, reports, users, and suppliers endpoint coverage | API automated tests | `backend/HSMS.ApiTests/Products/ProductApiTests.cs`, `backend/HSMS.ApiTests/Products/ProductApiNegativeTests.cs`, `backend/HSMS.ApiTests/Sales/SalesApiTests.cs`, `backend/HSMS.ApiTests/Reports/ReportsApiTests.cs`, `backend/HSMS.ApiTests/Users/UsersApiTests.cs`, `backend/HSMS.ApiTests/Suppliers/SuppliersApiTests.cs` |

## 5. What Each Student Should Demo

Each student should demonstrate actual running code for the feature area they own.

### M.Gowrishan
- mocked controller/service tests
- assertions on returned results and dependency calls
- example focus: authentication flow and JWT creation logic

### P.Shadhurshan
- validation and edge-case coverage
- security-focused tests such as SQL injection protection
- integration-style checks for data integrity or concurrent updates

### Falil M.N.M
- successful and failed authentication API requests
- HTTP status-code assertions
- response payload validation for login/register flows

### Y.Shanujen
- CRUD and negative testing for business endpoints
- validation/error behavior for products, sales, reports, users, or suppliers
- broader endpoint coverage beyond authentication

## 6. How To Run The Demo Code

### Backend automated tests

```bash
cd backend
dotnet test HSMS.Tests/HSMS.Tests.csproj
```

### API automated tests

Start the backend API first:

```bash
cd backend
dotnet run --project HSMS.API
```

Then run the API test suite in another terminal:

```bash
cd backend
dotnet test HSMS.ApiTests/HSMS.ApiTests.csproj
```

### Useful targeted demo commands

```bash
cd backend
dotnet test HSMS.Tests/HSMS.Tests.csproj --filter "FullyQualifiedName~AuthenticationServiceTests"
dotnet test HSMS.Tests/HSMS.Tests.csproj --filter "FullyQualifiedName~SqlInjectionProtectionTests"
dotnet test HSMS.ApiTests/HSMS.ApiTests.csproj --filter "FullyQualifiedName~AuthApiTests"
dotnet test HSMS.ApiTests/HSMS.ApiTests.csproj --filter "FullyQualifiedName~ProductApiTests"
```