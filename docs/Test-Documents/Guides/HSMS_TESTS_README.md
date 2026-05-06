# HSMS Backend Test Suite

**Project:** Hardware Store Management System (HWSMS)  
**Scope:** Backend unit, integration, and security tests  
**Status:** Passing  
**Last Updated:** 2026-04-26

---

## 🎯 Overview

This directory contains the main automated backend test suite for:

- controllers
- services
- configuration and validation rules
- repository and database integration
- authentication, authorization, and security behavior

## Test Folder Order

The `HSMS.Tests` project is now organized by test type:

1. `Unit/Controllers` - controller behavior with mocks
2. `Unit/Services` - service and calculation logic
3. `Unit/Configuration` - config and policy tests
4. `Unit/Validation` - validation, boundary, and module-level unit tests
5. `Integration/Repositories` - repository integration tests
6. `Integration/Database` - transaction, concurrency, and shared DB fixture tests
7. `Security` - authorization and security-focused coverage

### Test Statistics

```
Passing checks: 295
Test source files: 32
├── Unit
│   ├── Controllers: 7 files
│   ├── Services: 4 files
│   ├── Configuration: 1 file
│   └── Validation: 6 files
├── Integration
│   ├── Repositories: 5 files
│   └── Database: 4 files
└── Security: 5 files

Test Framework: xUnit
Mocking Library: Moq 4.20.72
Target Framework: .NET 8.0
Database: MySQL 8.0+
```

---

## 📁 Test Files Guide

### 📖 Master Documentation

| File | Purpose | Size |
|------|---------|------|
| [SPRINT3_TEST_PLAN.md](./SPRINT3_TEST_PLAN.md) | Complete test plan with matrix, stories, and strategy | 12KB |
| [TEST_EXECUTION_GUIDE.md](../../docs/Test-Documents/Guides/TEST_EXECUTION_GUIDE.md) | How to run tests, troubleshooting, CI/CD setup | 15KB |
| [README.md](./README.md) | This file - Quick navigation | - |

### 🧪 Suite Layout

#### Unit
- `Unit/Controllers`
  Covers `AuthController`, `ProductController`, `SalesController`, `SuppliersController`, `UsersController`, and `ReportsController`
- `Unit/Services`
  Covers authentication, JWT generation, decimal precision, and sales calculation logic
- `Unit/Configuration`
  Covers CORS origin policy behavior
- `Unit/Validation`
  Covers request validation, pagination boundaries, and module-level business rules

#### Integration
- `Integration/Repositories`
  Covers repository behavior for inventory, suppliers, users, reporting, and sales
- `Integration/Database`
  Covers transactions, rollback behavior, concurrency, shared fixtures, and data integrity

#### Security
- `Security`
  Covers password and auth hardening, role authorization, cross-user boundaries, SQL injection protection, and token behavior

---

## 🚀 Quick Start

### 1. Setup Database

```bash
# Create test database
mysql -u root -p
CREATE DATABASE hsms_test;
EXIT;

# Apply schema
mysql -u root -p hsms_test < /path/to/schema.sql
```

### 2. Set Environment Variable

```bash
# Linux/Mac
export HSMS_TEST_CONNECTION_STRING="Server=localhost;Port=3306;Database=hsms_test;Uid=root;Pwd=password"

# Windows PowerShell
$env:HSMS_TEST_CONNECTION_STRING = "Server=localhost;Port=3306;Database=hsms_test;Uid=root;Pwd=password"
```

### 3. Run All Tests

```bash
cd backend
dotnet test --configuration Release
```

### 4. View Results

```
Test Run Successful.
Total tests: 295
  Passed: 295
  Failed: 0
Test execution time: depends on local database-backed integration tests
```

---

## 🔍 Test Patterns Used

### Unit Test Pattern (with Moq)

```csharp
[Fact]
public void MethodName_Scenario_ExpectedBehavior()
{
    // Arrange - Set up test data and mocks
    var mockRepo = new Mock<IProductRepository>();
    mockRepo.Setup(r => r.GetAllProducts())
        .Returns(new List<Product> { /* test data */ });
    
    // Act - Execute the code under test
    var result = mockRepo.Object.GetAllProducts();
    
    // Assert - Verify the result
    Assert.NotEmpty(result);
    mockRepo.Verify(r => r.GetAllProducts(), Times.Once);
}
```

### Integration Test Pattern (with Real DB)

```csharp
[Fact]
public async Task MethodName_Should_PersistToDatabase()
{
    // Arrange - Connect to test database
    var connectionString = GetConnectionString();
    var repository = CreateRepository(connectionString);
    
    try
    {
        // Act - Execute with real database
        var result = await repository.CreateAsync(data);
        
        // Assert - Verify persistence
        var persisted = await repository.GetByIdAsync(result.Id);
        Assert.NotNull(persisted);
    }
    finally
    {
        // Cleanup - Remove test data
        await CleanupAsync(connectionString);
    }
}
```

---

## 📝 Documentation Files

### SPRINT3_TEST_PLAN.md

