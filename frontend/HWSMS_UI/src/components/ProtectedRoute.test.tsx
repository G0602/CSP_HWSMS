import { render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import ProtectedRoute from "./ProtectedRoute";
import type { AppRole } from "../auth/roles";

const authServiceMocks = vi.hoisted(() => ({
  isAuthenticated: vi.fn(),
  getCurrentUser: vi.fn(),
}));

vi.mock("../services/authService", () => ({
  isAuthenticated: authServiceMocks.isAuthenticated,
  getCurrentUser: authServiceMocks.getCurrentUser,
}));

const renderRoute = (allowedRoles?: AppRole[]) =>
  render(
    <MemoryRouter initialEntries={["/protected"]}>
      <Routes>
        <Route path="/login" element={<div>Login Page</div>} />
        <Route path="/access-denied" element={<div>Access Denied Page</div>} />
        <Route
          path="/protected"
          element={
            <ProtectedRoute allowedRoles={allowedRoles}>
              <div>Protected Content</div>
            </ProtectedRoute>
          }
        />
      </Routes>
    </MemoryRouter>,
  );

describe("ProtectedRoute", () => {
  beforeEach(() => {
    authServiceMocks.isAuthenticated.mockReset();
    authServiceMocks.getCurrentUser.mockReset();
  });

  it("redirects unauthenticated users to login", () => {
    authServiceMocks.isAuthenticated.mockReturnValue(false);

    renderRoute(["Admin"]);

    expect(screen.getByText("Login Page")).toBeTruthy();
  });

  it("redirects authenticated users without a matching role", () => {
    authServiceMocks.isAuthenticated.mockReturnValue(true);
    authServiceMocks.getCurrentUser.mockReturnValue({ role: "Cashier" });

    renderRoute(["Admin", "Manager"]);

    expect(screen.getByText("Access Denied Page")).toBeTruthy();
  });

  it("renders children for an allowed user", () => {
    authServiceMocks.isAuthenticated.mockReturnValue(true);
    authServiceMocks.getCurrentUser.mockReturnValue({ role: "Manager" });

    renderRoute(["Admin", "Manager"]);

    expect(screen.getByText("Protected Content")).toBeTruthy();
  });
});
