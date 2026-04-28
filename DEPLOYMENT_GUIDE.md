# HWSMS Deployment Guide

This guide documents the current deployment model for the project as it exists in this repository.

## Scope

This repository currently contains:

- an ASP.NET Core backend in `backend/HSMS.API`
- a Vite/React frontend in `frontend/HWSMS_UI`
- environment-variable driven backend configuration
- static frontend production build output via `npm run build`

This repository does not currently include active Docker Compose deployment files at the root, so deployment guidance below focuses on direct backend hosting and static frontend hosting.

## Deployment Overview

Typical production deployment has two parts:

1. Publish and host the backend API.
2. Build and host the frontend static files.

## Backend Deployment

### Required configuration

Provide these values through environment variables or hosting platform configuration:

| Setting | Purpose |
|---|---|
| `ConnectionStrings__DefaultConnection` | MySQL connection string |
| `JWT_SECRET` or `Jwt__Secret` | JWT signing secret |
| `JWT_ISSUER` or `Jwt__Issuer` | JWT issuer |
| `JWT_AUDIENCE` or `Jwt__Audience` | JWT audience |
| `CORS_ORIGINS` | Comma-separated allowed frontend origins |
| `ASPNETCORE_ENVIRONMENT` | `Production` in production |
| `ASPNETCORE_URLS` | Optional listening URLs |

Optional:

| Setting | Purpose |
|---|---|
| `FRONTEND_URL` | Added to CORS candidates |
| `LOW_STOCK_THRESHOLD` | Inventory/report threshold |
| `JWT_EXPIRY_MINUTES` | Token lifetime |

### Production security notes

- Use a strong JWT secret at least 32 bytes long.
- Do not rely on development seed users in production.
- Set `ASPNETCORE_ENVIRONMENT=Production`.
- Ensure production database credentials are not stored in source control.

### Publish and run

```bash
cd backend
dotnet restore
dotnet publish HSMS.API/HSMS.API.csproj -c Release -o ./publish
```

Start the API:

```bash
cd backend/publish
ASPNETCORE_ENVIRONMENT=Production dotnet HSMS.API.dll
```

### systemd example

Example service:

```ini
[Unit]
Description=HWSMS API
After=network.target

[Service]
WorkingDirectory=/var/www/hwsms-api
ExecStart=/usr/bin/dotnet /var/www/hwsms-api/HSMS.API.dll
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:5162
Environment=ConnectionStrings__DefaultConnection=Server=...;Database=...;Uid=...;Pwd=...;
Environment=Jwt__Secret=replace-with-real-secret
Environment=Jwt__Issuer=HSMS.API
Environment=Jwt__Audience=HSMS.Client
Environment=CORS_ORIGINS=https://your-frontend.example.com
Restart=always
RestartSec=5

[Install]
WantedBy=multi-user.target
```

Then:

```bash
sudo systemctl daemon-reload
sudo systemctl enable hwsms-api
sudo systemctl start hwsms-api
```

## Frontend Deployment

### Configure the production API URL

```bash
cd frontend/HWSMS_UI
cp .env.example .env.production
```

Set:

```env
VITE_API_BASE_URL=https://your-api.example.com
VITE_DEBUG=false
```

### Build

```bash
cd frontend/HWSMS_UI
npm install
npm run build
```

The deployable output is created in `frontend/HWSMS_UI/dist`.

### Static hosting options

You can host the frontend on:

- Azure Static Web Apps
- Vercel
- Netlify
- Nginx / Apache
- any static file host that supports SPA routing fallback

### Nginx SPA example

```nginx
server {
    listen 80;
    server_name your-frontend.example.com;

    root /var/www/hwsms-ui;
    index index.html;

    location / {
        try_files $uri $uri/ /index.html;
    }
}
```

## CORS

The backend builds its allow-list from:

- `CORS_ORIGINS`
- `FRONTEND_URL`
- built-in defaults depending on environment

For production, explicitly set `CORS_ORIGINS` to your real frontend origins.

Example:

```bash
export CORS_ORIGINS="https://your-frontend.example.com,https://www.your-frontend.example.com"
```

## Health Checks and Verification

After deployment, verify:

```bash
curl https://your-api.example.com/api/health
```

In production, Swagger is normally hidden because the current API only enables Swagger in `Development`.

Verify frontend build locally before release:

```bash
cd frontend/HWSMS_UI
npm run build
```

Verify backend startup:

```bash
cd backend
dotnet test
```

## Troubleshooting

### Backend starts locally but not in production

Check:

- `ConnectionStrings__DefaultConnection`
- `JWT_SECRET` / `Jwt__Secret`
- `JWT_ISSUER`
- `JWT_AUDIENCE`
- port binding permissions

### Browser requests fail with CORS errors

Check:

- `CORS_ORIGINS`
- `FRONTEND_URL`
- trailing slash mismatches
- correct frontend deployment URL

### Login works but protected requests fail

Check:

- token issuer and audience settings
- JWT secret consistency
- production time synchronization if tokens appear immediately expired

## Related Docs

- [README.md](./README.md)
- [QUICK_START.md](./QUICK_START.md)
- [ENV_VARIABLES_CHECKLIST.md](./ENV_VARIABLES_CHECKLIST.md)
- [backend/.env.example](./backend/.env.example)
- [frontend/HWSMS_UI/.env.example](./frontend/HWSMS_UI/.env.example)
