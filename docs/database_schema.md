# Database Schema

## Database Engine

- **Engine:** MySQL 8.0
- **Database name:** `CSP_HSMS` (development) / `hsms_test` (CI)
- **Character set:** Default MySQL 8 (utf8mb4)
- **Connection:** SSL required in production (`SslMode=Required`)

## Schema Bootstrap

The schema is managed by `DatabaseInitializer.InitializeAsync()` which runs on every backend startup.
It uses `CREATE TABLE IF NOT EXISTS`, `ALTER TABLE ... ADD COLUMN`, and `CREATE INDEX` statements
that are idempotent — safe to run on an existing populated database.

---

## Entity Relationship Diagram

> See: [Diagrams/ER_Diagram_HWSMS_CSP.png](./Diagrams/ER_Diagram_HWSMS_CSP.png)

---

## Tables

### `Users`

Stores system user accounts with hashed passwords and roles.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `Id` | INT | PK, AUTO_INCREMENT | Unique user ID |
| `Username` | VARCHAR(100) | NOT NULL, UNIQUE | Login username |
| `PasswordHash` | VARCHAR(512) | NOT NULL | BCrypt password hash |
| `Role` | VARCHAR(30) | NOT NULL, DEFAULT 'Cashier' | One of: `Admin`, `Manager`, `Cashier` |
| `CreatedAt` | DATETIME | NOT NULL, DEFAULT CURRENT_TIMESTAMP | Account creation timestamp |

**Indexes:** Primary key on `Id`. Unique index on `Username`.

---

### `Suppliers`

Stores hardware product suppliers.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `Id` | INT | PK, AUTO_INCREMENT | Unique supplier ID |
| `Name` | VARCHAR(255) | NOT NULL, UNIQUE | Supplier name |
| `ContactInfo` | VARCHAR(255) | NULL | Optional contact details |
| `CreatedAt` | DATETIME | NOT NULL, DEFAULT CURRENT_TIMESTAMP | Record creation timestamp |

**Indexes:** Primary key on `Id`. Unique index on `Name`.

---

### `Products`

Core inventory table. Each product optionally belongs to a supplier.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `Id` | INT | PK, AUTO_INCREMENT | Unique product ID |
| `Name` | VARCHAR(255) | NOT NULL | Product name |
| `SKU` | VARCHAR(100) | NOT NULL | Stock Keeping Unit identifier |
| `Price` | DECIMAL(10,2) | NOT NULL | Unit selling price |
| `Quantity` | INT | NOT NULL | Current stock level |
| `Category` | VARCHAR(255) | NOT NULL | Product category |
| `SupplierId` | INT | NULL, FK → Suppliers(Id) | Linked supplier (nullable) |
| `CreatedAt` | DATETIME | NOT NULL, DEFAULT CURRENT_TIMESTAMP | Record creation timestamp |

**Foreign Keys:**
- `FK_Products_Suppliers`: `SupplierId` → `Suppliers(Id)` ON DELETE SET NULL

**Indexes:**
- `PK` on `Id`
- `IX_Products_SupplierId` on `SupplierId`
- `IX_Products_Quantity` on `Quantity`
- `IX_Products_SKU` on `SKU`
- `IX_Products_Category` on `Category`

---

### `StockLogs`

Audit trail for every stock quantity change (manual adjustment or sale deduction).

| Column | Type | Constraints | Description |
|---|---|---|---|
| `Id` | INT | PK, AUTO_INCREMENT | Unique log entry ID |
| `ProductId` | INT | NOT NULL, FK → Products(Id) | The product whose stock changed |
| `PreviousQuantity` | INT | NOT NULL | Stock level before the change |
| `NewQuantity` | INT | NOT NULL | Stock level after the change |
| `ChangeAmount` | INT | NOT NULL | Delta (positive = restock, negative = deduction) |
| `Reason` | VARCHAR(255) | NULL | Human-readable reason for the change |
| `UpdatedAt` | DATETIME | NOT NULL, DEFAULT CURRENT_TIMESTAMP | Timestamp of the change |

**Foreign Keys:**
- `FK_StockLogs_Products`: `ProductId` → `Products(Id)` ON DELETE CASCADE

