#!/usr/bin/env node

const fs = require("fs");
const path = require("path");
const crypto = require("crypto");
const { spawnSync } = require("child_process");

const rootDir = path.resolve(__dirname, "..");
const backendEnvFile = path.join(rootDir, "backend", ".env");

function hasFlag(flag) {
  return process.argv.includes(flag);
}

function parseDotEnv(filePath) {
  if (!fs.existsSync(filePath)) {
    return {};
  }

  const lines = fs.readFileSync(filePath, "utf8").split(/\r?\n/);
  const result = {};

  for (const rawLine of lines) {
    const line = rawLine.trim();
    if (!line || line.startsWith("#")) {
      continue;
    }

    const equalIndex = line.indexOf("=");
    if (equalIndex === -1) {
      continue;
    }

    const key = line.slice(0, equalIndex).trim();
    const value = line.slice(equalIndex + 1).trim().replace(/^['"]|['"]$/g, "");
    result[key] = value;
  }

  return result;
}

function parseConnectionString(connectionString) {
  const parts = connectionString
    .split(";")
    .map((part) => part.trim())
    .filter(Boolean);

  const map = {};
  for (const part of parts) {
    const [rawKey, ...rawValue] = part.split("=");
    if (!rawKey || rawValue.length === 0) {
      continue;
    }

    map[rawKey.trim().toLowerCase()] = rawValue.join("=").trim();
  }

  return {
    host: map.server || map.host || "localhost",
    port: map.port || "3306",
    database: map.database || map["initial catalog"] || "",
    user: map.user || map.uid || map["user id"] || map.username || "root",
    password: map.password || map.pwd || ""
  };
}

function parseDatabaseUrl(databaseUrl) {
  if (!databaseUrl) {
    return null;
  }

  try {
    const parsed = new URL(databaseUrl);
    return {
      host: parsed.hostname || "",
      port: parsed.port || "3306",
      database: parsed.pathname ? parsed.pathname.replace(/^\//, "") : "",
      user: decodeURIComponent(parsed.username || ""),
      password: decodeURIComponent(parsed.password || "")
    };
  } catch {
    const parsedConnection = parseConnectionString(databaseUrl);
    if (!parsedConnection) {
      return null;
    }

    return {
      host: parsedConnection.host || "",
      port: parsedConnection.port || "3306",
      database: parsedConnection.database || "",
      user: parsedConnection.user || "",
      password: parsedConnection.password || ""
    };
  }
}

function loadConfig() {
  const fileEnv = parseDotEnv(backendEnvFile);
  const merged = { ...fileEnv, ...process.env };
  const online = hasFlag("--online");
  const dryRun = hasFlag("--dry-run");
  const help = hasFlag("--help") || hasFlag("-h");

  const onlineDatabaseUrl =
    merged.ONLINE_DB_URL ||
    merged.RAILWAY_DB_URL ||
    merged.MYSQL_URL ||
    merged.DATABASE_URL ||
    "";
  const parsedOnlineUrl = parseDatabaseUrl(onlineDatabaseUrl);

  const onlineConfig = {
    host:
      merged.ONLINE_DB_SERVER ||
      merged.MYSQLHOST ||
      merged.RAILWAY_TCP_PROXY_DOMAIN ||
      parsedOnlineUrl?.host ||
      "",
    port:
      merged.ONLINE_DB_PORT ||
      merged.MYSQLPORT ||
      merged.RAILWAY_TCP_PROXY_PORT ||
      parsedOnlineUrl?.port ||
      "3306",
    database:
      merged.ONLINE_DB_NAME ||
      merged.MYSQLDATABASE ||
      merged.RAILWAY_DATABASE ||
      merged.PGDATABASE ||
      parsedOnlineUrl?.database ||
      "",
    user: merged.ONLINE_DB_USER || merged.MYSQLUSER || parsedOnlineUrl?.user || "",
    password: merged.ONLINE_DB_PASSWORD || merged.MYSQLPASSWORD || parsedOnlineUrl?.password || ""
  };

  const connectionString =
    merged.ConnectionStrings__DefaultConnection ||
    merged.CONNECTIONSTRINGS__DEFAULTCONNECTION ||
    merged.DEFAULT_CONNECTION_STRING ||
    "";

  const parsedConnection = connectionString ? parseConnectionString(connectionString) : null;

  const localConfig = {
    host: merged.DB_SERVER || parsedConnection?.host || "localhost",
    port: merged.DB_PORT || parsedConnection?.port || "3306",
    database: merged.DB_NAME || parsedConnection?.database || "CSP_HSMS",
    user: merged.DB_USER || parsedConnection?.user || "root",
    password: merged.DB_PASSWORD || parsedConnection?.password || ""
  };

  const selected = online ? onlineConfig : localConfig;

  return {
    ...selected,
    online,
    dryRun,
    help
  };
}

function hashPassword(password) {
  const iterations = 100000;
  const salt = crypto.randomBytes(16);
  const hash = crypto.pbkdf2Sync(password, salt, iterations, 32, "sha256");
  return `${iterations}.${salt.toString("base64")}.${hash.toString("base64")}`;
}

function sqlString(value) {
  if (value === null || value === undefined) {
    return "NULL";
  }

  return `'${String(value).replace(/\\/g, "\\\\").replace(/'/g, "''")}'`;
}

function buildSeedData() {
  const now = new Date();
  const isoDate = now.toISOString().slice(0, 19).replace("T", " ");
  const adminPassword = process.env.ADMIN_PASSWORD || "change-admin-password";
  const managerPassword = process.env.MANAGER_PASSWORD || "change-manager-password";
  const cashierPassword = process.env.CASHIER_PASSWORD || "change-cashier-password";
  const cashier2Password = process.env.CASHIER_2_PASSWORD || "change-cashier-2-password";

  const users = [
    { id: 1, username: "admin", password: adminPassword, role: "Admin" },
    { id: 2, username: "manager", password: managerPassword, role: "Manager" },
    { id: 3, username: "cashier_1", password: cashierPassword, role: "Cashier" },
    { id: 4, username: "cashier_2", password: cashier2Password, role: "Cashier" }
  ];

  const suppliers = [
    { id: 1, name: "Lanka Hardware Imports", contactInfo: "011-2456789 | lanka.hardware@example.com" },
    { id: 2, name: "Prime Tools Distributors", contactInfo: "077-1234567 | prime.tools@example.com" },
    { id: 3, name: "BuildPro Supplies", contactInfo: "076-9988776 | buildpro@example.com" },
    { id: 4, name: "Metro Electricals", contactInfo: "071-4561230 | metro.electricals@example.com" }
  ];

  const products = [
    { id: 1, name: "Claw Hammer", sku: "HAM-001", price: 1850.0, quantity: 42, category: "Hand Tools", supplierId: 1 },
    { id: 2, name: "Phillips Screwdriver Set", sku: "SCR-002", price: 2250.0, quantity: 30, category: "Hand Tools", supplierId: 2 },
    { id: 3, name: "Adjustable Spanner", sku: "SPN-003", price: 3150.0, quantity: 18, category: "Hand Tools", supplierId: 2 },
    { id: 4, name: "Electric Drill 650W", sku: "DRL-004", price: 18950.0, quantity: 9, category: "Power Tools", supplierId: 3 },
    { id: 5, name: "Circular Saw Blade 7in", sku: "SAW-005", price: 4200.0, quantity: 7, category: "Power Tools", supplierId: 3 },
    { id: 6, name: "PVC Pipe 1in", sku: "PLB-006", price: 980.0, quantity: 64, category: "Plumbing", supplierId: 1 },
    { id: 7, name: "LED Bulb 12W", sku: "ELC-007", price: 650.0, quantity: 85, category: "Electrical", supplierId: 4 },
    { id: 8, name: "Extension Cord 5m", sku: "ELC-008", price: 2450.0, quantity: 14, category: "Electrical", supplierId: 4 },
    { id: 9, name: "Concrete Nails 2in Pack", sku: "FST-009", price: 720.0, quantity: 120, category: "Fasteners", supplierId: 1 },
    { id: 10, name: "Wall Plug Set", sku: "FST-010", price: 540.0, quantity: 95, category: "Fasteners", supplierId: 2 },
    { id: 11, name: "Paint Brush 2in", sku: "PNT-011", price: 890.0, quantity: 26, category: "Painting", supplierId: 3 },
    { id: 12, name: "Safety Gloves", sku: "SAF-012", price: 1100.0, quantity: 11, category: "Safety", supplierId: 1 }
  ];

  const stockLogs = [
    { id: 1, productId: 4, previousQuantity: 12, newQuantity: 9, changeAmount: -3, reason: "Initial demo sales adjustment" },
    { id: 2, productId: 5, previousQuantity: 10, newQuantity: 7, changeAmount: -3, reason: "Initial demo sales adjustment" },
    { id: 3, productId: 8, previousQuantity: 20, newQuantity: 14, changeAmount: -6, reason: "Initial demo sales adjustment" },
    { id: 4, productId: 12, previousQuantity: 15, newQuantity: 11, changeAmount: -4, reason: "Initial demo sales adjustment" }
  ];

  const sales = [
    { id: 1, soldAt: "2026-04-01 09:15:00", totalAmount: 4900.0, soldBy: "cashier_user" },
    { id: 2, soldAt: "2026-04-01 14:20:00", totalAmount: 24060.0, soldBy: "manager_user" },
    { id: 3, soldAt: "2026-04-03 11:05:00", totalAmount: 3560.0, soldBy: "cashier_2" },
    { id: 4, soldAt: "2026-04-05 16:40:00", totalAmount: 18600.0, soldBy: "admin_user" },
    { id: 5, soldAt: "2026-04-06 10:10:00", totalAmount: 6740.0, soldBy: "cashier_user" },
    { id: 6, soldAt: "2026-04-06 17:30:00", totalAmount: 9290.0, soldBy: "manager_user" }
  ];

  const saleItems = [
    { id: 1, saleId: 1, productId: 1, productName: "Claw Hammer", sku: "HAM-001", unitPrice: 1850.0, quantity: 2, lineSubtotal: 3700.0 },
    { id: 2, saleId: 1, productId: 12, productName: "Safety Gloves", sku: "SAF-012", unitPrice: 1100.0, quantity: 1, lineSubtotal: 1100.0 },
    { id: 3, saleId: 2, productId: 4, productName: "Electric Drill 650W", sku: "DRL-004", unitPrice: 18950.0, quantity: 1, lineSubtotal: 18950.0 },
    { id: 4, saleId: 2, productId: 7, productName: "LED Bulb 12W", sku: "ELC-007", unitPrice: 650.0, quantity: 4, lineSubtotal: 2600.0 },
    { id: 5, saleId: 2, productId: 10, productName: "Wall Plug Set", sku: "FST-010", unitPrice: 540.0, quantity: 3, lineSubtotal: 1620.0 },
    { id: 6, saleId: 2, productId: 11, productName: "Paint Brush 2in", sku: "PNT-011", unitPrice: 890.0, quantity: 1, lineSubtotal: 890.0 },
    { id: 7, saleId: 3, productId: 6, productName: "PVC Pipe 1in", sku: "PLB-006", unitPrice: 980.0, quantity: 2, lineSubtotal: 1960.0 },
    { id: 8, saleId: 3, productId: 9, productName: "Concrete Nails 2in Pack", sku: "FST-009", unitPrice: 720.0, quantity: 1, lineSubtotal: 720.0 },
    { id: 9, saleId: 3, productId: 7, productName: "LED Bulb 12W", sku: "ELC-007", unitPrice: 650.0, quantity: 1, lineSubtotal: 650.0 },
    { id: 10, saleId: 3, productId: 10, productName: "Wall Plug Set", sku: "FST-010", unitPrice: 540.0, quantity: 1, lineSubtotal: 540.0 },
    { id: 11, saleId: 4, productId: 4, productName: "Electric Drill 650W", sku: "DRL-004", unitPrice: 18950.0, quantity: 1, lineSubtotal: 18950.0 },
    { id: 12, saleId: 4, productId: 12, productName: "Safety Gloves", sku: "SAF-012", unitPrice: 1100.0, quantity: 1, lineSubtotal: 1100.0 },
    { id: 13, saleId: 5, productId: 8, productName: "Extension Cord 5m", sku: "ELC-008", unitPrice: 2450.0, quantity: 2, lineSubtotal: 4900.0 },
    { id: 14, saleId: 5, productId: 7, productName: "LED Bulb 12W", sku: "ELC-007", unitPrice: 650.0, quantity: 2, lineSubtotal: 1300.0 },
    { id: 15, saleId: 5, productId: 10, productName: "Wall Plug Set", sku: "FST-010", unitPrice: 540.0, quantity: 1, lineSubtotal: 540.0 },
    { id: 16, saleId: 5, productId: 9, productName: "Concrete Nails 2in Pack", sku: "FST-009", unitPrice: 720.0, quantity: 0, lineSubtotal: 0.0 },
    { id: 17, saleId: 6, productId: 5, productName: "Circular Saw Blade 7in", sku: "SAW-005", unitPrice: 4200.0, quantity: 2, lineSubtotal: 8400.0 },
    { id: 18, saleId: 6, productId: 11, productName: "Paint Brush 2in", sku: "PNT-011", unitPrice: 890.0, quantity: 1, lineSubtotal: 890.0 }
  ].filter((item) => item.quantity > 0);

  return {
    generatedAt: isoDate,
    users: users.map((user) => ({ ...user, passwordHash: hashPassword(user.password) })),
    suppliers,
    products,
    stockLogs,
    sales,
    saleItems,
    credentials: users.map(({ username, password, role }) => ({ username, password, role }))
  };
}

function buildSql(seed) {
  const userValues = seed.users
    .map((user) =>
      `(${user.id}, ${sqlString(user.username)}, ${sqlString(user.passwordHash)}, ${sqlString(user.role)}, ${sqlString(seed.generatedAt)})`)
    .join(",\n");

  const supplierValues = seed.suppliers
    .map((supplier) =>
      `(${supplier.id}, ${sqlString(supplier.name)}, ${sqlString(supplier.contactInfo)}, ${sqlString(seed.generatedAt)})`)
    .join(",\n");

  const productValues = seed.products
    .map((product) =>
      `(${product.id}, ${sqlString(product.name)}, ${sqlString(product.sku)}, ${product.price.toFixed(2)}, ${product.quantity}, ${sqlString(product.category)}, ${product.supplierId}, ${sqlString(seed.generatedAt)})`)
    .join(",\n");

  const stockLogValues = seed.stockLogs
    .map((log) =>
      `(${log.id}, ${log.productId}, ${log.previousQuantity}, ${log.newQuantity}, ${log.changeAmount}, ${sqlString(log.reason)}, ${sqlString(seed.generatedAt)})`)
    .join(",\n");

  const salesValues = seed.sales
    .map((sale) =>
      `(${sale.id}, ${sqlString(sale.soldAt)}, ${sale.totalAmount.toFixed(2)}, ${sqlString(sale.soldBy)})`)
    .join(",\n");

  const saleItemValues = seed.saleItems
    .map((item) =>
      `(${item.id}, ${item.saleId}, ${item.productId}, ${sqlString(item.productName)}, ${sqlString(item.sku)}, ${item.unitPrice.toFixed(2)}, ${item.quantity}, ${item.lineSubtotal.toFixed(2)})`)
    .join(",\n");

  return `
CREATE TABLE IF NOT EXISTS Suppliers (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  Name VARCHAR(255) NOT NULL UNIQUE,
  ContactInfo VARCHAR(255) NULL,
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS Products (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  Name VARCHAR(255) NOT NULL,
  SKU VARCHAR(100) NOT NULL,
  Price DECIMAL(10,2) NOT NULL,
  Quantity INT NOT NULL,
  Category VARCHAR(255) NOT NULL,
  SupplierId INT NULL,
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS Users (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  Username VARCHAR(100) NOT NULL UNIQUE,
  PasswordHash VARCHAR(512) NOT NULL,
  Role VARCHAR(30) NOT NULL DEFAULT 'Cashier',
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS Sales (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  SoldAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  TotalAmount DECIMAL(10,2) NOT NULL,
  SoldBy VARCHAR(100) NOT NULL
);

CREATE TABLE IF NOT EXISTS SaleItems (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  SaleId INT NOT NULL,
  ProductId INT NOT NULL,
  ProductName VARCHAR(255) NOT NULL,
  SKU VARCHAR(100) NOT NULL,
  UnitPrice DECIMAL(10,2) NOT NULL,
  Quantity INT NOT NULL,
  LineSubtotal DECIMAL(10,2) NOT NULL,
  CONSTRAINT FK_SaleItems_Sales FOREIGN KEY (SaleId) REFERENCES Sales(Id) ON DELETE CASCADE,
  CONSTRAINT FK_SaleItems_Products FOREIGN KEY (ProductId) REFERENCES Products(Id)
);

CREATE TABLE IF NOT EXISTS StockLogs (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  ProductId INT NOT NULL,
  PreviousQuantity INT NOT NULL,
  NewQuantity INT NOT NULL,
  ChangeAmount INT NOT NULL,
  Reason VARCHAR(255) NULL,
  UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  INDEX IX_StockLogs_ProductId (ProductId),
  CONSTRAINT FK_StockLogs_Products FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE CASCADE
);

SET FOREIGN_KEY_CHECKS = 0;
TRUNCATE TABLE SaleItems;
TRUNCATE TABLE Sales;
TRUNCATE TABLE StockLogs;
TRUNCATE TABLE Products;
TRUNCATE TABLE Suppliers;
TRUNCATE TABLE Users;
SET FOREIGN_KEY_CHECKS = 1;

INSERT INTO Users (Id, Username, PasswordHash, Role, CreatedAt)
VALUES
${userValues};

INSERT INTO Suppliers (Id, Name, ContactInfo, CreatedAt)
VALUES
${supplierValues};

INSERT INTO Products (Id, Name, SKU, Price, Quantity, Category, SupplierId, CreatedAt)
VALUES
${productValues};

INSERT INTO StockLogs (Id, ProductId, PreviousQuantity, NewQuantity, ChangeAmount, Reason, UpdatedAt)
VALUES
${stockLogValues};

INSERT INTO Sales (Id, SoldAt, TotalAmount, SoldBy)
VALUES
${salesValues};

INSERT INTO SaleItems (Id, SaleId, ProductId, ProductName, SKU, UnitPrice, Quantity, LineSubtotal)
VALUES
${saleItemValues};

ALTER TABLE Users AUTO_INCREMENT = 100;
ALTER TABLE Suppliers AUTO_INCREMENT = 100;
ALTER TABLE Products AUTO_INCREMENT = 100;
ALTER TABLE StockLogs AUTO_INCREMENT = 100;
ALTER TABLE Sales AUTO_INCREMENT = 100;
ALTER TABLE SaleItems AUTO_INCREMENT = 100;
`;
}

function printHelp() {
  console.log(`Usage:
  node scripts/reset-and-seed-db.js
  node scripts/reset-and-seed-db.js --dry-run
  node scripts/reset-and-seed-db.js --online
  node scripts/reset-and-seed-db.js --online --dry-run

Configuration sources, in order:
  Local mode (default):
    1. Environment variables in the current shell
    2. backend/.env
    3. ConnectionStrings__DefaultConnection

  Online mode (--online):
    1. ONLINE_DB_URL | RAILWAY_DB_URL | MYSQL_URL | DATABASE_URL
    2. ONLINE_DB_* or Railway MYSQL* variables (MYSQLHOST, MYSQLPORT, MYSQLDATABASE, MYSQLUSER, MYSQLPASSWORD)

Supported environment variables:
  Local:  DB_SERVER, DB_PORT, DB_NAME, DB_USER, DB_PASSWORD
  Online: ONLINE_DB_URL, RAILWAY_DB_URL, MYSQL_URL, DATABASE_URL
          ONLINE_DB_SERVER, ONLINE_DB_PORT, ONLINE_DB_NAME, ONLINE_DB_USER, ONLINE_DB_PASSWORD
          MYSQLHOST, MYSQLPORT, MYSQLDATABASE, MYSQLUSER, MYSQLPASSWORD

This script will:
  - create required tables if missing
  - clean the database
  - insert dummy users, suppliers, products, stock logs, sales, and sale items
`);
}

function runMysql(config, sql) {
  const args = [
    `--host=${config.host}`,
    `--port=${config.port}`,
    `--user=${config.user}`,
    "--default-character-set=utf8mb4",
    config.database
  ];

  const env = { ...process.env };
  if (config.password) {
    env.MYSQL_PWD = config.password;
  }

  const result = spawnSync("mysql", args, {
    input: sql,
    encoding: "utf8",
    env
  });

  if (result.status !== 0) {
    const errorMessage = result.stderr || result.stdout || "mysql command failed";
    throw new Error(errorMessage.trim());
  }
}

function main() {
  const config = loadConfig();

  if (config.help) {
    printHelp();
    return;
  }

  if (!config.database) {
    throw new Error(
      config.online
        ? "Database name is missing for online mode. Set MYSQL_URL/DATABASE_URL (or ONLINE_DB_NAME)."
        : "Database name is missing. Set DB_NAME or ConnectionStrings__DefaultConnection."
    );
  }

  if (config.online && (!config.host || !config.user)) {
    throw new Error("Online mode requires host and user. Set MYSQL_URL/DATABASE_URL or ONLINE_DB_* / MYSQL* variables.");
  }

  if (config.online && config.host && config.host.endsWith(".railway.internal")) {
    throw new Error(
      "The host '" +
        config.host +
        "' is a private Railway hostname and cannot be reached from your local machine. " +
        "Use Railway public TCP proxy values (MYSQLHOST, MYSQLPORT, MYSQLDATABASE, MYSQLUSER, MYSQLPASSWORD) " +
        "or ONLINE_DB_SERVER/ONLINE_DB_PORT/ONLINE_DB_NAME/ONLINE_DB_USER/ONLINE_DB_PASSWORD."
    );
  }

  const seed = buildSeedData();
  const sql = buildSql(seed);

  if (config.dryRun) {
    console.log(`Dry run only. Parsed database config (${config.online ? "online" : "local"} mode):`);
    console.log(JSON.stringify({
      host: config.host,
      port: config.port,
      database: config.database,
      user: config.user,
      passwordConfigured: Boolean(config.password)
    }, null, 2));
    console.log("\nSeeded demo credentials:");
    for (const credential of seed.credentials) {
      console.log(`- ${credential.role}: ${credential.username} / ${credential.password}`);
    }
    return;
  }

  runMysql(config, sql);

  console.log(`Database '${config.database}' was cleaned and seeded successfully (${config.online ? "online" : "local"} mode).`);
  console.log("Demo login credentials:");
  for (const credential of seed.credentials) {
    console.log(`- ${credential.role}: ${credential.username} / ${credential.password}`);
  }
}

try {
  main();
} catch (error) {
  console.error("Failed to reset and seed database.");
  console.error(error instanceof Error ? error.message : String(error));
  process.exit(1);
}
