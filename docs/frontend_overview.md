# Frontend Overview

## Technology Stack

| Technology | Version | Purpose |
|---|---|---|
| React | 19.2 | UI component framework |
| TypeScript | ~5.9 | Type safety |
| Vite | 7.x | Build tool and dev server |
| React Router DOM | 7.x | Client-side routing |
| Axios | 1.x | HTTP API client |
| Tailwind CSS | 3.4 | Utility-first styling |
| Vitest | 3.x | Test runner |
| Testing Library | 16.x | React component testing utilities |

---

## Project Structure

```
frontend/HWSMS_UI/
├── index.html                   ← HTML entry point (React root mount)
├── vite.config.ts               ← Vite build configuration
├── tailwind.config.js           ← Tailwind configuration
├── tsconfig.app.json            ← TypeScript configuration
├── vercel.json                  ← SPA routing fallback (if deployed to Vercel)
├── .env.development             ← Local dev environment variables
├── .env.production              ← Production environment variables
├── .env.example                 ← Template for environment variables
└── src/
    ├── main.tsx                 ← React DOM entry point
    ├── App.tsx                  ← Root component: BrowserRouter + all Routes
    ├── App.css / index.css      ← Global styles + Tailwind directives
    ├── vite-env.d.ts            ← Vite env type declarations
    ├── auth/
    │   └── roles.ts             ← AppRoles constants (Admin, Manager, Cashier)
    ├── config/
    │   └── api.ts               ← Exports API_BASE_URL from VITE_API_BASE_URL
    ├── constants/               ← Shared application constants
    ├── components/
    │   ├── ProtectedRoute.tsx   ← Wraps routes that require auth + specific roles
    │   ├── PublicOnlyRoute.tsx  ← Redirects authenticated users away from /login
    │   ├── BackendHealthBanner.tsx ← Polls /api/health, shows warning if unhealthy
    │   └── [other shared UI components]
    ├── hooks/                   ← Custom React hooks (data fetching, etc.)
    ├── pages/
    │   ├── LoginPage.tsx
    │   ├── ProductDashboard.tsx
    │   ├── InventoryPage.tsx
    │   ├── SalesPage.tsx
    │   ├── SupplierPage.tsx
    │   ├── TransactionHistoryPage.tsx
    │   ├── InvoicePreviewPage.tsx
    │   ├── DailySalesReportPage.tsx
    │   ├── UsersPage.tsx
    │   └── AccessDeniedPage.tsx
    ├── services/
    │   ├── authService.ts       ← Login, logout, user management, token storage
    │   ├── productService.ts    ← Product CRUD + inventory + stock update
    │   ├── supplierService.ts   ← Supplier CRUD
    │   ├── saleService.ts       ← Create sale
    │   ├── transactionService.ts ← Sales history
    │   ├── invoiceService.ts    ← Invoice retrieval
    │   ├── reportService.ts     ← Daily/monthly/analytics/summary/low-stock reports + CSV export
    │   ├── healthService.ts     ← Backend health polling
    │   └── apiError.ts          ← Axios error helper
    └── test/
        └── [Vitest test files]
```

---

## Routing

All routes are defined in `App.tsx` using React Router v7.

| Route | Component | Access | Roles |
|---|---|---|---|
| `/login` | `LoginPage` | Public only | — |
| `/dashboard` | `ProductDashboard` | Protected | Admin, Manager |
| `/inventory` | `InventoryPage` | Protected | Admin, Manager |
| `/sales` | `SalesPage` | Protected | Admin, Manager, Cashier |
| `/suppliers` | `SupplierPage` | Protected | Admin, Manager |
| `/transactions` | `TransactionHistoryPage` | Protected | Admin, Manager |
| `/transactions/:transactionId/invoice` | `InvoicePreviewPage` | Protected | Admin, Manager |
| `/reports/daily` | `DailySalesReportPage` | Protected | Admin, Manager |
| `/users` | `UsersPage` | Protected | Admin only |
| `/access-denied` | `AccessDeniedPage` | Protected (any auth) | — |
| `*` (catch-all) | Redirect to `/inventory` | — | — |

### Route Guards

**`ProtectedRoute`**: Checks `isAuthenticated()` and optionally checks the user's role against `allowedRoles`. If not authenticated → redirects to `/login`. If authenticated but wrong role → redirects to `/access-denied`.

**`PublicOnlyRoute`**: Wraps `/login`. If the user is already authenticated → redirects to `/inventory`.

---

## Authentication Flow

Session state is stored in **`sessionStorage`** (cleared on tab/window close).

| Key | Value |
|---|---|
| `hsms_access_token` | Raw JWT string |
| `hsms_auth_user` | JSON: `{ userId, username, role, expiresAtUtc }` |

**Login sequence:**
1. User submits username + password on `LoginPage`.
2. `authService.login()` → `POST /api/auth/login`.
3. On success, `persistSession()` writes token + user object to `sessionStorage`.
4. React Router redirects to `/inventory` (or role-appropriate landing page).

**Authentication check (`isAuthenticated()`):**
1. Retrieves token and user from `sessionStorage`.
2. Parses `expiresAtUtc` and compares to `Date.now()`.
3. Returns `false` if token missing, expiry missing, or already expired.

