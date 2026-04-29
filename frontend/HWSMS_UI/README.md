# HWSMS Frontend

React + TypeScript + Vite frontend for the Hardware Store Management System.

## Stack

- React 19
- TypeScript
- Vite
- Tailwind CSS
- React Router
- Axios
- Vitest

## Main Features

- login and registration flows
- protected routes by role
- inventory and product views
- supplier management
- sales screen
- transaction history and invoice preview
- reporting screens
- backend connectivity banner/health checks

## Environment Variables

The frontend uses Vite environment loading.

Primary values:

| Variable | Purpose |
|---|---|
| `VITE_API_BASE_URL` | Base backend URL |
| `VITE_DEBUG` | Optional debug logging flag |

Example local setup:

```bash
cd frontend/HWSMS_UI
cp .env.example .env.development
```

## Run Locally

```bash
cd frontend/HWSMS_UI
npm install
npm run dev
```

Default local URL:

- `http://localhost:5173`

## Build and Test

```bash
npm run build
npm run test
npm run lint
```

## Route Access Summary

| Route | Access |
|---|---|
| `/login` | public |
| `/dashboard` | Admin, Manager |
| `/inventory` | Admin, Manager |
| `/sales` | Admin, Manager, Cashier |
| `/suppliers` | Admin, Manager |
| `/transactions` | Admin, Manager |
| `/transactions/:transactionId/invoice` | Admin, Manager |
| `/reports/daily` | Admin, Manager |
| `/users` | Admin |

## API Base URL Resolution

The frontend resolves the API base URL in this order:

1. `VITE_API_BASE_URL`
2. legacy `VITE_API_URL`
3. local default in development
4. deployed backend default in production

## Related Docs

- [../../README.md](../../README.md)
- [../../QUICK_START.md](../../QUICK_START.md)
- [../../DEPLOYMENT_GUIDE.md](../../DEPLOYMENT_GUIDE.md)
