# HSMS Backend

This is the backend for the **Hardware/Software Management System (HSMS)**, built with **ASP.NET Core 8** following a **Clean Architecture** pattern.

## Project Structure

```
backend/
├── HSMS.sln                  # Solution file
├── HSMS.API/                 # Entry point — ASP.NET Core Web API
│   ├── Program.cs            # Application entry point
│   ├── appsettings.json      # Production configuration
│   ├── appsettings.Development.json  # Development configuration
│   └── Properties/
│       └── launchSettings.json       # Launch profiles (URLs, env vars)
├── HSMS.Application/         # Application layer (use cases / services)
├── HSMS.Domain/              # Domain layer (entities, value objects, interfaces)
├── HSMS.Infrastructure/      # Infrastructure layer (database, external services)
└── HSMS.Tests/               # Unit/integration tests (xUnit)
```

### Layer Responsibilities

| Project | Role |
|---|---|
| **HSMS.API** | HTTP request handling, routing, middleware, Swagger |
| **HSMS.Application** | Business logic, use cases, DTOs, service interfaces |
| **HSMS.Domain** | Core entities, domain events, repository interfaces |
| **HSMS.Infrastructure** | EF Core / database, external integrations, repository implementations |
| **HSMS.Tests** | xUnit test project for unit and integration tests |

## Entry Point

The entry point is **`HSMS.API/Program.cs`**.

It uses the ASP.NET Core minimal hosting model:
- Registers services via `builder.Services`
- Builds the `WebApplication`
- Configures the HTTP pipeline (Swagger in Development)
- Maps API endpoints
- Calls `app.Run()` to start the server

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

Verify your installation:

```bash
dotnet --version
# should output 8.x.x
```

## How to Run

### From the `backend/` directory (recommended)

```bash
cd backend

# HTTP only
dotnet run --project HSMS.API --launch-profile http

# HTTP + HTTPS
dotnet run --project HSMS.API --launch-profile https
```

### From the `HSMS.API/` directory

```bash
cd backend/HSMS.API
dotnet run
```

The API will start on:

| Profile | URL |
|---|---|
| http | http://localhost:5162 |
| https | https://localhost:7111 and http://localhost:5162 |

## Swagger / OpenAPI

When running in **Development** mode, the interactive Swagger UI is available at:

```
http://localhost:5162/swagger
```

It lists all available API endpoints and lets you send test requests directly from the browser.

## Running Tests

```bash
cd backend
dotnet test
```

## Building

```bash
cd backend
dotnet build
```
