# Sprint 3 Test Execution Guide

**Status:** Complete Test Suite Created  
**Date:** 2024  
**Project:** HWSMS (Hardware Store Management System)  
**Coverage:** All 4 EPICs (Inventory, Suppliers, Users, Reporting)

---

## 📋 Test Suite Overview

### Test Files Created (9 Total)

| Test File | Type | Test Count | Coverage |
|-----------|------|-----------|----------|
| `InventoryManagementTests.cs` | Unit | 5 | Inventory viewing, low-stock logic, stock updates |
| `SupplierManagementTests.cs` | Unit | 8 | CRUD operations, delete protection |
| `UserAdministrationTests.cs` | Unit | 7 | User creation, role management, deletion |
| `ReportingModuleTests.cs` | Unit | 6 | Report generation, calculations, CSV export |
| `AuthorizationTests.cs` | Unit | 6 | Policy enforcement, role validation |
| `InventoryIntegrationTests.cs` | Integration | 4 | Database stock updates, transactions |
| `SupplierIntegrationTests.cs` | Integration | 4 | Supplier-product relationships |
| `UserManagementIntegrationTests.cs` | Integration | 3 | User persistence, role updates |
| `ReportingIntegrationTests.cs` | Integration | 4 | Report queries, calculations |
| **TOTAL** | **Unit: 32 / Integration: 15** | **47** | **Full Sprint 3 Coverage** |

---

## 🚀 Quick Start

### Prerequisites

```bash
# 1. Ensure .NET 8.0 SDK is installed
dotnet --version

# 2. Navigate to backend directory
cd backend

# 3. Restore dependencies
dotnet restore
```

### Environment Setup

```bash
# Set test database connection string
export HSMS_TEST_CONNECTION_STRING="Server=localhost;Port=3306;Database=hsms_test;Uid=root;Pwd=password"

# OR (Windows PowerShell)
$env:HSMS_TEST_CONNECTION_STRING = "Server=localhost;Port=3306;Database=hsms_test;Uid=root;Pwd=password"

# OR (Windows CMD)
set HSMS_TEST_CONNECTION_STRING=Server=localhost;Port=3306;Database=hsms_test;Uid=root;Pwd=password
```

**Note:** Create `hsms_test` database before running tests:
```sql
CREATE DATABASE hsms_test;
USE hsms_test;
-- Run your schema migration scripts
```

---

## ✅ Test Execution Scenarios

### Scenario 1: Run All Tests

```bash
cd backend
dotnet test --configuration Release --verbosity normal
```

**Expected Output:**
```
Test Run Successful.
Total tests: 47
     Passed: 47
     Failed: 0
Test execution time: ~45-60 seconds
```

### Scenario 2: Run Tests by Category

#### A. Unit Tests Only
```bash
dotnet test --filter "Category=Unit" --configuration Release
```

#### B. Integration Tests Only
```bash
dotnet test --filter "Category=Integration" --configuration Release
```

#### C. Run Specific EPIC Tests

```bash
# EPIC 3.1: Inventory Management
dotnet test --filter "EPIC3.1" --configuration Release

# EPIC 3.2: Supplier Management  
dotnet test --filter "EPIC3.2" --configuration Release

# EPIC 3.3: User Administration
dotnet test --filter "EPIC3.3" --configuration Release

# EPIC 3.4: Reporting
dotnet test --filter "EPIC3.4" --configuration Release
```

### Scenario 3: Run Tests with Code Coverage

#### Install Coverage Tool
```bash
dotnet tool install --global coverlet.console
```

#### Generate Coverage Report
```bash
cd backend
dotnet test --configuration Release /p:CollectCoverage=true /p:CoverageFormat=opencover /p:CoverageFileName=coverage.xml
```

#### Generate HTML Coverage Report
```bash
dotnet tool install --global ReportGenerator
reportgenerator -reports:coverage.xml -targetdir:coverage-report -reporttypes:Html
```

#### View Report
```bash
# Open in browser (adjust path as needed)
open coverage-report/index.html
# OR
start coverage-report/index.html
```

### Scenario 4: Run Tests with Detailed Logging

```bash
dotnet test --configuration Release --verbosity detailed --logger "console;verbosity=detailed"
```

### Scenario 5: Run Single Test File

```bash
# Run only inventory tests
dotnet test ./HSMS.Tests/InventoryManagementTests.cs

# Run only integration tests
dotnet test ./HSMS.Tests/InventoryIntegrationTests.cs
```

### Scenario 6: Run Single Test Method

```bash
dotnet test --filter "Name~GetInventoryProducts_Should_Return_AllProducts"
```

---

## 🧪 Test Organization by EPIC

### EPIC 3.1: Inventory Management (US-01 to US-03)

