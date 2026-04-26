# HSMS.ApiTests - REST API Testing with RestSharp

## 📋 Project Overview

This project demonstrates **API testing** using **RestSharp** and **xUnit** for the Hardware Store Management System (HSMS) backend. It contains comprehensive automated tests for authentication and product management endpoints.

---

## 🎯 Project Structure

```
HSMS.ApiTests/
├── Helpers/
│   ├── ApiClient.cs              # Reusable REST client wrapper
│   └── ApiTestConstants.cs        # Test constants and endpoints
├── AuthApiTests.cs               # Member 1: Authentication positive tests
├── AuthApiNegativeTests.cs       # Member 2: Authentication error handling
├── ProductApiTests.cs            # Member 3: Product CRUD operations
├── ProductApiNegativeTests.cs    # Member 4: Product edge cases
└── HSMS.ApiTests.csproj         # Project configuration
```

---

## 👥 Member Contributions

### Member 1 - `AuthApiTests.cs`
**Focus:** Authentication API - Positive Scenarios
- **7 test cases** covering successful login workflows
- Tests valid credentials, response validation, token generation
- Verifies response contains required fields (userId, username, role, accessToken)
- Includes performance testing (response time < 2 seconds)
- **Key Tests:**
  - Login with valid admin credentials
  - Login with valid manager credentials  
  - Response structure validation
  - Token presence verification
  - User metadata validation

### Member 2 - `AuthApiNegativeTests.cs`
**Focus:** Authentication API - Error Handling & Security
- **13 test cases** covering error conditions and edge cases
- Tests invalid credentials, missing fields, malicious input
- Validates proper HTTP status codes (401, 400)
- Includes security testing (SQL injection attempts)
- **Key Tests:**
  - Wrong password rejection (401)
  - Non-existent user handling
  - Empty username/password validation
  - Missing required fields detection
  - SQL injection prevention
  - Brute force simulation (multiple failed attempts)
  - Case sensitivity handling

### Member 3 - `ProductApiTests.cs`
**Focus:** Product API - CRUD Operations (Positive Scenarios)
- **12 test cases** covering product management success paths
- Tests product retrieval, creation, and data validation
- Verifies response structure and HTTP status codes
- Includes performance testing
- **Key Tests:**
  - Get all products (200 OK)
  - Get specific product by ID
  - Create new product (201 Created)
  - Product data integrity
  - Multiple product creation
  - Price and stock validation
  - Supplier reference handling
  - Response time performance

### Member 4 - `ProductApiNegativeTests.cs`
**Focus:** Product API - Edge Cases & Validation
- **19 test cases** covering error conditions and boundary cases
- Tests invalid input, missing fields, and extreme values
- Validates business logic constraints
- Includes security and data validation tests
- **Key Tests:**
  - Invalid product ID (404 Not Found)
  - Negative/zero IDs handling
  - Non-numeric ID validation
  - Missing required fields (name, price, stock)
  - Negative price rejection
  - Zero price rejection
  - Extremely long names
  - Invalid supplier references
  - Boundary value testing (very high price/stock)
  - SQL injection in product name
  - Special character handling
  - Duplicate name handling

---

## 🔧 Setup Instructions

### Prerequisites
- .NET 8.0 SDK or higher
- Visual Studio 2022 (or VS Code)
- Running backend API at `http://localhost:5162`

### Installation

1. **Restore Dependencies**
   ```powershell
   cd backend
   dotnet restore
   ```

2. **Build the Project**
   ```powershell
   dotnet build HSMS.ApiTests/HSMS.ApiTests.csproj
   ```

### Running Tests

**Run all tests:**
```powershell
dotnet test HSMS.ApiTests/HSMS.ApiTests.csproj
```

**Run specific test class:**
```powershell
dotnet test HSMS.ApiTests/HSMS.ApiTests.csproj --filter "ClassName=HSMS.ApiTests.AuthApiTests"
```

**Run with verbose output:**
```powershell
dotnet test HSMS.ApiTests/HSMS.ApiTests.csproj --verbosity=detailed
```

**Run with code coverage:**
```powershell
dotnet test HSMS.ApiTests/HSMS.ApiTests.csproj /p:CollectCoverage=true
```

---

## 🧪 Test Coverage Summary

