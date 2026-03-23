# HWSMS - Quick Start / Quick Reference

## 🚀 Local Development (5 minutes)

### Backend
```bash
cd backend
cp .env.example .env.development
# Make sure MySQL is running on localhost:3306
dotnet run
# API available at: http://localhost:5162
```

### Frontend
```bash
cd frontend/HWSMS_UI
cp .env.example .env.development
npm install
npm run dev
# UI available at: http://localhost:5173
```

**Default Credentials:**
- Username: `admin` | Password: `Admin@123`
- Username: `manager` | Password: `Manager@123`
- Username: `cashier` | Password: `Cashier@123`

---

## 🐳 Local Development with Docker

```bash
# Copy Docker override template
cp docker-compose.override.yml.example docker-compose.override.yml

# Start application stack
docker-compose up -d

# View logs
docker-compose logs -f backend

# Access services
# Frontend: http://localhost:3000
# Backend: http://localhost:5000
# API Docs: http://localhost:5000/swagger
```

---

## ☁️ Production Deployment

### 1. Prepare Environment Variables

```bash
# Generate a strong JWT secret
openssl rand -base64 48

# Set environment variables
export DB_SERVER=your-db-host.com
export DB_NAME=hwsms_prod
export DB_USER=hwsms_user
export DB_PASSWORD=<secure_password>
export JWT_SECRET=<generated_secret>
export CORS_ORIGINS=https://yourdomain.com
export FRONTEND_API_URL=https://api.yourdomain.com
```

### 2. Backend Deployment

```bash
cd backend
dotnet publish -c Release -o ./publish
cd publish
# Start with environment variables
ASPNETCORE_ENVIRONMENT=Production dotnet HSMS.API.dll
```

### 3. Frontend Deployment

```bash
cd frontend/HWSMS_UI
npm install
npm run build
# Copy dist/ folder to web server
```

### 4. Docker Deployment

```bash
# Set all environment variables (see Production Deployment section in DEPLOYMENT_GUIDE.md)

# Deploy
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

---

## 📋 Environment Variables by Scenario

### Development (`.env.development`)
```env
# Already pre-configured for local development
DB_SERVER=localhost
JWT_SECRET=dev-secret-key
CORS_ORIGINS=http://localhost:5173
SEED_DEFAULT_USERS=true
```

### Production
```env
DB_SERVER=<production-db-host>
DB_PASSWORD=<strong-password>
JWT_SECRET=<random-64-char-secret>
CORS_ORIGINS=https://yourdomain.com
SEED_DEFAULT_USERS=false
ASPNETCORE_ENVIRONMENT=Production
```

---

## 🔍 Verify Deployment

```bash
# Check backend is running
curl http://your-api.com/swagger

# Check frontend can reach backend
# Open browser and test login

# Check logs
docker-compose logs backend
# Or
journalctl -u hwsms-api -f
```

---

## ⚠️ Common Issues

| Issue | Solution |
|-------|----------|
| Frontend can't connect to backend | Check `CORS_ORIGINS` includes frontend URL |
| API returns 401 | Check `JWT_SECRET` is same on backend |
| Database connection fails | Check `DB_SERVER`, `DB_USER`, `DB_PASSWORD` |
| Default users not created | Check `SEED_DEFAULT_USERS=true` and database connection |

---

## 📚 Full Documentation

See `DEPLOYMENT_GUIDE.md` for comprehensive deployment guide with:
- Detailed environment variable reference
- Systemd service setup
- Nginx configuration
- Kubernetes examples
- Security best practices

---

## 📦 Files Reference

| File | Purpose | Action |
|------|---------|--------|
| `.env.example` | Template (both backend & frontend) | Copy to `.env.development` |
| `.env.development` | Local development defaults | Pre-configured, don't commit |
| `.env.production` | Production defaults (frontend only) | Update before deployment |
| `docker-compose.override.yml.example` | Docker local dev setup | Copy to `docker-compose.override.yml` |
| `docker-compose.prod.yml` | Docker production setup | Use with docker-compose.yml |
| `DEPLOYMENT_GUIDE.md` | Full deployment documentation | Reference during deployment |

---

## 🔐 Security Checklist

Before production:
- [ ] Generate new JWT secret
- [ ] Set strong database password
- [ ] Configure CORS for production domain only
- [ ] Set `SEED_DEFAULT_USERS=false`
- [ ] Set `ASPNETCORE_ENVIRONMENT=Production`
- [ ] Enable HTTPS/SSL
- [ ] Remove default credentials

---

Quick questions? Most answers are in `DEPLOYMENT_GUIDE.md` ✅
