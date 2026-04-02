# HSMS — Hardware Store Management System

A full-stack inventory management web application for a hardware store, built as a SLIIT Year 3 Semester 1 CSP assignment.

---

## Table of Contents

1. [Overview](#overview)
2. [Tech Stack](#tech-stack)
3. [Architecture](#architecture)
4. [Project Structure](#project-structure)
5. [Prerequisites](#prerequisites)
6. [Getting Started](#getting-started)
   - [Backend (local)](#backend-local)
   - [Frontend (local)](#frontend-local)
   - [Docker (full stack)](#docker-full-stack)
7. [Environment Variables](#environment-variables)
8. [API Reference](#api-reference)
9. [Running Tests](#running-tests)
10. [Database Schema](#database-schema)

---

## Overview

HSMS lets hardware-store staff manage their product inventory through a single-page React dashboard. The dashboard communicates with a RESTful ASP.NET Core API backed by a MySQL database. Products can be **created**, **viewed**, **updated**, and **deleted** (full CRUD).

---

## Tech Stack

| Concern       | Technology                                      |
|---------------|-------------------------------------------------|
| Backend       | ASP.NET Core 8 (C# 12), .NET 8                 |
| Database      | MySQL 8                                         |
| ORM / DA      | Raw ADO.NET (`MySql.Data`)                      |
| API Docs      | Swagger / OpenAPI (Swashbuckle 6)               |
| Frontend      | React 19, TypeScript, Vite 6                    |
| Styling       | Tailwind CSS 3                                  |
| HTTP Client   | Axios                                           |
| Unit Tests    | xUnit 2.5, Moq 4.20, coverlet                  |
| Containerise  | Docker, Docker Compose                          |

---

## Architecture

The backend follows **Clean Architecture** with four independently-compilable projects:

```
┌─────────────────────────────────────────────────────────┐
│  HSMS.API  (ASP.NET Core — Controllers, Program.cs)     │
│     │  depends on ↓                                     │
│  HSMS.Application  (Interfaces, DTOs)                   │
│     │  depends on ↓                                     │
│  HSMS.Domain  (Entities — pure C# classes)              │
│                                                         │
│  HSMS.Infrastructure  (Repository — MySQL / ADO.NET)   │
│     └─ implements HSMS.Application interfaces           │
│                                                         │
│  HSMS.Tests  (xUnit + Moq unit tests for the API)      │
└─────────────────────────────────────────────────────────┘
```

Dependency direction: `API → Application ← Infrastructure`, with `Domain` at the centre. The API never references `Infrastructure` directly — it only knows the `IProductRepository` interface.

---

## Project Structure

```
CSP_HWSMS/
├── backend/
│   ├── HSMS.sln
│   ├── HSMS.API/               # Web host: controllers, middleware, DI setup
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   └── Controllers/
│   │       └── ProductController.cs
│   ├── HSMS.Application/       # Business contracts (interfaces + DTOs)
│   │   ├── DTOs/
│   │   │   ├── ProductCreateDTO.cs
│   │   │   └── ProductUpdateDTO.cs
│   │   └── Interfaces/
│   │       ├── IProductRepository.cs
│   │       └── IProductService.cs
│   ├── HSMS.Domain/            # Pure domain entities (no framework dependencies)
│   │   └── Entities/
│   │       └── Product.cs
│   ├── HSMS.Infrastructure/    # Data access: MySQL via ADO.NET
│   │   ├── Data/
│   │   │   └── DbConnectionFactory.cs
│   │   └── Repositories/
│   │       └── ProductRepository.cs
│   └── HSMS.Tests/             # Unit tests
│       ├── ProductControllerTests.cs
│       └── ProductBasicTests.cs
├── frontend/
│   └── HWSMS_UI/               # React + TypeScript + Vite SPA
│       ├── src/
│       │   ├── pages/          # ProductPage, ProductDashboard
│       │   ├── components/     # Navbar, ProductTable, ProductForm, StatsCard, …
│       │   └── services/       # Axios API wrapper
│       ├── .env.example
│       └── package.json
├── hardware-store-hsms/
│   ├── docker-compose.yml
│   └── docker/
│       ├── backend.Dockerfile
│       └── frontend.Dockerfile
└── docs/
    ├── Diagrams/
    │   ├── Usecase_Diagram_HWSMS_CSP..drawio
    │   └── Sequence_diagrams/
    ├── SRS/
    └── Test-Documents/
```

---

## Prerequisites

| Tool             | Minimum Version | Notes                                  |
|------------------|-----------------|----------------------------------------|
| .NET SDK         | 8.0             | `dotnet --version`                     |
| MySQL Server     | 8.0             | Running locally or via Docker          |
| Node.js          | 18 LTS          | `node --version`                       |
| npm              | 9+              | Bundled with Node                      |
| Docker + Compose | Latest          | Only needed for containerised run      |

---

## Getting Started

### Backend (local)

1. **Configure the connection string**

   Edit `backend/HSMS.API/appsettings.json` (or create `appsettings.Development.json` to override):

   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "server=localhost;database=CSP_HSMS;user=YOUR_USER;password=YOUR_PASSWORD;"
     }
   }
   ```

   > The application creates the `Products` table automatically on first startup — no migration step needed.

2. **Restore dependencies and run**

   ```bash
   cd backend
   dotnet restore
   dotnet run --project HSMS.API
   ```

3. **Swagger UI** is available at:

   ```
   http://localhost:5162/swagger
   ```

---

### Frontend (local)

1. **Copy the environment file**

   ```bash
   cd frontend/HWSMS_UI
   cp .env.example .env
   ```

   The default value points to the local API:

   ```env
   VITE_API_URL=http://localhost:5162/api/Product
   ```

2. **Install dependencies and start the dev server**

   ```bash
   npm install
   npm run dev
   ```

3. The app is served at `http://localhost:5173`.

---

### Docker (full stack)

> The Docker Compose file is in `hardware-store-hsms/`. Dockerfiles are in `hardware-store-hsms/docker/`.

```bash
cd hardware-store-hsms
docker compose up --build
```

| Service   | Exposed Port |
|-----------|-------------|
| Backend   | 5000        |
| Frontend  | 3000        |
| MySQL DB  | 3306        |

---

## Environment Variables

### Frontend (`frontend/HWSMS_UI/.env`)

| Variable       | Default                                   | Description                              |
|----------------|-------------------------------------------|------------------------------------------|
| `VITE_API_URL` | `http://localhost:5162/api/Product`       | Base URL for all product API requests    |

### Backend (`appsettings.json`)

| Key                                  | Description                         |
|--------------------------------------|-------------------------------------|
| `ConnectionStrings:DefaultConnection`| Full MySQL connection string        |

---

## API Reference

Base URL: `http://localhost:5162/api/product`

All request/response bodies are JSON. Interactive docs available via `/swagger`.

### Product Endpoints

#### `GET /api/product`

Returns all products.

**Response `200 OK`**
```json
[
  {
    "id": 1,
    "name": "Claw Hammer",
    "sku": "HMR-001",
    "price": 1500.00,
    "quantity": 25,
    "category": "Hand Tools",
    "createdAt": "2026-03-01T08:00:00"
  }
]
```

---

#### `GET /api/product/{id}`

Returns a single product by Id.

| Parameter | Type | Description        |
|-----------|------|--------------------|
| `id`      | int  | Product primary key |

**Responses**

| Status | Meaning                        |
|--------|--------------------------------|
| 200    | Product found — returns object |
| 404    | No product with that Id        |

---

#### `POST /api/product`

Creates a new product.

**Request body**
```json
{
  "name": "Claw Hammer",
  "sku": "HMR-001",
  "price": 1500.00,
  "quantity": 25,
  "category": "Hand Tools"
}
```

**Validation rules**
- `price` must be `> 0`
- `quantity` must be `>= 0`

**Responses**

| Status | Meaning                                          |
|--------|--------------------------------------------------|
| 201    | Created — Location header points to new resource |
| 400    | Validation failed (price ≤ 0 or negative qty)    |

---

#### `PUT /api/product/{id}`

Fully replaces an existing product's fields.

**Request body** — same shape as `POST`.

**Responses**

| Status | Meaning                 |
|--------|-------------------------|
| 200    | Updated successfully    |
| 404    | Product not found       |

---

#### `DELETE /api/product/{id}`

Permanently removes a product.

**Responses**

| Status | Meaning                 |
|--------|-------------------------|
| 200    | Deleted successfully    |
| 404    | Product not found       |

---

## Running Tests

The test suite uses **xUnit** for test structure and **Moq** to mock the repository, so no database connection is required.

```bash
cd backend
dotnet test
```

### Test coverage

| Test                                                       | Asserts                         |
|------------------------------------------------------------|---------------------------------|
| `GetProducts_Should_Return_Ok`                             | `200 OK` with product list      |
| `AddProduct_Should_Return_Created`                         | `201 Created`                   |
| `DeleteProduct_Should_Return_Ok_When_Deleted`              | `200 OK`                        |
| `UpdateProduct_Should_Return_NotFound_When_Not_Updated`    | `404 Not Found`                 |

---

## Database Schema

The `Products` table is created automatically by `ProductRepository` if it does not exist.

```sql
CREATE TABLE IF NOT EXISTS Products (
    Id         INT            AUTO_INCREMENT PRIMARY KEY,
    Name       VARCHAR(255)   NOT NULL,
    SKU        VARCHAR(100)   NOT NULL,
    Price      DECIMAL(10,2)  NOT NULL,
    Quantity   INT            NOT NULL,
    Category   VARCHAR(255)   NOT NULL,
    CreatedAt  DATETIME       NOT NULL DEFAULT CURRENT_TIMESTAMP
);
```