| Category | Member | Test File | Test Count | Coverage |
|----------|--------|-----------|-----------|----------|
| Authentication (Positive) | 1 | AuthApiTests.cs | 7 | Happy path scenarios |
| Authentication (Negative) | 2 | AuthApiNegativeTests.cs | 13 | Error handling & security |
| Products (Positive) | 3 | ProductApiTests.cs | 12 | CRUD operations |
| Products (Negative) | 4 | ProductApiNegativeTests.cs | 19 | Edge cases & validation |
| **TOTAL** | - | - | **51** | **Complete coverage** |

---

## 🔑 Test Credentials

Default test account for authentication tests:

| Field | Value |
|-------|-------|
| Username | `admin` |
| Password | `change-admin-password` |
| Role | Admin |
| API Endpoint | `http://localhost:5162/api/auth/login` |

Alternative test account:

| Field | Value |
|-------|-------|
| Username | `manager` |
| Password | `change-manager-password` |
| Role | Manager |

---

## 📊 Key Testing Scenarios

### Authentication Tests
✅ Valid login with correct credentials  
✅ Invalid credentials rejection  
✅ Missing field validation  
✅ Response structure validation  
✅ Token generation verification  
✅ Security testing (SQL injection prevention)  
✅ Performance baseline establishment  

### Product Tests
✅ Retrieve product lists  
✅ Get product by ID  
✅ Create new products  
✅ Validate product data integrity  
✅ Test boundary values  
✅ Handle non-existent resources (404)  
✅ Validate business logic constraints  
✅ Prevent invalid/malicious input  

---

## 🛠️ Technology Stack

| Tool | Version | Purpose |
|------|---------|---------|
| RestSharp | 107.3.0 | HTTP client library |
| xUnit | 2.6.6 | Unit testing framework |
| .NET | 8.0 | Runtime platform |
| C# | 12 | Programming language |

---

## 📝 Test Naming Convention

Tests follow the format: `MethodName_Scenario_ExpectedResult`

Examples:
- `Login_WithValidCredentials_Should_Return_200_And_AccessToken`
- `GetProductById_WithInvalidId_Should_Return_404`
- `CreateProduct_WithNegativePrice_Should_Return_400`

---

## 🎓 Learning Outcomes

After completing this assignment, you should understand:

1. **API Testing Fundamentals**
   - How to test REST endpoints
   - HTTP request/response validation
   - Status code assertions

2. **RestSharp Framework**
   - Creating HTTP clients
   - Sending requests (GET, POST, PUT, DELETE)
   - Handling responses and errors
   - Token-based authentication

3. **Test Design**
   - Positive vs. negative test cases
   - Edge case identification
   - Boundary value testing
   - Security testing basics

4. **xUnit Framework**
   - Creating test cases with `[Fact]`
   - Organizing tests with namespaces
   - Using assertions (Assert.Equal, Assert.True, etc.)
   - Test execution and reporting

---

## 📌 Important Notes

- All tests are **independent** and can run in any order
- Tests use **synchronous wrappers** for xUnit compatibility
- Each test is **self-contained** with setup and assertions
- Performance tests have **reasonable timeouts** (< 2 seconds)
- Tests validate **HTTP status codes** and **response structure**
- **No external dependencies** required beyond RestSharp and xUnit

---

## ✅ Verification Checklist

Before submission, verify:

- [ ] Project builds without errors: `dotnet build HSMS.ApiTests/HSMS.ApiTests.csproj`
- [ ] All 51 tests are present (7 + 13 + 12 + 19)
- [ ] Tests can be discovered: `dotnet test --list-tests HSMS.ApiTests/HSMS.ApiTests.csproj`
- [ ] Backend API is running at `http://localhost:5162`
- [ ] Each member's tests are in separate files
- [ ] All assertions are meaningful and specific
- [ ] Response time tests are included
- [ ] Security tests (SQL injection) are included

---

## 📞 Support

If tests fail:

1. **Verify backend is running:** Check `http://localhost:5162/api/health`
2. **Check credentials:** Ensure admin/manager users exist
3. **Review response:** Add debug output to see actual API responses
4. **Check network:** Ensure backend is accessible
5. **Rebuild project:** `dotnet clean && dotnet build`

---

## 📄 Assignment Details

- **Course:** Software Testing & Quality Assurance
- **Assignment:** Test Tool Demonstration (API Testing)
- **Tools Used:** RestSharp, xUnit
- **System Under Test:** Hardware Store Management System (HSMS)
- **Submission Date:** [Your Submission Date]
- **Group Size:** 4 members (roles distributed as per test files)

---


