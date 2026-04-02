# HWSMS - Environment Variables Configuration Index

> **Status:** ✅ Complete and Ready for Deployment
>
> **Last Updated:** March 16, 2026
>
> **Key Achievement:** All hardcoded secrets removed. Application now uses proper environment variables for every environment (Development, Staging, Production).

---

## 📖 Documentation Index

### For Developers Getting Started
Start here if you're new to the project:
- **[QUICK_START.md](QUICK_START.md)** ⭐ START HERE
  - 5-minute local development setup
  - Quick reference for common issues
  - Production deployment overview

### For Production Deployment
Use this before deploying to any server:
- **[ENV_VARIABLES_CHECKLIST.md](ENV_VARIABLES_CHECKLIST.md)** ⭐ BEFORE DEPLOYING
  - Pre-deployment verification checklist
  - Step-by-step health checks
  - File permissions and security setup
  - Emergency procedures

### For Comprehensive Information
Reference this for detailed guidance:
- **[DEPLOYMENT_GUIDE.md](DEPLOYMENT_GUIDE.md)** 📚 COMPLETE REFERENCE
  - Full environment variable reference
  - Systemd service setup
  - Nginx configuration
  - Kubernetes examples
  - Troubleshooting guide
  - Security best practices

### For Understanding Changes
See what was modified:
- **[ENVIRONMENT_VARIABLES_SUMMARY.md](ENVIRONMENT_VARIABLES_SUMMARY.md)** 📊 WHAT CHANGED
  - Overview of all modifications
  - Migration guide for existing deployments
  - Configuration priority explanation
  - Benefits summary

---

## 🗂️ Configuration Files Reference

### Backend Configuration Files

| File | Purpose | Location | Action |
|------|---------|----------|--------|
| `.env.example` | Template showing all available variables | `backend/` | Reference |
| `.env.development` | Pre-configured for local development | `backend/` | Use as-is for local dev |
| `.env.production` | `N/A - See DEPLOYMENT_GUIDE.md` | N/A | Create based on guide |

### Frontend Configuration Files

| File | Purpose | Location | Action |
|------|---------|----------|--------|
| `.env.example` | Template showing all available variables | `frontend/HWSMS_UI/` | Reference |
| `.env.development` | Pre-configured for local development | `frontend/HWSMS_UI/` | Use as-is for local dev |
| `.env.production` | Template for production deployment | `frontend/HWSMS_UI/` | Customize for production |

### Docker Configuration Files

| File | Purpose | Location | Action |
|------|---------|----------|--------|
| `docker-compose.override.yml.example` | Template for local Docker development | Root | Copy to `docker-compose.override.yml` |
| `docker-compose.prod.yml` | Production Docker setup with env var support | Root | Use with docker-compose.yml |

---

## 🔑 Essential Environment Variables

### Absolutely Critical (Security)
```env
# Database Credentials
DB_PASSWORD=<strong_secure_password>

# JWT Secret (generate a random 64+ character string)
JWT_SECRET=<generated_secret_key>

# CORS Origins (your frontend URL)
CORS_ORIGINS=https://yourdomain.com
```

### Environment-Specific
```env
# Development
ASPNETCORE_ENVIRONMENT=Development
SEED_DEFAULT_USERS=true

# Production
ASPNETCORE_ENVIRONMENT=Production
SEED_DEFAULT_USERS=false
```

For complete list, see **backend/.env.example**

---

## 🚀 Quick Commands

### Local Development Setup
```bash
# Backend
cd backend
cp .env.example .env.development
dotnet run

# Frontend (in new terminal)
cd frontend/HWSMS_UI
cp .env.example .env.development
npm install && npm run dev
```

### Docker Local Development
```bash
cp docker-compose.override.yml.example docker-compose.override.yml
docker-compose up -d
```

### Production Deployment
See **ENV_VARIABLES_CHECKLIST.md** for step-by-step instructions

---

## 📋 Pre-Deployment Checklist

Before deploying to **any** environment:

- [ ] All `.env` files are created (not committed to git)
- [ ] All `.env.example` files are present (committed to git)
- [ ] Database credentials are strong and unique per environment
- [ ] JWT_SECRET is randomly generated (64+ characters)
- [ ] CORS_ORIGINS includes your actual frontend URL
- [ ] SEED_DEFAULT_USERS is disabled (`false`) in production
- [ ] All required variables are set (see ENV_VARIABLES_CHECKLIST.md)
- [ ] Application starts without errors
- [ ] Frontend can connect to backend API

For detailed verification steps, see **ENV_VARIABLES_CHECKLIST.md**

---

## 🔒 Security Highlights

✅ **What's Better Now:**
- ✅ No secrets in version control
- ✅ Different secrets per environment
- ✅ Easy credential rotation
- ✅ Supports secret management tools (Vault, AWS Secrets Manager, etc.)
- ✅ Production-safe by default
- ✅ CORS configured per deployment