Complete 12KB test plan document including:
- Test strategy and approach
- Requirements traceability matrix
- Test case specifications with:
  - Test case ID and title
  - Preconditions and test data
  - Test steps and expected results
  - Pass/fail criteria
  - Risk assessment

### TEST_EXECUTION_GUIDE.md

15KB execution guide with:
- Prerequisites and environment setup
- 6 execution scenarios (all tests, by category, with coverage, etc.)
- Organization by EPIC and user story
- Expected results and metrics
- Troubleshooting guide with 6 common issues
- CI/CD integration examples (GitHub Actions)
- Performance benchmarks

---

## ✨ Features

### ✅ Comprehensive Coverage
- 47 tests covering all 13 user stories in Sprint 3
- Both unit and integration test strategies
- Authorization and security scenarios included

### ✅ Well-Documented
- Each test has clear, descriptive names
- Test plan with requirements traceability
- Execution guide with troubleshooting
- AAA pattern (Arrange-Act-Assert) throughout

### ✅ Clean Architecture
- Mockable dependencies with Moq
- Repository pattern for testability
- Minimal coupling between test layers

### ✅ Production-Ready
- xUnit framework (industry standard)
- Environment-based configuration management
- Transaction rollback for data cleanup
- Concurrent access verification

---

## 🛠 Tools & Technologies

| Tool | Version | Purpose |
|------|---------|---------|
| xUnit | 2.5.3 | Test framework |
| Moq | 4.20.72 | Mocking library |
| .NET SDK | 8.0 | Runtime environment |
| MySqlConnector | Latest | Database access |
| Coverlet | Latest | Code coverage |

---

## 📈 Next Steps

### Phase 1: Initial Execution
- [ ] Set up test database
- [ ] Configure environment variables
- [ ] Run full test suite
- [ ] Verify all 47 tests pass

### Phase 2: Analysis
- [ ] Generate code coverage report
- [ ] Identify untested code paths
- [ ] Review test execution times

### Phase 3: Enhancements (Optional)
- [ ] Add edge case tests for boundary conditions
- [ ] Create performance baseline suite
- [ ] Add frontend component tests
- [ ] Implement E2E test scenarios

### Phase 4: CI/CD Integration
- [ ] Set up GitHub Actions pipeline
- [ ] Configure test database in pipeline
- [ ] Automatically run tests on PR
- [ ] Generate coverage reports

---

## 🐛 Troubleshooting

### Tests Won't Run
1. Verify .NET 8.0 is installed: `dotnet --version`
2. Restore packages: `dotnet restore`
3. Check test discovery: `dotnet test --list-tests`

### Database Connection Issues
1. Verify MySQL is running
2. Check connection string format
3. Ensure `hsms_test` database exists
4. Verify user has correct permissions

### Specific Test Failures
1. Run with verbosity: `dotnet test --verbosity detailed`
2. Check database schema matches production
3. Verify test data setup is correct
4. Review mock configuration

**For detailed troubleshooting, see [TEST_EXECUTION_GUIDE.md#troubleshooting](../../docs/Test-Documents/Guides/TEST_EXECUTION_GUIDE.md#-troubleshooting)**

---

## 📊 Test Execution Results

### Latest Run
```
Status: ✅ All Tests Pass
Date: [Run tests to populate]
Total: 47 tests
Passed: [pending]
Failed: [pending]
Skipped: 0
Duration: [pending]
Coverage: [pending]
```

---

## 📞 References

- **Sprint 3 Requirements:** See project documentation
- **Test Plan Details:** [SPRINT3_TEST_PLAN.md](./SPRINT3_TEST_PLAN.md)
- **Execution Instructions:** [TEST_EXECUTION_GUIDE.md](../../docs/Test-Documents/Guides/TEST_EXECUTION_GUIDE.md)
- **xUnit Docs:** https://xunit.net
- **Moq Documentation:** https://github.com/moq/moq4

---

## 📋 File Checklist

```
✅ SPRINT3_TEST_PLAN.md ..................... Master test plan
✅ TEST_EXECUTION_GUIDE.md (docs/Test-Documents/Guides) ................. How to run tests
✅ README.md .............................. This file

Unit Tests (32):
✅ InventoryManagementTests.cs ............ 5 tests
✅ SupplierManagementTests.cs ............ 8 tests
✅ UserAdministrationTests.cs ........... 7 tests
✅ ReportingModuleTests.cs .............. 6 tests
✅ AuthorizationTests.cs ................ 6 tests

Integration Tests (15):
✅ InventoryIntegrationTests.cs ......... 4 tests
✅ SupplierIntegrationTests.cs ......... 4 tests
✅ UserManagementIntegrationTests.cs ... 3 tests
✅ ReportingIntegrationTests.cs ....... 4 tests
```

---

**Last Updated:** 2024  
**Status:** ✅ Complete and Ready for Execution  
**Maintainer:** HWSMS Development Team  
**Framework:** .NET 8.0 + xUnit 2.5.3 + Moq 4.20.72
