# Environment Variables Implementation - Summary of Changes

**Date:** March 16, 2026
**Status:** ✅ Complete
**Impact:** Production-ready environment variable management for easy deployment

---

## Overview

The HWSMS application has been updated to use proper environment variables for all configuration settings. This makes the application:
- **Easy to deploy** across different environments (development, staging, production)
- **Secure** (no hardcoded secrets in version control)
- **Flexible** (no code changes needed for different deployments)
- **Professional** (follows industry best practices)

---

## Files Created

### Backend Configuration Files

| File | Purpose | Location |
|------|---------|----------|
| `.env.example` | Template with all available env vars | `backend/` |
| `.env.development` | Development defaults (pre-configured) | `backend/` |
| `DEPLOYMENT_GUIDE.md` | Comprehensive deployment guide | Root |
| `QUICK_START.md` | Quick reference guide | Root |
| `ENV_VARIABLES_CHECKLIST.md` | Pre-deployment checklist | Root |

### Frontend Configuration Files

| File | Purpose | Location |
|------|---------|----------|
| `.env.example` | Template with all available env vars | `frontend/HWSMS_UI/` |
| `.env.development` | Development defaults (pre-configured) | `frontend/HWSMS_UI/` |
| `.env.production` | Production defaults (template) | `frontend/HWSMS_UI/` |

### Docker Configuration Files

| File | Purpose | Location |
|------|---------|----------|
| `docker-compose.override.yml.example` | Docker local dev setup template | Root |
| `docker-compose.prod.yml` | Docker production setup | Root |

---

## Files Modified

### Backend

#### `/backend/HSMS.API/Program.cs` - Enhanced Configuration
**Changes:**
- Added environment-specific configuration loading
- Changed CORS configuration to use `CORS_ORIGINS` environment variable
- Modified seed users function to accept `IConfiguration` parameter
- Updated seed users to read passwords from environment variables
- Added logic to disable seed users in production

**Key Updates:**
```c#
// Before: policy.WithOrigins("http://localhost:5173")
// After: Reads from CORS_ORIGINS environment variable

// Before: Hardcoded default passwords
// After: Reads from ADMIN_PASSWORD, MANAGER_PASSWORD, CASHIER_PASSWORD env vars
```

#### `/backend/HSMS.API/appsettings.json` - Placeholder Values
**Changes:**
- Removed hardcoded database credentials
- Removed hardcoded JWT secret
- Replaced with placeholder values that will be overridden by environment variables

**Note:** ASP.NET Core automatically uses environment variables (with name mapping) to override configuration values.

### Frontend

#### `frontend/HWSMS_UI/.env.example` - Updated Template
**Changes:**
- Enhanced documentation with development and production examples
- Added notes about URL formatting

### GitIgnore

#### `/.gitignore` - Updated Docker Section
**Changes:**
- Added proper exclusion for local docker-compose.override.yml
- Added exception to allow docker-compose.override.yml.example to be tracked

---

## Environment Variables Blueprint

### Backend - Database Configuration
| Variable | Example Value | Notes |
|----------|--------------|-------|
| `DB_SERVER` | `localhost` | MySQL server hostname |
| `DB_PORT` | `3306` | Optional, defaults to 3306 |
| `DB_NAME` | `CSP_HSMS` | Database name |
| `DB_USER` | `root` | Database user |
| `DB_PASSWORD` | `secure_pass_123` | Database password |

### Backend - JWT Configuration
| Variable | Example Value | Notes |
|----------|--------------|-------|
| `JWT_SECRET` | `<64-char-random-string>` | **CRITICAL** - Generate strong random secret |
| `JWT_ISSUER` | `HSMS.API` | JWT token issuer |
| `JWT_AUDIENCE` | `HSMS.Client` | JWT token audience |
| `JWT_EXPIRY_MINUTES` | `60` | Token expiration time |

