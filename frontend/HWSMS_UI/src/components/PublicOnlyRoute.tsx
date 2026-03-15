import { Navigate } from "react-router-dom";
import type { ReactNode } from "react";
import { isAuthenticated } from "../services/authService";

type PublicOnlyRouteProps = {
  children: ReactNode;
};

const PublicOnlyRoute = ({ children }: PublicOnlyRouteProps) => {
  if (isAuthenticated()) {
    return <Navigate to="/dashboard" replace />;
  }

  return children;
};

export default PublicOnlyRoute;