**Indexes:**
- `PK` on `Id`
- `IX_StockLogs_ProductId` on `ProductId`

---

### `Sales`

Header table for a completed sales transaction.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `Id` | INT | PK, AUTO_INCREMENT | Unique transaction ID |
| `SoldAt` | DATETIME | NOT NULL, DEFAULT CURRENT_TIMESTAMP | Timestamp of the sale |
| `TotalAmount` | DECIMAL(10,2) | NOT NULL | Grand total of the transaction |
| `SoldBy` | VARCHAR(100) | NOT NULL | Username of the cashier/manager who processed the sale |

**Indexes:**
- `PK` on `Id`
- `IX_Sales_SoldAt` on `SoldAt`

---

### `SaleItems`

Line items within a sales transaction. Stores a snapshot of product details at time of sale.

| Column | Type | Constraints | Description |
|---|---|---|---|
| `Id` | INT | PK, AUTO_INCREMENT | Unique line item ID |
| `SaleId` | INT | NOT NULL, FK → Sales(Id) | Parent transaction |
| `ProductId` | INT | NOT NULL, FK → Products(Id) | Product sold |
| `ProductName` | VARCHAR(255) | NOT NULL | Snapshot of product name at time of sale |
| `SKU` | VARCHAR(100) | NOT NULL | Snapshot of SKU at time of sale |
| `UnitPrice` | DECIMAL(10,2) | NOT NULL | Snapshot of unit price at time of sale |
| `Quantity` | INT | NOT NULL | Quantity sold |
| `LineSubtotal` | DECIMAL(10,2) | NOT NULL | `UnitPrice × Quantity` |

**Foreign Keys:**
- `SaleId` → `Sales(Id)` ON DELETE CASCADE
- `ProductId` → `Products(Id)` (no cascade — product data is snapshotted)

**Indexes:**
- `PK` on `Id`
- `IX_SaleItems_SaleId` on `SaleId`
- `IX_SaleItems_ProductId` on `ProductId`

---

## Entity Relationships

```
Users
  (no FK relationships — standalone authentication table)

Suppliers ──(1:N)──► Products
  • A supplier can supply many products
  • Deleting a supplier sets SupplierId = NULL on linked products (ON DELETE SET NULL)

Products ──(1:N)──► StockLogs
  • Every stock change creates a StockLogs row
  • Deleting a product cascades to delete its StockLogs (ON DELETE CASCADE)

Products ──(M:N via SaleItems)──► Sales
  • A sale can contain many products
  • A product can appear in many sales
  • SaleItems is the join table
  • Deleting a sale cascades to delete its SaleItems (ON DELETE CASCADE)
  • SaleItems.ProductId has NO cascade — product data is already snapshotted
```

---

## Data Flow for a Sale Transaction

```
SalesController.CreateSale()
    │
    ▼ SaleRepository.CreateSaleAsync() [within MySqlTransaction]
    │
    ├── For each item:
    │     SELECT Id, Name, SKU, Price, Quantity FROM Products WHERE Id = @ProductId
    │     → validates product exists and Quantity >= requested qty
    │
    ├── For each item:
    │     UPDATE Products SET Quantity = Quantity - @Qty WHERE Id = @ProductId
    │
    ├── INSERT INTO Sales (SoldAt, TotalAmount, SoldBy) VALUES (...)
    │     → gets new Sale.Id via LAST_INSERT_ID()
    │
    ├── For each item:
    │     INSERT INTO SaleItems (SaleId, ProductId, ProductName, SKU, UnitPrice, Quantity, LineSubtotal)
    │
    └── COMMIT (or ROLLBACK on any error)
```

---

## Notes on Data Integrity

- `SaleItems` stores **snapshots** of product name, SKU, and price at time of sale. This means historical invoices remain accurate even if a product is later renamed or repriced.
- The `StockLogs` table provides a full audit trail for every quantity change — both manual adjustments (via `/api/Product/{id}/stock`) and automatic sale deductions.
- The `Users` table is entirely separate from all inventory/sales tables — there are no FK relationships between user records and operational data.
