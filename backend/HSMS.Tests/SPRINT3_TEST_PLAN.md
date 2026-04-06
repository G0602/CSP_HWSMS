# Sprint 3 Test Plan & Documentation

**Project:** HWSMS (Hardware Store Inventory Management System)  
**Sprint:** Sprint 3 (EPIC 3.1-3.4)  
**Created:** April 5, 2026  
**Test Framework:** xUnit with Moq for mocking

---

## 📋 Executive Summary

This document outlines the comprehensive test strategy for Sprint 3 components covering:
- **EPIC 3.1:** Inventory Management (3 user stories)
- **EPIC 3.2:** Supplier Management (3 user stories)
- **EPIC 3.3:** User & Role Administration (3 user stories)
- **EPIC 3.4:** Reporting Module (4 user stories)

**Test Coverage Goals:**
- ✅ Unit Tests: All business logic and API controllers
- ✅ Integration Tests: Database operations and transactions
- ✅ Edge Cases: Error scenarios, boundary conditions, data validation
- ✅ Authorization: Role-based access control enforcement

---

## 🏗️ Test Architecture

### Test Organization

```
HSMS.Tests/
├── Unit Tests (Mocked Dependencies)
│   ├── InventoryManagementTests.cs          [NEW]
│   ├── SupplierManagementTests.cs           [NEW]
│   ├── UserAdministrationTests.cs           [NEW]
│   ├── ReportingModuleTests.cs              [NEW]
│   └── AuthorizationTests.cs                [NEW]
├── Integration Tests (Real Database)
│   ├── InventoryIntegrationTests.cs         [NEW]
│   ├── SupplierIntegrationTests.cs          [NEW]
│   ├── UserManagementIntegrationTests.cs    [NEW]
│   ├── ReportingIntegrationTests.cs         [NEW]
│   └── SaleRepositoryIntegrationTests.cs    [EXISTING]
└── Existing Tests
    ├── ProductControllerTests.cs
    ├── SuppliersControllerTests.cs
    ├── UsersControllerTests.cs
    ├── ReportsControllerTests.cs
    ├── AuthSecurityTests.cs
    └── SaleCalculatorTests.cs
```

### Test Dependencies

- **xUnit 2.5.3**: Test framework
- **Moq 4.20.72**: Mocking framework
- **Microsoft.NET.Test.SDK 17.8.0**: Test runtime
- **MySQL.Data**: For integration tests (live database)

---

## 🧪 EPIC 3.1: Inventory Management Tests

### Story S3-US-01: View Inventory with Stock Status

#### Unit Tests
```
✓ GetInventoryProducts_Should_Return_All_Products_With_Stock_Status
✓ GetInventoryProducts_Should_Include_Quantity_Category_Price
✓ GetInventoryProducts_Should_Mark_Low_Stock_Items_Correctly
✓ GetInventoryProducts_Should_Include_Supplier_Information
✓ GetInventoryProducts_Should_Return_Empty_When_No_Products
```

#### Integration Tests
```
✓ GetInventoryProducts_Should_Fetch_All_Products_From_Database
✓ GetInventoryProducts_Should_Persist_Supplier_Relationships
```

### Story S3-US-02: Low Stock Alerts

#### Unit Tests
```
✓ GetLowStockProducts_Should_Filter_Products_Below_Threshold
✓ GetLowStockProducts_Should_Use_Configured_Threshold_Value
✓ GetLowStockProducts_Should_Respect_Threshold_Configuration
✓ GetLowStockProducts_Should_Be_Empty_When_No_Low_Stock_Items
✓ GetLowStockProducts_Should_Mark_All_Returned_Items_As_LowStock
```

#### Integration Tests
```
✓ GetLowStockProducts_Should_Query_Threshold_From_Configuration
✓ GetLowStockProducts_Should_Return_Accurate_Count
```

### Story S3-US-03: Manual Stock Update

