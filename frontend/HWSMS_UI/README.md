# HWSMS Frontend

Frontend overview for the current React 19 + TypeScript + Vite application.

## Stack

- React 19
- TypeScript
- Vite
- Tailwind CSS
- React Router
- Axios
- Vitest

## Current pages and behavior

- login
- dashboard
- inventory
- sales
- suppliers
- transaction history
- invoice preview
- daily reporting
- user management
- backend health banner
- protected-route and public-only route handling

## Route access summary

| Route | Access |
|---|---|
| `/login` | Public |
| `/dashboard` | Admin, Manager |
| `/inventory` | Admin, Manager |
| `/sales` | Admin, Manager, Cashier |
| `/suppliers` | Admin, Manager |
| `/transactions` | Admin, Manager |
| `/transactions/:transactionId/invoice` | Admin, Manager |
| `/reports/daily` | Admin, Manager |
| `/users` | Admin |
| `/access-denied` | Authenticated users |

## Environment

The frontend uses Vite environment loading.

Primary variables:

| Variable | Purpose |
|---|---|
| `VITE_API_BASE_URL` | Backend API base URL |
| `VITE_DEBUG` | Optional debug flag |

API base URL resolution:

1. `VITE_API_BASE_URL`
2. legacy `VITE_API_URL`
3. local default in development
4. deployed default in production

Example setup:

```bash
cd frontend/HWSMS_UI
cp .env.example .env.development
```

## Run locally

```bash
cd frontend/HWSMS_UI
npm install
npm run dev
```

Default local URL:

- `http://localhost:5173`

## Tests and build

Validated current frontend automation:

- `17` passing tests

Commands:

```bash
npm test
npm run build
npm run lint
```

## Related Docs

- [../../README.md](../../README.md)
- [../../QUICK_START.md](../../QUICK_START.md)
- [../../DEPLOYMENT_GUIDE.md](../../DEPLOYMENT_GUIDE.md)
- [../../docs/Test-Documents/TESTING_OVERVIEW.md](../../docs/Test-Documents/TESTING_OVERVIEW.md)
