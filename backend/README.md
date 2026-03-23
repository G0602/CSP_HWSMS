# HSMS Backend

ASP.NET Core 8 REST API for the Hardware Store Management System.

---

## Solution Structure

```
backend/
├── HSMS.sln
├── HSMS.API/            # Web host — controllers, middleware, Program.cs
├── HSMS.Application/    # Interfaces (contracts) & DTOs
├── HSMS.Domain/         # Entity classes (pure C#, no framework deps)
├── HSMS.Infrastructure/ # MySQL data-access (ADO.NET)
└── HSMS.Tests/          # Unit tests (xUnit + Moq)
```

### Layer responsibilities

| Project                | Role                                                                           |
|------------------------|--------------------------------------------------------------------------------|
| `HSMS.Domain`          | Core business objects (`Product`). No external dependencies.                  |
| `HSMS.Application`     | Defines **what** the system can do: `IProductRepository`, `IProductService`, DTOs. |
| `HSMS.Infrastructure`  | Implements `IProductRepository` using raw ADO.NET against MySQL.               |
| `HSMS.API`             | Hosts the ASP.NET Core pipeline; wires DI; exposes REST endpoints via controllers. |
| `HSMS.Tests`           | Unit-tests controller logic with an in-memory mock repository (no DB needed). |

---

## Key Files

| File | Description |
|------|-------------|
| [HSMS.API/Program.cs](HSMS.API/Program.cs) | DI registration, middleware pipeline, CORS config |
| [HSMS.API/Controllers/ProductController.cs](HSMS.API/Controllers/ProductController.cs) | All product CRUD endpoints |
| [HSMS.Application/Interfaces/IProductRepository.cs](HSMS.Application/Interfaces/IProductRepository.cs) | Repository contract |
| [HSMS.Application/DTOs/ProductCreateDTO.cs](HSMS.Application/DTOs/ProductCreateDTO.cs) | Create payload shape |
| [HSMS.Application/DTOs/ProductUpdateDTO.cs](HSMS.Application/DTOs/ProductUpdateDTO.cs) | Update payload shape |
| [HSMS.Domain/Entities/Product.cs](HSMS.Domain/Entities/Product.cs) | Product domain entity |
| [HSMS.Infrastructure/Repositories/ProductRepository.cs](HSMS.Infrastructure/Repositories/ProductRepository.cs) | MySQL ADO.NET implementation |
| [HSMS.Infrastructure/Data/DbConnectionFactory.cs](HSMS.Infrastructure/Data/DbConnectionFactory.cs) | MySQL connection factory |

---

## Configuration

### `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "server=localhost;database=CSP_HSMS;user=CSP;password=sql123;"
  }
}
```

Override with `appsettings.Development.json` or environment variables for other environments.

> **Auto-migration:** `ProductRepository` calls `EnsureProductsTableExists()` in its constructor, so the `Products` table is created automatically if it does not exist. No migration tooling needed.

---

## Running Locally

```bash
# from the backend/ directory
dotnet restore
dotnet run --project HSMS.API
```

Swagger UI: [http://localhost:5162/swagger](http://localhost:5162/swagger)

---

## Running Tests

```bash
dotnet test
```

Tests mock `IProductRepository` with Moq — no database connection required.

---

## API Endpoints

| Method   | URL                    | Description             | Success |
|----------|------------------------|-------------------------|---------|
| `GET`    | `/api/product`         | List all products       | 200     |
| `GET`    | `/api/product/{id}`    | Get product by Id       | 200 / 404 |
| `POST`   | `/api/product`         | Create new product      | 201 / 400 |
| `PUT`    | `/api/product/{id}`    | Update existing product | 200 / 404 |
| `DELETE` | `/api/product/{id}`    | Delete product          | 200 / 404 |

### Request body (`POST` / `PUT`)

```json
{
  "name":     "Claw Hammer",
  "sku":      "HMR-001",
  "price":    1500.00,
  "quantity": 25,
  "category": "Hand Tools"
}
```

### Validation rules (enforced in the controller)

- `price` must be **> 0**
- `quantity` must be **>= 0**

---

## NuGet Packages

| Package | Version | Used In |
|---------|---------|---------|
| `Swashbuckle.AspNetCore` | 6.6.2 | HSMS.API — Swagger UI |
| `Microsoft.AspNetCore.OpenApi` | 8.0.24 | HSMS.API — OpenAPI metadata |
| `MySql.Data` | latest | HSMS.Infrastructure — MySQL driver |
| `xunit` | 2.5.3 | HSMS.Tests |
| `Moq` | 4.20.72 | HSMS.Tests — repository mocking |
| `coverlet.collector` | 8.0.0 | HSMS.Tests — code coverage |

---

## CORS

The API allows requests from `http://localhost:5173` (Vite dev server) via the `FrontendPolicy`.  
Update `Program.cs` with the production frontend URL before deploying.
