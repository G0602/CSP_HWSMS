import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { AppRoles } from "./auth/roles";
import ProtectedRoute from "./components/ProtectedRoute";
import PublicOnlyRoute from "./components/PublicOnlyRoute";
import AccessDeniedPage from "./pages/AccessDeniedPage";
import InvoicePreviewPage from "./pages/InvoicePreviewPage";
import LoginPage from "./pages/LoginPage";
import ProductDashboard from "./pages/ProductDashboard";
import RegisterPage from "./pages/RegisterPage";
import SalesPage from "./pages/SalesPage";
import TransactionHistoryPage from "./pages/TransactionHistoryPage";

function App() {
  return (
    <BrowserRouter>
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
          path="/register"
          element={
            <PublicOnlyRoute>
              <RegisterPage />
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
          path="/sales"
          element={
            <ProtectedRoute allowedRoles={[AppRoles.Admin, AppRoles.Manager, AppRoles.Cashier]}>
              <SalesPage />
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
          path="/access-denied"
          element={
            <ProtectedRoute>
              <AccessDeniedPage />
            </ProtectedRoute>
          }
        />
        <Route path="*" element={<Navigate to="/dashboard" replace />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;



