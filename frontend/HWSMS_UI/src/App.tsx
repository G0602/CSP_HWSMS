import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { AppRoles } from "./auth/roles";
import BackendHealthBanner from "./components/BackendHealthBanner";
import ProtectedRoute from "./components/ProtectedRoute";
import PublicOnlyRoute from "./components/PublicOnlyRoute";
import AccessDeniedPage from "./pages/AccessDeniedPage";
import DailySalesReportPage from "./pages/DailySalesReportPage";
import InventoryPage from "./pages/InventoryPage";
import InvoicePreviewPage from "./pages/InvoicePreviewPage";
import LoginPage from "./pages/LoginPage";
import ProductDashboard from "./pages/ProductDashboard";
import SalesPage from "./pages/SalesPage";
import SupplierPage from "./pages/SupplierPage";
import TransactionHistoryPage from "./pages/TransactionHistoryPage";
import UsersPage from "./pages/UsersPage";

function App() {
  return (
    <BrowserRouter>
      <BackendHealthBanner />
      <Routes>
        <Route
          path="/login"
          element={
            <PublicOnlyRoute>
              <LoginPage />
            </PublicOnlyRoute>
          }
        />
        <Route
          path="/dashboard"
          element={
            <ProtectedRoute allowedRoles={[AppRoles.Admin, AppRoles.Manager]}>
              <ProductDashboard />
            </ProtectedRoute>
          }
        />
        <Route
          path="/inventory"
          element={
            <ProtectedRoute allowedRoles={[AppRoles.Admin, AppRoles.Manager]}>
              <InventoryPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/sales"
          element={
            <ProtectedRoute allowedRoles={[AppRoles.Admin, AppRoles.Manager, AppRoles.Cashier]}>
              <SalesPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/suppliers"
          element={
            <ProtectedRoute allowedRoles={[AppRoles.Admin, AppRoles.Manager]}>
              <SupplierPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/transactions"
          element={
            <ProtectedRoute allowedRoles={[AppRoles.Admin, AppRoles.Manager]}>
              <TransactionHistoryPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/transactions/:transactionId/invoice"
          element={
            <ProtectedRoute allowedRoles={[AppRoles.Admin, AppRoles.Manager]}>
              <InvoicePreviewPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/reports/daily"
          element={
            <ProtectedRoute allowedRoles={[AppRoles.Admin, AppRoles.Manager]}>
              <DailySalesReportPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/users"
          element={
            <ProtectedRoute allowedRoles={[AppRoles.Admin]}>
              <UsersPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/access-denied"
          element={
            <ProtectedRoute>
              <AccessDeniedPage />
            </ProtectedRoute>
          }
        />
        <Route path="*" element={<Navigate to="/inventory" replace />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;