**Logout:** `authService.logout()` removes both `sessionStorage` keys.

**Authenticated API calls:** Every service module calls `getAuthHeader()` which returns `{ Authorization: "Bearer <token>" }` and attaches it to all Axios requests.

---

## Service Layer

All API calls use **Axios** and are organized by resource domain.

### `authService.ts`
- `login(payload)` → `POST /api/auth/login`
- `createUser(payload)` → `POST /api/users`
- `getUsers()` → `GET /api/users`
- `updateUserRole(userId, role)` → `PUT /api/users/{userId}/role`
- `deleteUser(userId)` → `DELETE /api/users/{userId}`
- `resetUserPassword(userId, payload)` → `PUT /api/users/{userId}/password`
- `logout()` — clears sessionStorage
- `getAccessToken()`, `getCurrentUser()`, `isAuthenticated()`, `getAuthHeader()`

### `productService.ts`
- `getProducts()` → `GET /api/Product`
- `getInventoryProducts()` → `GET /api/Product/inventory`
- `searchProducts(query)` → `GET /api/Product/search?query=`
- `addProduct(payload)` → `POST /api/Product`
- `updateProduct(id, payload)` → `PUT /api/Product/{id}`
- `updateProductStock(id, payload)` → `PUT /api/Product/{id}/stock`
- `deleteProduct(id)` → `DELETE /api/Product/{id}`

### `supplierService.ts`
- `getSuppliers()` → `GET /api/suppliers`
- `addSupplier(payload)` → `POST /api/suppliers`
- `updateSupplier(id, payload)` → `PUT /api/suppliers/{id}`
- `deleteSupplier(id)` → `DELETE /api/suppliers/{id}`

### `saleService.ts`
- `createSale(payload)` → `POST /api/Sales`

### `transactionService.ts`
- `getSalesHistory(params)` → `GET /api/Sales/history`

### `invoiceService.ts`
- `getInvoice(saleId)` → `GET /api/Sales/{saleId}/invoice`

### `reportService.ts`
- `getDailySalesReport()` → `GET /api/reports/daily`
- `getMonthlySalesReport()` → `GET /api/reports/monthly`
- `getSalesAnalytics(params)` → `GET /api/reports/analytics`
- `getSalesSummary()` → `GET /api/reports/summary`
- `getLowStockReport()` → `GET /api/reports/low-stock`
- CSV exports for daily, monthly, analytics, low-stock

### `healthService.ts`
- `checkHealth()` → `GET /api/health`

---

## Environment Variables

All frontend environment variables must be prefixed with `VITE_` to be exposed at build time.

| Variable | Description | Example |
|---|---|---|
| `VITE_API_BASE_URL` | Base URL of the backend API | `https://your-backend.azurewebsites.net` |
| `VITE_DEBUG` | Enable debug logging (optional) | `true` |

**Local development (`.env.development`):**
```
VITE_API_BASE_URL=http://localhost:5162
```

**Production (`.env.production`):**
```
VITE_API_BASE_URL=https://your-production-backend.azurewebsites.net
```

> **Important:** In the CI/CD pipeline, `VITE_API_BASE_URL` is injected via the GitHub Actions variable `vars.VITE_API_BASE_URL` during the `npm run build` step. The value baked into the production `dist/` bundle cannot be changed at runtime.

---

## Pages Description

### `LoginPage`
Standard username/password form. No public registration. Redirects to `/inventory` on success.

### `ProductDashboard`
Overview dashboard showing inventory KPIs, low-stock alerts, and product summary stats.

### `InventoryPage`
Full inventory management: list all products with supplier info, add/edit/delete products, update stock quantities with reason tracking.

### `SalesPage`
POS-style transaction interface. Search products, add to cart, adjust quantities, confirm sale. Shows live running total. On confirmation → creates sale and triggers stock deduction.

### `SupplierPage`
Manage supplier records: list, add, edit, delete. Deletion blocked on the UI if products are linked.

### `TransactionHistoryPage`
Searchable and filterable list of past sales transactions. Links to individual invoice views.

### `InvoicePreviewPage`
Renders a printable invoice for a single transaction. Loaded via route param `/transactions/:transactionId/invoice`.

### `DailySalesReportPage`
Full analytics dashboard: daily/monthly charts, date-range analytics, summary KPIs, low-stock table. Includes CSV export buttons for all reports.

### `UsersPage`
Admin-only. Full user management: list all users, create new users, change roles, reset passwords, delete users.

### `AccessDeniedPage`
Shown when a user is authenticated but attempts to access a route their role does not permit.

---

## Frontend Testing

Tests are written with **Vitest** and **@testing-library/react**.

- Test files co-located in `src/services/*.test.ts` or `src/test/`.
- `npm test` runs: `vitest run` (single-pass, no watch).
- Total passing tests: **17**.

Currently tested:
- `authService.ts` — login, logout, session persistence, token retrieval, `isAuthenticated()` expiry logic.
- Component rendering tests for key UI components.

Run with:
```bash
cd frontend/HWSMS_UI
npm test
```