#### Unit Tests
```
✓ UpdateProductStock_Should_Accept_Valid_Quantities
✓ UpdateProductStock_Should_Return_Ok_When_Valid
✓ UpdateProductStock_Should_Return_BadRequest_When_Quantity_Is_Negative
✓ UpdateProductStock_Should_Return_NotFound_When_Product_Not_Exists
✓ UpdateProductStock_Should_Persist_Changes_Immediately
```

#### Integration Tests
```
✓ UpdateProductStock_Should_Update_Database_Transaction_Safe
✓ UpdateProductStock_Should_Not_Allow_Negative_Quantities
✓ UpdateProductStock_Should_Handle_Concurrent_Updates
```

---

## 🏪 EPIC 3.2: Supplier Management Tests

### Story S3-US-04: Add Supplier

#### Unit Tests
```
✓ AddSupplier_Should_Create_With_Name_And_Optional_Contact_Info
✓ AddSupplier_Should_Return_Created_When_Valid
✓ AddSupplier_Should_Return_BadRequest_When_Name_Is_Empty
✓ AddSupplier_Should_Trim_Whitespace_From_Name_And_Contact
✓ AddSupplier_Should_Return_Created_With_Supplier_ID
```

#### Integration Tests
```
✓ AddSupplier_Should_Persist_To_Database
✓ AddSupplier_Should_Generate_CreatedAt_Timestamp
```

### Story S3-US-05: Update/Delete Supplier

#### Unit Tests
```
✓ UpdateSupplier_Should_Modify_Name_And_Contact_Info
✓ UpdateSupplier_Should_Return_NotFound_When_Not_Exists
✓ UpdateSupplier_Should_Return_BadRequest_On_Empty_Name
✓ DeleteSupplier_Should_Remove_When_Not_Linked
✓ DeleteSupplier_Should_Return_Conflict_When_Linked_To_Products
✓ DeleteSupplier_Should_Return_NotFound_When_Not_Exists
✓ DeleteSupplier_Should_Prevent_Delete_If_Linked_BONUS
```

#### Integration Tests
```
✓ UpdateSupplier_Should_Persist_Changes_To_Database
✓ DeleteSupplier_Should_Check_Product_References_Before_Delete
✓ DeleteSupplier_Should_Support_Cascade_Protection
```

### Story S3-US-06: Link Supplier to Product

#### Unit Tests
```
✓ AddProduct_Should_Accept_Valid_SupplierId
✓ AddProduct_Should_Reject_Invalid_SupplierId
✓ AddProduct_Should_Set_SupplierId_To_Null_When_Not_Provided
✓ UpdateProduct_Should_Change_Supplier_Link
✓ UpdateProduct_Should_Allow_Removing_Supplier
✓ UpdateProduct_Should_Validate_Supplier_Exists
```

#### Integration Tests
```
✓ AddProduct_Should_Create_FK_Relationship_To_Supplier
✓ UpdateProduct_Should_Modify_Supplier_FK
✓ Product_Should_Maintain_Referential_Integrity
```

---

## 👥 EPIC 3.3: User & Role Administration Tests

### Story S3-US-07: Admin Creates Users

#### Unit Tests
```
✓ CreateUser_Should_Return_Created_When_Valid
✓ CreateUser_Should_Return_BadRequest_When_Username_Empty
✓ CreateUser_Should_Return_BadRequest_When_Password_Too_Short
✓ CreateUser_Should_Return_BadRequest_When_Invalid_Role
✓ CreateUser_Should_Return_Conflict_When_Username_Exists
✓ CreateUser_Should_Hash_Password_Before_Storage
```

#### Integration Tests
```
✓ CreateUser_Should_Persist_To_Database
✓ CreateUser_Should_Enforce_Username_Uniqueness
✓ CreateUser_Should_Store_Hashed_Password
```

### Story S3-US-08: Role Assignment

#### Unit Tests
```
✓ UpdateUserRole_Should_Change_Role_When_Valid
✓ UpdateUserRole_Should_Return_NotFound_When_User_Not_Exists
✓ UpdateUserRole_Should_Return_BadRequest_When_Invalid_Role
✓ UpdateUserRole_Should_Refresh_Token_On_Self_Update
✓ UpdateUserRole_Should_Update_Immediately
```

