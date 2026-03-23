# Environment Variables - Deployment Checklist

Use this checklist when deploying to ensure all environment variables are properly configured.

## Pre-Deployment Review

- [ ] All files have been committed to git (check `git status`)
- [ ] `.env` files are NOT in git (verify with `git ls-files | grep .env`)
- [ ] `.env.example` files ARE in git
- [ ] `docker-compose.override.yml.example` is in git
- [ ] DEPLOYMENT_GUIDE.md and QUICK_START.md are in git

## Backend Environment Variables

### Database Configuration
- [ ] `DB_SERVER` - Set to database host (not "localhost" for remote DB)
- [ ] `DB_PORT` - Set if not default 3306
- [ ] `DB_NAME` - Database name created
- [ ] `DB_USER` - Dedicated database user created
- [ ] `DB_PASSWORD` - Strong password set (18+ chars)

**Quick Test:**
```bash
mysql -h $DB_SERVER -u $DB_USER -p$DB_PASSWORD -e "SELECT 1"
```

### JWT Configuration
- [ ] `JWT_SECRET` - Generated secret (64+ characters, random)
- [ ] `JWT_ISSUER` - Set to "HSMS.API" or your issuer
- [ ] `JWT_AUDIENCE` - Set to "HSMS.Client" or your audience
- [ ] `JWT_EXPIRY_MINUTES` - Token expiry time (default: 60)

**Secret Generation Command:**
```bash
# Choose one method:
openssl rand -base64 48          # Use this result with JWT_SECRET
python3 -c "import secrets; print(secrets.token_urlsafe(64))"
dotnet user-jwts create --display-name "Production Key"
```

### CORS Configuration
- [ ] `CORS_ORIGINS` - Includes all frontend URLs (comma-separated, no trailing slash)
  - Development: `http://localhost:5173`
  - Production: `https://yourdomain.com,https://www.yourdomain.com`

**Test CORS:**
```bash
curl -X OPTIONS http://your-api.com/api/Auth/Login \
  -H "Origin: https://yourdomain.com" \
  -H "Access-Control-Request-Method: POST"
```

### Server Configuration
- [ ] `ASPNETCORE_ENVIRONMENT` - Set to `Production` for production
- [ ] `ASPNETCORE_URLS` - Listening URL (e.g., `http://+:80` for Docker)

### Seed Data Configuration
- [ ] `SEED_DEFAULT_USERS` - Set to `false` for production
- [ ] If `SEED_DEFAULT_USERS=true`:
  - [ ] `ADMIN_PASSWORD` - Strong password set
  - [ ] `MANAGER_PASSWORD` - Strong password set
  - [ ] `CASHIER_PASSWORD` - Strong password set

**Note:** Always disable seeding (`SEED_DEFAULT_USERS=false`) in production

## Frontend Environment Variables

### Vite Configuration
- [ ] `VITE_API_BASE_URL` - Points to backend API URL
  - Development: `http://localhost:5162`
  - Docker: `http://backend:80`
  - Production: `https://api.yourdomain.com` (no trailing slash!)

- [ ] `VITE_DEBUG` - Set to `false` for production

**Test Frontend API Connection:**
```bash
# In browser console on frontend:
fetch('http://your-api.com/api/Auth/Login').then(r => console.log(r.status))
```

## Docker Deployment Checklist

### For docker-compose.override.yml (Local Development)
- [ ] Copied from `docker-compose.override.yml.example`
- [ ] File is NOT in git (verify in .gitignore)
- [ ] All database credentials are set
- [ ] CORS_ORIGINS includes local URLs

### For docker-compose.prod.yml (Production Deployment)
- [ ] All required environment variables are set before running
- [ ] Database URLs point to production database
- [ ] JWT_SECRET is production secret
- [ ] SEED_DEFAULT_USERS is set to false
- [ ] CORS_ORIGINS is production URL only

**Deploy with:**
```bash
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

## Systemd Service Configuration (Linux)

If running as systemd service:
- [ ] Service file created: `/etc/systemd/system/hwsms-api.service`
- [ ] `EnvironmentFile=/etc/hwsms/.env.production` is set
- [ ] File permissions are restrictive: `chmod 600 /etc/hwsms/.env.production`

**Test Service:**
```bash
sudo systemctl restart hwsms-api
sudo journalctl -u hwsms-api -f
```

## File Permissions

```bash
# Backend .env files
chmod 600 backend/.env.production
chmod 644 backend/.env.example

# Frontend .env files
chmod 600 frontend/HWSMS_UI/.env.production
chmod 644 frontend/HWSMS_UI/.env.example

# Docker files
chmod 600 docker-compose.override.yml
chmod 644 docker-compose.override.yml.example
```

## Verification Steps

### 1. Database Connection
```bash
# Test database is accessible
mysql -h $DB_SERVER -u $DB_USER -p$DB_PASSWORD -e "USE $DB_NAME; SELECT 1;"
```

### 2. Backend Health
```bash
# Test backend is responding
curl -s http://your-api.com/swagger | head -20

# Or check with Docker
docker-compose logs backend | tail -20
```

### 3. Frontend Build
```bash
# Check production build succeeds
cd frontend/HWSMS_UI
npm run build
# Verify dist folder is created with content
ls -lah dist/
```

### 4. API Connection Test
```bash
# Test authentication endpoint
curl -X POST http://your-api.com/api/Auth/Login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Admin@123"}'
```

### 5. CORS Test
```bash
# Test CORS headers from frontend domain
curl -I -X OPTIONS http://your-api.com/api/Auth/Login \
  -H "Origin: https://yourdomain.com" \
  -H "Access-Control-Request-Method: POST"

# Should include:
# Access-Control-Allow-Origin: https://yourdomain.com
```

## Production Deployment Timeline

1. **Day Before:**
   - [ ] Generate all secrets
   - [ ] Test database backup
   - [ ] Review all env var settings
   - [ ] Run through verification steps

2. **Deployment Day:**
   - [ ] Set all environment variables
   - [ ] Deploy backend
   - [ ] Run health checks
   - [ ] Deploy frontend
   - [ ] Test end-to-end flow
   - [ ] Monitor logs for 30 minutes

3. **Post-Deployment:**
   - [ ] Document any custom settings
   - [ ] Update team on access credentials
   - [ ] Set up monitoring/alerting
   - [ ] Schedule secret rotation (90 days)

## Common Environment Variable Mistakes

❌ **DON'T:**
- Use default passwords in production
- Commit `.env` files to git
- Use same secret for multiple environments
- Hard-code database passwords in code
- Forget the trailing "/" in CORS_ORIGINS
- Mix development and production values
- Share secrets via email/Slack

✅ **DO:**
- Use `.env.example` as template
- Generate strong random secrets
- Use different secrets per environment
- Store secrets in secure management system
- Use comma-separated list for CORS_ORIGINS
- Verify all values before deployment
- Use 1Password, LastPass, or AWS Secrets Manager

## Emergency Reset

If you need to reset deployment configuration:

```bash
# Clear all environment variables (bash)
unset DB_SERVER DB_NAME DB_USER DB_PASSWORD JWT_SECRET CORS_ORIGINS

# Reload from file
source .env.production
```

## Need Help?

- **Deployment issues?** → See `DEPLOYMENT_GUIDE.md`
- **Quick overview?** → See `QUICK_START.md`
- **Stuck?** → Check git logs for previous deployments
- **Security concerns?** → Review "Security Best Practices" in `DEPLOYMENT_GUIDE.md`

---

**Last Updated:** March 16, 2026
**Maintained By:** Development Team