### Backend - CORS Configuration
| Variable | Example Value | Notes |
|----------|--------------|-------|
| `CORS_ORIGINS` | `http://localhost:5173` | Comma-separated list of allowed origins |

### Backend - Server Configuration
| Variable | Example Value | Notes |
|----------|--------------|-------|
| `ASPNETCORE_ENVIRONMENT` | `Development` | Environment name |
| `ASPNETCORE_URLS` | `http://localhost:5162` | Server listening URLs |

### Backend - Seed Data Configuration
| Variable | Example Value | Notes |
|----------|--------------|-------|
| `SEED_DEFAULT_USERS` | `true` | Enable/disable default user creation |
| `ADMIN_PASSWORD` | `Admin@123` | Default admin password |
| `MANAGER_PASSWORD` | `Manager@123` | Default manager password |
| `CASHIER_PASSWORD` | `Cashier@123` | Default cashier password |

### Frontend Configuration
| Variable | Example Value | Notes |
|----------|--------------|-------|
| `VITE_API_BASE_URL` | `http://localhost:5162` | Backend API base URL |
| `VITE_DEBUG` | `false` | Enable debug logging |

---

## Deployment Scenarios

### Scenario 1: Local Development
```bash
# Setup
cd backend && cp .env.example .env.development
cd frontend/HWSMS_UI && cp .env.example .env.development

# Run
dotnet run  # Backend uses .env.development automatically
npm run dev # Frontend uses .env.development automatically
```

### Scenario 2: Docker Local Development
```bash
# Setup
cp docker-compose.override.yml.example docker-compose.override.yml

# Run
docker-compose up -d

# Pre-configured with:
# - Database: localhost with credentials
# - Backend: port 5000
# - Frontend: port 3000
```

### Scenario 3: Production on Server
```bash
# Before deployment
export DB_SERVER=prod-db.com
export DB_PASSWORD=<strong_password>
export JWT_SECRET=<generated_secret>
export CORS_ORIGINS=https://yourdomain.com
export ASPNETCORE_ENVIRONMENT=Production
export SEED_DEFAULT_USERS=false

# Deploy
dotnet publish -c Release
cd publish && dotnet HSMS.API.dll
```

### Scenario 4: Docker Production
```bash
# All environment variables set (see ENV_VARIABLES_CHECKLIST.md)

# Deploy
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

---

## Security Improvements

### Before (Hardcoded)
```
❌ Secrets visible in source code
❌ Hardcoded CORS origins
❌ Default passwords in code
❌ Database credentials in appsettings
❌ Hard to change for different environments
```

### After (Environment Variables)
```
✅ All secrets externalized
✅ CORS origins configurable per deployment
✅ Default passwords controlled via env vars
✅ Database credentials never in code
✅ Easy to change for any environment
✅ Different secrets per environment supported
```

---

## Configuration Priority (How Values Are Used)

ASP.NET Core loads configuration with this priority (highest to lowest):
1. **Environment Variables** ← Highest priority
2. `appsettings.production.json`
3. `appsettings.json`
4. Code defaults

**Example:**
```
Environment Variable: JWT_SECRET="env-secret"
appsettings.json:     "Secret": "json-secret"
Result: Uses "env-secret" (environment wins!)
```

---

## How to Use Environment Variables

### Method 1: Export in Shell
```bash
export JWT_SECRET="your-secret"
export DB_PASSWORD="your-password"
dotnet run
```

### Method 2: .env File (Development)
```bash
# .env.development (auto-loaded by IDE/framework)
JWT_SECRET=dev-secret
DB_PASSWORD=dev-password
```

### Method 3: Docker Environment
```bash
docker run -e JWT_SECRET="secret" -e DB_PASSWORD="pass" ...
```

### Method 4: Systemd Service
```ini
# /etc/systemd/system/hwsms.service
EnvironmentFile=/etc/hwsms/.env.production
```

### Method 5: CI/CD Pipeline
```yaml
# GitHub Actions, GitLab CI, etc.
env:
  JWT_SECRET: ${{ secrets.JWT_SECRET }}
  DB_PASSWORD: ${{ secrets.DB_PASSWORD }}
