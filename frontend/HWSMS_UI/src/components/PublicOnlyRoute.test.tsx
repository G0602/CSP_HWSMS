import { render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import PublicOnlyRoute from "./PublicOnlyRoute";

const authServiceMocks = vi.hoisted(() => ({
  isAuthenticated: vi.fn(),
}));

vi.mock("../services/authService", () => ({
  isAuthenticated: authServiceMocks.isAuthenticated,
}));

const renderRoute = () =>
  render(
    <MemoryRouter initialEntries={["/login"]}>
      <Routes>
        <Route path="/dashboard" element={<div>Dashboard Page</div>} />
        <Route
          path="/login"
          element={
            <PublicOnlyRoute>
              <div>Login Form</div>
            </PublicOnlyRoute>
          }
        />
      </Routes>
    </MemoryRouter>,
  );

describe("PublicOnlyRoute", () => {
  beforeEach(() => {
    authServiceMocks.isAuthenticated.mockReset();
  });

  it("redirects authenticated users to dashboard", () => {
    authServiceMocks.isAuthenticated.mockReturnValue(true);

    renderRoute();

    expect(screen.getByText("Dashboard Page")).toBeTruthy();
  });

  it("renders children for unauthenticated users", () => {
    authServiceMocks.isAuthenticated.mockReturnValue(false);

    renderRoute();

    expect(screen.getByText("Login Form")).toBeTruthy();
  });
});