**Unit Tests (5):**
- ✅ `GetInventoryProducts_Should_Return_AllProducts`
- ✅ `GetInventoryProducts_Should_Include_LowStockFlag`
- ✅ `GetInventoryProducts_Should_Filter_ByMinimumQuantity`
- ✅ `UpdateProductStock_Should_Prevent_NegativeQuantity`
- ✅ `UpdateProductStock_Should_Execute_AtomicTransaction`

**Integration Tests (4):**
- ✅ `UpdateProductStock_Should_Persist_ToDatabase`
- ✅ `UpdateProductStock_Should_Maintain_Consistency`
- ✅ `LowStockQuery_Should_Use_Configured_Threshold`
- ✅ `ConcurrentStockUpdates_Should_Maintain_Atomicity`

---

### EPIC 3.2: Supplier Management (US-04 to US-06)

**Unit Tests (8):**
- ✅ `AddSupplier_Should_CreateSupplierRecord`
- ✅ `AddSupplier_Should_ValidateContactInfo`
- ✅ `UpdateSupplier_Should_ModifySupplierDetails`
- ✅ `DeleteSupplier_Should_NotAllowIfProductsLinked`
- ✅ `DeleteSupplier_Should_AllowIfNoProductsLinked`
- ✅ `LinkSupplierToProduct_Should_UpdateSupplierReference`
- ✅ `GetSuppliers_Should_ReturnAllSuppliers`
- ✅ `SearchSuppliers_Should_FilterByName`

**Integration Tests (4):**
- ✅ `AddSupplier_Should_Persist_ToDatabase`
- ✅ `DeleteSupplier_Should_Protect_LinkedRecords`
- ✅ `LinkSupplierToProduct_Should_MaintainForeignKeyIntegrity`
- ✅ `SupplierProductRelationship_Should_Enforce_Constraints`

---

### EPIC 3.3: User & Role Administration (US-07 to US-09)

**Unit Tests (7):**
- ✅ `CreateUser_Should_HashPassword`
- ✅ `CreateUser_Should_AssignDefaultRole`
- ✅ `UpdateUserRole_Should_ModifyUserRole`
- ✅ `DeleteUser_Should_RemoveAllUserData`
- ✅ `GetUsers_Should_ReturnAllUsers`
- ✅ `GenerateJwtToken_Should_IncludeUserClaims`
- ✅ `RefreshJwtToken_Should_IssueNewValidToken`

**Integration Tests (3):**
- ✅ `CreateUser_Should_Persist_ToDatabase`
- ✅ `UpdateUserRole_Should_Propagate_ToDatabase`
- ✅ `UserDeletion_Should_CascadeFollowingConstraints`

---

### EPIC 3.4: Reporting Module (US-10 to US-13)

**Unit Tests (6):**
- ✅ `GetDailySalesReport_Should_AggregateByDate`
- ✅ `GetDailySalesReport_Should_CalculateTotalAmount`
- ✅ `GetMonthlySalesReport_Should_GroupByMonth`
- ✅ `GetMonthlySalesReport_Should_CalculateMonthlyTotals`
- ✅ `GetLowStockReport_Should_FilterByThreshold`
- ✅ `ExportReportToCSV_Should_FormatDataCorrectly`

**Integration Tests (4):**
- ✅ `GetDailySalesReportAsync_Should_Query_DatabaseAccurately`
- ✅ `GetDailySalesReportAsync_Should_Calculate_CorrectTotals`
- ✅ `GetMonthlySalesReportAsync_Should_Query_DatabaseAccurately`
- ✅ `GetMonthlySalesReportAsync_Should_Group_ByMonthCorrectly`

---

### Authorization & Security (Cross-cutting)

**Unit Tests (6):**
- ✅ `InventoryRead_Should_AllowAuthorizedUsers`
- ✅ `InventoryWrite_Should_DenyUnauthorized`
- ✅ `InventoryManagerRead_Should_RequireManagerRole`
- ✅ `SalesCreate_Should_RequireSpecificRole`
- ✅ `UsersManage_Should_RequireAdminRole`
- ✅ `AuthorizationPolicy_Should_EvaluateCorrectly`

---

## 📊 Expected Test Results

### Coverage Metrics (Target)

| Metric | Target | Current |
|--------|--------|---------|
| Line Coverage | >80% | To be determined |
| Branch Coverage | >70% | To be determined |
| Method Coverage | >85% | To be determined |

### Test Distribution

- **Unit Tests:** 32 tests (68%)
  - Fast execution (<30 seconds)
  - Mock dependencies
  - Zero database required

- **Integration Tests:** 15 tests (32%)
  - Real database interaction
  - Transaction validation
  - Constraint enforcement
  - ~15-30 seconds execution time

### Estimated Run Times

