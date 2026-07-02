# Maintenance Guide

## Routine Maintenance Tasks

---

## 1. Adding a New API Endpoint

### Backend (minimal checklist)

1. **Define or reuse a DTO** in `HSMS.Application/DTOs/`.
2. **Add the interface method** to the appropriate interface in `HSMS.Application/Interfaces/`.
3. **Implement the method** in the corresponding repository in `HSMS.Infrastructure/Repositories/`.
4. **Add the controller action** to the appropriate controller in `HSMS.API/Controllers/`.
   - Apply the correct `[Authorize(Policy = AuthPolicies.XYZ)]` attribute.
   - Follow the existing response patterns (`Ok()`, `NotFound()`, `BadRequest()`, `Conflict()`).
5. **Write API tests** in the corresponding `HSMS.ApiTests/` folder.
6. **Write unit tests** in `HSMS.Tests/` if any business logic is involved.

### Frontend (minimal checklist)

1. **Add the service method** to the appropriate service file in `src/services/`.
2. **Call the method** from the relevant page or component.
3. **Update types** if a new DTO shape is returned.

---

## 2. Adding a New Database Table or Column

The database is managed by `DatabaseInitializer.cs` — there is no separate migration tool.

### Adding a new table

Add a new `CREATE TABLE IF NOT EXISTS` block inside `DatabaseInitializer.InitializeAsync()`,
**after** any tables it has foreign key dependencies on.

```csharp
const string newTableSql = @"CREATE TABLE IF NOT EXISTS MyNewTable (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    SomeColumn VARCHAR(255) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);";
await ExecuteNonQueryAsync(connection, newTableSql, cancellationToken);
```

### Adding a new column to an existing table

Use `EnsureColumnExistsAsync()` — it will only execute the ALTER TABLE if the column does not
already exist:

```csharp
await EnsureColumnExistsAsync(
    connection,
    "MyTable",
    "NewColumn",
    "ALTER TABLE MyTable ADD COLUMN NewColumn VARCHAR(255) NULL;",
    cancellationToken);
```

> **Never** drop columns or tables through the initializer. Destructive changes require
> a manual migration script run against the production database.

### Adding a new index

Use `EnsureIndexExistsAsync()`:

```csharp
await EnsureIndexExistsAsync(
    connection,
    "MyTable",
    "IX_MyTable_NewColumn",
    "CREATE INDEX IX_MyTable_NewColumn ON MyTable (NewColumn);",
    cancellationToken);
```

---

## 3. Adding a New User Role

1. **Add the role constant** in `HSMS.API/Auth/AppRoles.cs`:
   ```csharp
   public const string Supervisor = "Supervisor";
   ```

2. **Update or add authorization policies** in `Program.cs` to include the new role where needed.

3. **Update the role validation** in `UsersController.cs` → `NormalizeRole()` method.

4. **Update the frontend** `src/auth/roles.ts` and any `allowedRoles` arrays in `App.tsx`.

5. **Update seeding** in `Program.cs` → `SeedDefaultUsersAsync()` if the new role needs a default user in development.

---

## 4. Rotating Secrets

### JWT Secret rotation

1. Generate a new secret (at least 32 bytes, base64-encoded recommended).
2. Update `JWT__SECRET` in:
   - **Azure App Service** → Configuration → Application Settings
   - **GitHub Actions Secrets** → `JWT__SECRET`
3. All existing JWT tokens will be **immediately invalidated** — users will need to log in again.
4. No code changes required.

### Database password rotation

1. Change the password in **Azure Database for MySQL** → Server Parameters / Users.
2. Update `ConnectionStrings__DefaultConnection` (or `DB__PASSWORD`) in Azure App Service → Configuration.
3. Restart the Azure App Service.
4. Verify `/api/health` returns `"status":"healthy"` after restart.

---

## 5. Updating Dependencies

### Backend (.NET)

```bash
cd backend

# Check for outdated packages
dotnet list package --outdated

# Update a specific package
dotnet add HSMS.API package Microsoft.AspNetCore.Authentication.JwtBearer --version X.Y.Z

# Restore and verify build
dotnet restore HSMS.sln
dotnet build HSMS.sln -c Release
dotnet test HSMS.Tests/HSMS.Tests.csproj -c Release
```

### Frontend (Node.js)

```bash
cd frontend/HWSMS_UI

# Check for updates
npm outdated

# Update all to latest within semver ranges
npm update

# Update a specific package
npm install react@latest

# Verify tests pass
npm test
npm run build
```

---

## 6. Environment Variable Changes

When adding, renaming, or removing a configuration key:

1. Update `CheckEnvironmentVariables()` in `Program.cs` if it is a mandatory key.
2. Update `appsettings.json` with the new key (empty value as placeholder).
3. Update `appsettings.Development.json` with a local development value.
4. Update the `.github/workflows/ci-cd.yml` `env:` block if needed for CI.
5. Update Azure App Service → Configuration with the production value.
6. Update documentation:
   - [`ENVIRONMENT_VARIABLES_SUMMARY.md`](../ENVIRONMENT_VARIABLES_SUMMARY.md)
   - [`ENV_VARIABLES_CHECKLIST.md`](../ENV_VARIABLES_CHECKLIST.md)
   - [`backend_overview.md`](./backend_overview.md) — Configuration Reference table

---

## 7. Monitoring and Health Checks

### Health endpoint

```
GET /api/health
```

Returns:
```json
{
  "status": "healthy",
  "timestamp": "2026-07-03T00:00:00Z",
  "checks": {
    "mysql": {
      "status": "healthy",
      "description": "Database connection is available."
    }
  }
}
```

Returns HTTP `200` when healthy, `503` when unhealthy.

### Diagnosing a failed health check

| Symptom | Likely cause | Action |
|---|---|---|
| `"status":"unhealthy"` with mysql error | DB connection string wrong / DB server down | Check Azure DB status, verify env vars in App Service |
| API returns `500` on startup | Missing mandatory environment variable | Check App Service logs → Application Insights |
| Frontend shows "Backend unavailable" banner | `VITE_API_BASE_URL` points to wrong URL | Rebuild frontend with correct env var |
| `401 Unauthorized` on all requests | Mismatched JWT issuer/audience/secret between frontend build and backend runtime | Verify `JWT__ISSUER`, `JWT__AUDIENCE`, `JWT__SECRET` match |

---

## 8. Database Backup and Restore

Azure Database for MySQL Flexible Server provides **automated backups** (point-in-time restore).
For manual backup:

```bash
# Backup
mysqldump -h <azure-host> -u <user> -p<password> \
  --ssl-mode=REQUIRED \
  CSP_HSMS > backup_$(date +%Y%m%d).sql

# Restore to a new database
mysql -h <azure-host> -u <user> -p<password> \
  --ssl-mode=REQUIRED \
  CSP_HSMS < backup_20260703.sql
```

> After restoring, restart the Azure App Service to allow `DatabaseInitializer` to verify
> schema integrity on startup.

---

## 9. Resetting and Re-seeding the Development Database

Use the provided Node.js script:

```bash
# From the project root
node backend/reset-and-seed-db.js
```

This script drops and recreates all tables and populates them with realistic sample data
for development and demo purposes.

> ⚠️ **Never run this against the production database.**

---

## 10. Generating Test Artifacts

Postman collection and JMeter test plan are generated from scripts — do not edit them manually.

```bash
# Regenerate Postman collection
node scripts/generate-postman-collection.js

# Regenerate JMeter test plan
node scripts/generate-jmeter-test-plan.js
```

After regenerating, commit the updated files in `postman/` and `jmeter/`.