❌ **What Was Removed:**
- ❌ Hardcoded database password in appsettings.json
- ❌ Hardcoded JWT secret in code
- ❌ Hard-coded CORS origins
- ❌ Default user passwords in source code

---

## 📚 Documentation Structure

```
HWSMS Root
├── QUICK_START.md                        ← START HERE (developers)
├── ENV_VARIABLES_CHECKLIST.md            ← BEFORE DEPLOYING (ops/devops)
├── DEPLOYMENT_GUIDE.md                   ← COMPLETE REFERENCE (all teams)
├── ENVIRONMENT_VARIABLES_SUMMARY.md      ← UNDERSTANDING CHANGES
├── CONFIGURATION_INDEX.md                ← THIS FILE
│
├── backend/
│   ├── .env.example                      ← Backend variables template
│   ├── .env.development                  ← Backend dev defaults
│   └── HSMS.API/
│       ├── Program.cs                    ✓ Uses env variables
│       └── appsettings.json              ✓ No hardcoded secrets
│
├── frontend/HWSMS_UI/
│   ├── .env.example                      ← Frontend variables template
│   ├── .env.development                  ← Frontend dev defaults
│   ├── .env.production                   ← Frontend prod template
│   └── src/services/
│       └── *.ts                          ✓ Uses VITE_API_BASE_URL
│
└── docker-compose.*.yml                  ✓ Uses env variables
```

---

## 🔄 Workflow Examples

### Scenario 1: New Developer (Local Development)
```bash
# 1. Clone repository
git clone <repo>
cd CSP_HWSMS

# 2. Copy templates (already have defaults)
cd backend && cp .env.example .env.development
cd ../frontend/HWSMS_UI && cp .env.example .env.development

# 3. Start developing
# Backend: dotnet run (uses .env.development)
# Frontend: npm run dev (uses .env.development)
```

### Scenario 2: Deploying to Staging
```bash
# 1. Prepare environment variables
export DB_SERVER=staging-db.internal
export DB_PASSWORD=staging_secure_password
export JWT_SECRET=<generate_new_secret>
export CORS_ORIGINS=https://staging.yourdomain.com

# 2. Deploy
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d

# 3. Verify
curl -s https://staging-api.yourdomain.com/swagger | head -20
```

### Scenario 3: Deploying to Production
See **ENV_VARIABLES_CHECKLIST.md** for complete step-by-step guide

---

## ⚡ Performance Impact

**No Performance Changes:**
- Environment variable reading happens once at startup
- No runtime overhead
- No API calls to fetch config
- Same performance as hardcoded values

---

## 🆘 Troubleshooting

### Common Issues

| Issue | Solution | Documentation |
|-------|----------|-----------------|
| App won't start | Missing env vars | ENV_VARIABLES_CHECKLIST.md |
| Frontend can't connect | Wrong CORS_ORIGINS | DEPLOYMENT_GUIDE.md |
| Database connection fails | Wrong connection string | ENV_VARIABLES_CHECKLIST.md |
| Default users not created | SEED_DEFAULT_USERS not set | QUICK_START.md |
| JWT authentication fails | JWT_SECRET mismatch | DEPLOYMENT_GUIDE.md |

For detailed troubleshooting, see **DEPLOYMENT_GUIDE.md → Troubleshooting** section

---

## 🔗 Quick Links

- 📖 [QUICK_START.md](QUICK_START.md) - Get running in 5 minutes
- ✓ [ENV_VARIABLES_CHECKLIST.md](ENV_VARIABLES_CHECKLIST.md) - Pre-deployment checklist
- 📚 [DEPLOYMENT_GUIDE.md](DEPLOYMENT_GUIDE.md) - Complete deployment reference
- 📊 [ENVIRONMENT_VARIABLES_SUMMARY.md](ENVIRONMENT_VARIABLES_SUMMARY.md) - What changed
- 🔧 [backend/.env.example](backend/.env.example) - Backend variables
- 🔧 [frontend/HWSMS_UI/.env.example](frontend/HWSMS_UI/.env.example) - Frontend variables

---

## 📞 Questions?

- **Quick setup questions?** → See QUICK_START.md
- **Deployment questions?** → See DEPLOYMENT_GUIDE.md
- **Pre-deployment verification?** → See ENV_VARIABLES_CHECKLIST.md
- **Understanding what changed?** → See ENVIRONMENT_VARIABLES_SUMMARY.md

---

## ✅ Implementation Status

- [x] Backend environment variables
- [x] Frontend environment variables
- [x] Docker configuration
- [x] Development setup guides
- [x] Production deployment guides
- [x] Security best practices
- [x] Troubleshooting documentation
- [x] Pre-deployment checklists
- [x] Code modifications (Program.cs, appsettings.json)
- [x] Git ignore configuration

**Status: Ready for Production Deployment** 🚀

---

**Last Updated:** March 16, 2026
**Maintained By:** Development Team
**Version:** 1.0.0