#### Integration Tests
```
✓ UpdateUserRole_Should_Persist_Change_To_Database
✓ UpdateUserRole_Should_Reflect_Changes_Immediately
```

### Story S3-US-09: User Management Dashboard

#### Unit Tests
```
✓ GetUsers_Should_Return_All_Users
✓ GetUsers_Should_Include_Id_Username_Role_CreatedAt
✓ GetUsers_Should_Return_Empty_List_When_No_Users
✓ DeleteUser_Should_Remove_User_When_Valid
✓ DeleteUser_Should_Return_NotFound_When_Not_Exists
```

#### Integration Tests
```
✓ GetUsers_Should_Return_All_Database_Records
✓ DeleteUser_Should_Remove_From_Database
```

---

## 📊 EPIC 3.4: Reporting Module Tests

### Story S3-US-10: Daily Sales Report

#### Unit Tests
```
✓ GetDailySalesReport_Should_Return_Daily_Totals
✓ GetDailySalesReport_Should_Aggregate_By_Date
✓ GetDailySalesReport_Should_Include_TotalAmount
✓ GetDailySalesReport_Should_Return_Empty_When_No_Sales
```

#### Integration Tests
```
✓ GetDailySalesReport_Should_Query_Database_Accurately
✓ GetDailySalesReport_Should_Calculate_Correct_Totals
```

### Story S3-US-11: Monthly Sales Report

#### Unit Tests
```
✓ GetMonthlySalesReport_Should_Return_Monthly_Totals
✓ GetMonthlySalesReport_Should_Aggregate_By_Month
✓ GetMonthlySalesReport_Should_Include_TotalAmount
✓ GetMonthlySalesReport_Should_Handle_Multiple_Years
```

#### Integration Tests
```
✓ GetMonthlySalesReport_Should_Query_Database_Accurately
✓ GetMonthlySalesReport_Should_Group_By_Month_Correctly
```

### Story S3-US-12: Low Stock Report

#### Unit Tests
```
✓ GetLowStockReport_Should_Filter_By_Threshold
✓ GetLowStockReport_Should_Include_Product_Details
✓ GetLowStockReport_Should_Return_Empty_When_No_Low_Stock
```

#### Integration Tests
```
✓ GetLowStockReport_Should_Use_Configured_Threshold
✓ GetLowStockReport_Should_Return_Accurate_Products
```

### Story S3-US-13: Export Report (CSV/PDF)

#### Unit Tests
```
✓ ExportReport_Should_Generate_CSV_For_Daily
✓ ExportReport_Should_Generate_CSV_For_Monthly
✓ ExportReport_Should_Generate_CSV_For_Low_Stock
✓ ExportReport_Should_Escape_Special_Characters
✓ ExportReport_Should_Return_BadRequest_For_Invalid_Type
```

#### Integration Tests
```
✓ ExportReport_Should_Include_All_Data_In_CSV
✓ ExportReport_Should_Generate_Valid_CSV_Format
```

---

## 🔐 Authorization & Security Tests

### Policy-Based Access Control
```
✓ InventoryManagerRead_Should_Allow_Manager_Access
✓ InventoryManagerRead_Should_Allow_Admin_Access
✓ InventoryManagerRead_Should_Deny_Cashier_Access
✓ UsersManage_Should_Allow_Admin_Only
✓ UsersManage_Should_Deny_Manager_And_Cashier
✓ SalesRead_Should_Allow_Manager_And_Admin
✓ SalesRead_Should_Deny_Cashier
```

---

## 🧪 Test Execution & Running

### Run All Tests
```bash
cd backend/HSMS.Tests
dotnet test
```

### Run Specific Test Class
```bash
dotnet test --filter "ClassName=InventoryManagementTests"
```

### Run Unit Tests Only (No Integration)
```bash
dotnet test --filter "Category!=Integration"
```

### Run with Coverage Report
```bash
dotnet test /p:CollectCoverageMetrics=true
```

### Set Integration Test Database
```bash
export HSMS_TEST_CONNECTION_STRING="Server=localhost;Database=CSP_HSMS_Tests;User=root;Password=password"
dotnet test
```