```

---

## Migration Guide (For Existing Deployments)

If you have an existing deployment, follow these steps:

### Step 1: Update Code
```bash
# Pull latest changes
git pull origin Gowrishan
```

### Step 2: Create Environment Files
```bash
# Backend
cd backend
cp .env.example .env.production
# Edit .env.production with your production values

# Frontend
cd ../frontend/HWSMS_UI
cp .env.example .env.production
# Edit .env.production with your production values
```

### Step 3: Verify Configuration
```bash
# Backend
echo $JWT_SECRET  # Should show your secret

# Frontend
cat .env.production | grep VITE_API_BASE_URL
```

### Step 4: Deploy
```bash
# No code changes needed, just restart with new config
dotnet run  # Or docker-compose up -d
```

---

## Troubleshooting

### Environment Variables Not Being Read

**Problem:** Application still using hardcoded values
**Solution:**
```bash
# 1. Verify variable is set
echo $JWT_SECRET  # Should not be empty

# 2. Check in-app (add logging)
var jwtSecret = builder.Configuration["Jwt:Secret"];
Console.WriteLine($"JWT Secret loaded: {jwtSecret}");

# 3. Verify naming convention
# JSON key: Jwt:Secret → Env var: Jwt__Secret or JWT_SECRET
```

### CORS Issues

**Problem:** Frontend getting CORS error
**Solution:**
```bash
# 1. Check CORS_ORIGINS includes frontend URL
echo $CORS_ORIGINS

# 2. Verify no trailing slashes
# ✅ CORS_ORIGINS=http://localhost:5173
# ❌ CORS_ORIGINS=http://localhost:5173/

# 3. Restart backend with new setting
```

### Database Connection Failed

**Problem:** Can't connect to database
**Solution:**
```bash
# 1. Test credentials
mysql -h $DB_SERVER -u $DB_USER -p$DB_PASSWORD

# 2. Check all vars are set
echo "Server: $DB_SERVER"
echo "Database: $DB_NAME"
echo "User: $DB_USER"
echo "Password: $DB_PASSWORD"

# 3. Verify connection string format
# Should be: server=host;database=name;user=user;password=pass;
```

---

## Documentation Files

For more information, see:

1. **DEPLOYMENT_GUIDE.md** - Comprehensive deployment guide
   - Systemd service setup
   - Nginx configuration
   - Kubernetes examples
   - Security best practices
   - Detailed troubleshooting

2. **QUICK_START.md** - Quick reference
   - 5-minute local setup
   - Quick production deployment steps
   - Common issues table

3. **ENV_VARIABLES_CHECKLIST.md** - Pre-deployment checklist
   - Step-by-step verification
   - Health check commands
   - File permissions
   - Emergency reset procedures

---

## Summary of Benefits

| Benefit | Before | After |
|---------|--------|-------|
| Secrets in source control | ✅ Yes (BAD) | ❌ No (GOOD) |
| Change config without code | ❌ No | ✅ Yes |
| Multiple environment support | ❌ Hard | ✅ Easy |
| Production-ready | ❌ No | ✅ Yes |
| Deployment documentation | ❌ Missing | ✅ Complete |
| Security compliance | ❌ Low | ✅ High |
| Developer experience | ⚠️ Confusing | ✅ Clear |
| CI/CD pipeline ready | ❌ No | ✅ Yes |

---

## Next Steps

1. ✅ Review this summary
2. ✅ Read QUICK_START.md for local development
3. ✅ Use ENV_VARIABLES_CHECKLIST.md before deploying
4. ✅ Reference DEPLOYMENT_GUIDE.md for specific deployment scenarios

---

**Questions?**
- For local development → See QUICK_START.md
- For production deployment → See DEPLOYMENT_GUIDE.md
- For pre-deployment verification → See ENV_VARIABLES_CHECKLIST.md
