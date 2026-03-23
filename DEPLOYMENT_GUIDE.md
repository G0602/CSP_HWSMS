# HWSMS - Environment Variables & Deployment Guide

This guide explains how to configure and deploy the Hardware Store Hardware Sales Management System (HWSMS) with proper environment variables for different environments.

## Table of Contents
1. [Local Development Setup](#local-development-setup)
2. [Environment Variables Reference](#environment-variables-reference)
3. [Backend Deployment](#backend-deployment)
4. [Frontend Deployment](#frontend-deployment)
5. [Docker Deployment](#docker-deployment)
6. [Security Best Practices](#security-best-practices)

---

## Local Development Setup

### Backend Setup

1. **Copy the environment template:**
   ```bash
   cd backend
   cp .env.example .env.development
   ```

2. **Edit `.env.development` with your local database credentials:**
   ```bash
   # Edit the file with your values
   vim .env.development
   ```

3. **Before running the application:**
   - Ensure MySQL is running on `localhost:3306`
   - The `.env.development` file will be auto-loaded by the application
   - Default seed users will be created automatically

4. **Run the application:**
   ```bash
   cd HSMS.API
   dotnet run
   ```

### Frontend Setup

1. **Copy the environment file:**
   ```bash
   cd frontend/HWSMS_UI
   cp .env.example .env.development
   ```

2. **The `.env.development` file is pre-configured for local development:**
   ```
   VITE_API_BASE_URL=http://localhost:5162
   VITE_DEBUG=true
   ```

3. **Install and run:**
   ```bash
   npm install
   npm run dev
   ```

The frontend will be available at `http://localhost:5173`

---

## Environment Variables Reference

### Backend Environment Variables

#### Database Configuration
| Variable | Description | Example | Required |
|----------|-------------|---------|----------|
| `DB_SERVER` | MySQL server host | `localhost` or `db` (Docker) | Yes |
| `DB_PORT` | MySQL server port | `3306` | No (default: 3306) |
| `DB_NAME` | Database name | `CSP_HSMS` | Yes |
| `DB_USER` | Database user | `root` | Yes |
| `DB_PASSWORD` | Database password | `your_secure_password` | Yes |

#### JWT Configuration
| Variable | Description | Example | Required |
|----------|-------------|---------|----------|
| `JWT_SECRET` | JWT signing secret (min 32 chars) | `your-very-long-secret-key` | Yes |
| `JWT_ISSUER` | JWT issuer claim | `HSMS.API` | No (default: HSMS.API) |
| `JWT_AUDIENCE` | JWT audience claim | `HSMS.Client` | No (default: HSMS.Client) |
| `JWT_EXPIRY_MINUTES` | Token expiration time | `60` | No (default: 60) |

#### CORS Configuration
| Variable | Description | Example | Required |
|----------|-------------|---------|----------|
| `CORS_ORIGINS` | Allowed frontend origins (comma-separated) | `http://localhost:5173,https://myapp.com` | Yes |

#### Server Configuration
| Variable | Description | Example | Required |
|----------|-------------|---------|----------|
| `ASPNETCORE_ENVIRONMENT` | Deployment environment | `Development` or `Production` | No (default: Production) |
| `ASPNETCORE_URLS` | Server URLs to listen on | `http://localhost:5162` | No |

#### Seed Data Configuration
| Variable | Description | Example | Required |
|----------|-------------|---------|----------|
| `SEED_DEFAULT_USERS` | Enable default user creation | `true` or `false` | No (default: true) |
| `ADMIN_PASSWORD` | Default admin password | `Admin@123` | No (only if SEED_DEFAULT_USERS=true) |
| `MANAGER_PASSWORD` | Default manager password | `Manager@123` | No (only if SEED_DEFAULT_USERS=true) |
| `CASHIER_PASSWORD` | Default cashier password | `Cashier@123` | No (only if SEED_DEFAULT_USERS=true) |

### Frontend Environment Variables

#### Vite Configuration
| Variable | Description | Example | Required |
|----------|-------------|---------|----------|
| `VITE_API_BASE_URL` | Backend API base URL | `http://localhost:5162` | Yes |
| `VITE_DEBUG` | Enable debug logging | `true` or `false` | No |

---

## Backend Deployment

### Linux/Windows Server Deployment

1. **Generate a secure JWT secret:**
   ```bash
   # Generate a 64-character random string (using OpenSSL)
   openssl rand -base64 48

   # Or use Python
   python3 -c "import secrets; print(secrets.token_urlsafe(64))"

   # Or use .NET CLI
   dotnet user-jwts create --display-name "Production"
   ```

2. **Create production environment file:**
   ```bash
   # Create a secure file (readable only by app user)
   sudo nano /etc/hwsms/.env.production
   chmod 600 /etc/hwsms/.env.production
   ```

3. **Set environment variables:**
   ```bash
   export DB_SERVER=db.example.com
   export DB_NAME=hwsms_prod
   export DB_USER=hwsms_app
   export DB_PASSWORD=<secure_password>
   export JWT_SECRET=<generated_secret>
   export CORS_ORIGINS=https://myapp.com,https://www.myapp.com
   export ASPNETCORE_ENVIRONMENT=Production
   export SEED_DEFAULT_USERS=false
   ```

4. **Publish and run:**
   ```bash
   cd backend
   dotnet publish -c Release -o /var/www/hwsms
   cd /var/www/hwsms
   dotnet HSMS.API.dll
   ```

### Using systemd service

Create `/etc/systemd/system/hwsms-api.service`:

```ini
[Unit]
Description=HWSMS API Service
After=network.target

[Service]
Type=notify
User=hwsms
WorkingDirectory=/var/www/hwsms
ExecStart=/usr/bin/dotnet HSMS.API.dll

# Load environment variables from file
EnvironmentFile=/etc/hwsms/.env.production

# Restart configuration
Restart=always
RestartSec=5

[Install]
WantedBy=multi-user.target
```

Enable and start:
```bash
sudo systemctl daemon-reload
sudo systemctl enable hwsms-api
sudo systemctl start hwsms-api
```

---

## Frontend Deployment

### Build for Production

1. **Create production environment file:**
   ```bash
   cd frontend/HWSMS_UI
   cp .env.example .env.production
   ```

2. **Update with production API URL:**
   ```bash
   VITE_API_BASE_URL=https://api.yourdomain.com
   VITE_DEBUG=false
   ```

3. **Build the application:**
   ```bash
   npm run build
   ```

4. **Deploy static files to your web server:**
   ```bash
   # Copy dist folder to your web server
   scp -r dist/* user@server:/var/www/hwsms-ui/
   ```

### Nginx Configuration Example

```nginx
server {
    listen 80;
    server_name yourdomain.com;

    root /var/www/hwsms-ui;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }

    location /api {
        proxy_pass http://api.yourdomain.com;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

---

## Docker Deployment

### Local Development (Docker Compose)

```bash
# The docker-compose.override.yml is automatically used for development
docker-compose up -d

# View logs
docker-compose logs -f

# Access the application
# Frontend: http://localhost:3000
# Backend: http://localhost:5000
# Swagger API Docs: http://localhost:5000/swagger
```

### Production Deployment (Docker Compose)

1. **Set environment variables:**
   ```bash
   export DB_SERVER=mysql.example.com
   export DB_NAME=hwsms_prod
   export DB_USER=hwsms_user
   export DB_PASSWORD=<secure_password>
   export JWT_SECRET=<generated_secret>
   export CORS_ORIGINS=https://myapp.com
   export FRONTEND_API_URL=https://api.myapp.com
   ```

2. **Deploy:**
   ```bash
   docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
   ```

3. **View logs:**
   ```bash
   docker-compose -f docker-compose.yml -f docker-compose.prod.yml logs -f
   ```

### Kubernetes Deployment

Create a `ConfigMap` for non-sensitive configuration:

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: hwsms-config
data:
  ASPNETCORE_ENVIRONMENT: "Production"
  CORS_ORIGINS: "https://myapp.com"
  SEED_DEFAULT_USERS: "false"
```

Create a `Secret` for sensitive data:

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: hwsms-secrets
type: Opaque
data:
  DB_SERVER: ZGI=  # base64 encoded
  DB_PASSWORD: <base64_encoded_password>
  JWT_SECRET: <base64_encoded_secret>
```

---

## Security Best Practices

### 1. Environment Variable Security

✅ **DO:**
- Generate strong JWT secrets (64+ characters, random, no patterns)
- Use secure database passwords (18+ characters, mix of upper/lower/numbers/special chars)
- Store `.env` files outside of version control (use `.gitignore`)
- Use different secrets for each environment
- Rotate secrets periodically
- Use encrypted storage for production environment variables

❌ **DON'T:**
- Commit `.env` files to git
- Use default passwords in production
- Share environment variables via unencrypted channels
- Expose environment variables in frontend code
- Use the same secret across multiple environments

### 2. File Permissions

```bash
# Backend .env files should be readable only by the app
chmod 600 backend/.env.production
chmod 600 backend/.env.development

# Frontend .env files (check if deploying source)
chmod 600 frontend/HWSMS_UI/.env.production
```

### 3. Secret Management Tools

For production deployments, consider using:

- **AWS Systems Manager Parameter Store**
  ```bash
  aws ssm put-parameter --name "/hwsms/jwt-secret" --value "secret" --type "SecureString"
  ```

- **Azure Key Vault**
  ```bash
  az keyvault secret set --vault-name myvault --name "jwt-secret" --value "secret"
  ```

- **HashiCorp Vault**
  ```bash
  vault kv put secret/hwsms jwt_secret="secret"
  ```

### 4. Deployment Checklist

Before deploying to production:

- [ ] Generate new JWT secret (using secure random generator)
- [ ] Set up secure database with complex password
- [ ] Configure CORS origins for production domain only
- [ ] Disable default user seeding (`SEED_DEFAULT_USERS=false`)
- [ ] Set `ASPNETCORE_ENVIRONMENT=Production`
- [ ] Configure HTTPS/SSL certificates
- [ ] Set up firewall rules
- [ ] Enable database backups
- [ ] Monitor application logs
- [ ] Test authentication and authorization
- [ ] Verify CORS is working correctly
- [ ] Test frontend API communication

### 5. Database Security

```sql
-- Create dedicated database user (MySQL example)
CREATE USER 'hwsms_app'@'localhost' IDENTIFIED BY 'strong_password_here';
GRANT SELECT, INSERT, UPDATE, DELETE ON hwsms_prod.* TO 'hwsms_app'@'localhost';
FLUSH PRIVILEGES;

-- Make sure root user has strong password
ALTER USER 'root'@'localhost' IDENTIFIED BY 'root_strong_password';
```

---

## Troubleshooting

### Backend Won't Start

1. Check environment variables are set:
   ```bash
   echo $JWT_SECRET
   echo $DB_PASSWORD
   ```

2. Verify database connection:
   ```bash
   mysh -h $DB_SERVER -u $DB_USER -p$DB_PASSWORD -e "SELECT 1"
   ```

3. Check logs:
   ```bash
   docker-compose logs backend
   # Or
   journalctl -u hwsms-api -f
   ```

### Frontend Can't Connect to Backend

1. Verify CORS_ORIGINS includes frontend URL:
   ```bash
   # Check what's set
   echo $CORS_ORIGINS
   ```

2. Check VITE_API_BASE_URL is correct:
   ```bash
   # Check frontend .env file
   cat frontend/HWSMS_UI/.env.production
   ```

3. Test API connectivity:
   ```bash
   curl -v http://localhost:5162/swagger/
   ```

### Default Users Not Created

1. Check if SEED_DEFAULT_USERS is true:
   ```bash
   echo $SEED_DEFAULT_USERS
   ```

2. Check database connection works
3. View application logs for errors

---

## Summary

The HWSMS application now uses environment variables for all configuration, making it:

✅ Easy to deploy across different environments
✅ Secure (secrets not in version control)
✅ Flexible (no code changes needed for different deployments)
✅ Production-ready (proper separation of concerns)

For questions or issues, refer to the backend and frontend `.env.example` files for detailed configuration options.