| Test Category | Time |
|---------------|------|
| All Unit Tests | ~30 seconds |
| All Integration Tests | ~30 seconds |
| Full Suite | ~45-60 seconds |

---

## 🔧 Troubleshooting

### Issue: Tests Fail with "Connection String Not Found"

**Solution:**
```bash
# Verify environment variable is set
echo $HSMS_TEST_CONNECTION_STRING  # Linux/Mac
echo %HSMS_TEST_CONNECTION_STRING% # Windows

# If empty, set it again:
export HSMS_TEST_CONNECTION_STRING="your_connection_string"
```

### Issue: MySQL Connection Timeout

**Solution:**
```bash
# 1. Verify MySQL is running
systemctl status mysql  # Linux
services.msc           # Windows

# 2. Test connection manually
mysql -u root -p -h localhost

# 3. Update connection string if needed
# Default: Server=localhost;Port=3306;Database=hsms_test;Uid=root;Pwd=password
```

### Issue: Tests Pass Locally but Fail in CI/CD

**Solution:**
```bash
# Set pipeline environment variables in CI/CD configuration
# GitHub Actions example:
- name: Run Tests
  env:
    HSMS_TEST_CONNECTION_STRING: ${{ secrets.TEST_DB_CONNECTION }}
  run: dotnet test
```

### Issue: Integration Tests Fail with "Database Schema Mismatch"

**Solution:**
```bash
# Ensure test database has all tables/procedures before running
# Run schema migration:
mysql hsms_test < migration_script.sql

# OR recreate from production schema
mysqldump --no-data -u root -p hsms > schema.sql
mysql -u root -p hsms_test < schema.sql
```

### Issue: Xunit Tests Not Discovered

**Solution:**
```bash
# 1. Verify HSMS.Tests.csproj references correct xUnit packages
cat HSMS.Tests/HSMS.Tests.csproj | grep -i "xunit"

# 2. Rebuild solution
dotnet clean && dotnet build

# 3. List available tests
dotnet test --list-tests
```

---

## 📝 CI/CD Integration

### GitHub Actions Example

```yaml
name: Run Sprint 3 Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    
    services:
      mysql:
        image: mysql:8.0
        env:
          MYSQL_ROOT_PASSWORD: password
          MYSQL_DATABASE: hsms_test
        options: >-
          --health-cmd="mysqladmin ping"
          --health-interval=10s
          --health-timeout=5s
          --health-retries=5
        ports:
          - 3306:3306
    
    steps:
    - uses: actions/checkout@v2
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v1
      with:
        dotnet-version: '8.0.x'
    
    - name: Restore
      run: dotnet restore backend/
    
    - name: Run Tests
      env:
        HSMS_TEST_CONNECTION_STRING: "Server=localhost;Uid=root;Pwd=password;Database=hsms_test"
      run: dotnet test backend/ --configuration Release --no-restore
```

---

## ✨ Next Steps

### After Running All Tests Successfully

1. **Review Coverage Report**
   - Identify any untested code paths
   - Target >80% line coverage

2. **Add Edge Case Tests** (Optional)
   - Create `EdgeCasesTests.cs` for boundary conditions
   - Test null references, empty collections, zero values

3. **Performance Baseline** (Optional)
   - Create `PerformanceTests.cs`
   - Establish acceptable execution times for reports
   - Monitor report generation <500ms

4. **Frontend Integration Tests** (Optional)
   - Create React component tests for `InventoryPage.tsx`, `SupplierPage.tsx`, `UsersPage.tsx`
   - Test API client integration

5. **E2E Tests** (Optional)
   - Use Playwright or Cypress
   - Test complete user workflows across frontend + backend

---

## 📞 Support

### Questions or Issues?

1. Check test output for specific assertions that failed
2. Review test method documentation in test files
3. Verify database state and schema
4. Check environment variables and connection strings
5. Run with `--verbosity detailed` for more information

### Test Result Interpretation

| Symbol | Meaning |
|--------|---------|
| ✅ Passed | Test executed and all assertions passed |
| ❌ Failed | One or more assertions failed |
| ⊘ Skipped | Test was intentionally skipped |
| ⚠ Warning | Test passed but generated warnings |

---

## 📚 References

- **All Test Plan:** See `SPRINT3_TEST_PLAN.md`
- **xUnit Documentation:** https://xunit.net/docs/getting-started/netcore
- **Moq Documentation:** https://github.com/moq/moq4/wiki/Quickstart
- **MySQL MySqlConnector:** https://dev.mysql.com/doc/connector-net/en/connector-net-connections-string.html

---

**Last Updated:** 2024  
**Test Framework:** xUnit 2.5.3 + Moq 4.20.72  
**Target Framework:** .NET 8.0  
**Database:** MySQL 8.0+