---

## 📊 Test Coverage Goals

| Component | Unit Coverage | Integration Coverage | Total |
|-----------|---------------|----------------------|-------|
| Inventory | 95% | 90% | 92.5% |
| Suppliers | 95% | 85% | 90% |
| Users | 95% | 85% | 90% |
| Reporting | 90% | 85% | 87.5% |
| **Overall** | **94%** | **86%** | **90%** |

---

## 🛠️ Test Data Fixtures

### Product Fixtures
```csharp
Product LowStockProduct = new() 
{ 
    Id = 1, Name = "Low Stock Item", Quantity = 5, 
    Category = "Tools", Price = 1000, SKU = "LS-001" 
}

Product NormalStockProduct = new() 
{ 
    Id = 2, Name = "Normal Item", Quantity = 20, 
    Category = "Tools", Price = 2000, SKU = "NS-001" 
}
```

### User Fixtures
```csharp
User AdminUser = new() 
{ 
    Id = 1, Username = "admin", Role = "Admin", 
    PasswordHash = HashPassword("Admin@123") 
}

User ManagerUser = new() 
{ 
    Id = 2, Username = "manager", Role = "Manager",
    PasswordHash = HashPassword("Manager@123") 
}
```

### Supplier Fixtures
```csharp
Supplier SupplierA = new() 
{ 
    Id = 1, Name = "ABC Supplies", 
    ContactInfo = "+94-11-123456" 
}
```

---

## ⚠️ Edge Cases & Boundary Tests

### Inventory Tests
- [ ] Stock quantity = 0
- [ ] Stock quantity = Threshold
- [ ] Stock quantity = Threshold - 1
- [ ] Very large quantities (MAX_INT)
- [ ] Negative quantities (should fail)
- [ ] Decimal prices with multiple decimal places

### Supplier Tests
- [ ] Whitespace-only names
- [ ] Names with special characters
- [ ] Very long supplier names
- [ ] Contact info with special formats

### User Tests
- [ ] Password exactly 8 characters (minimum)
- [ ] Password with 7 characters (should fail)
- [ ] Username with special characters
- [ ] Duplicate username creation
- [ ] Role normalization (case insensitive)

### Report Tests
- [ ] Date range spanning multiple months
- [ ] All reports with zero data
- [ ] CSV with quotes and commas
- [ ] Very large datasets (1000+ records)

---

## 📝 Test Naming Convention

All tests follow the pattern:

```
[MethodName]_Should_[ExpectedBehavior]_[Condition]
```

Examples:
- `GetInventoryProducts_Should_Return_All_Products_With_Stock_Status`
- `UpdateProductStock_Should_Return_BadRequest_When_Quantity_Is_Negative`
- `DeleteSupplier_Should_Return_Conflict_When_Linked_To_Products`

---

## 🔄 Test Lifecycle

Each test follows the **AAA Pattern**:

```csharp
// ARRANGE: Set up test data and mocks
var mockRepo = new Mock<IProductRepository>();
mockRepo.Setup(...).Returns(...);

// ACT: Execute the operation
var result = await controller.GetInventoryProducts();

// ASSERT: Verify the outcome
var okResult = Assert.IsType<OkObjectResult>(result);
Assert.Equal(200, okResult.StatusCode);
```

---

## ✅ Quality Checklist

- [ ] All unit tests pass locally
- [ ] All integration tests pass with test database
- [ ] Code coverage > 85%
- [ ] No flaky tests (tests pass consistently)
- [ ] Test names clearly describe behavior
- [ ] Edge cases covered
- [ ] Authorization tests included
- [ ] Database transactions verified
- [ ] Error handling tested
- [ ] Documentation complete

---

## 📚 Related Documentation

- Code: Backend Controllers & Repositories
- Architecture: EPIC_3_REQUIREMENTS_ANALYSIS.md
- Database: DB Schema & Relationships
- API: /api/products, /api/suppliers, /api/users, /api/reports

---

**Document Version:** 1.0  
**Last Updated:** April 5, 2026  
**Status:** Ready for Implementation
