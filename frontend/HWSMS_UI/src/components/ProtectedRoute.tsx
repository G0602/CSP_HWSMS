import { Navigate } from "react-router-dom";
import type { ReactNode } from "react";
import { getCurrentUser, isAuthenticated } from "../services/authService";
import type { AppRole } from "../auth/roles";

type ProtectedRouteProps = {
  children: ReactNode;
  allowedRoles?: AppRole[];
};

const ProtectedRoute = ({ children, allowedRoles }: ProtectedRouteProps) => {
  if (!isAuthenticated()) {
    return <Navigate to="/login" replace />;
  }

  if (allowedRoles && allowedRoles.length > 0) {
    const user = getCurrentUser();
    if (!user || !allowedRoles.includes(user.role as AppRole)) {
      return <Navigate to="/access-denied" replace />;
    }
  }

  return children;
};

export default ProtectedRoute;
