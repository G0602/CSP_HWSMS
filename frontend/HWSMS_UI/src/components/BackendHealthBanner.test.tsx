import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import BackendHealthBanner from "./BackendHealthBanner";

const healthMocks = vi.hoisted(() => ({
  useHealthCheck: vi.fn(),
  getApiBaseUrl: vi.fn(() => "http://localhost:5162"),
}));

vi.mock("../hooks/useHealthCheck", () => ({
  useHealthCheck: healthMocks.useHealthCheck,
}));

vi.mock("../services/healthService", () => ({
  getApiBaseUrl: healthMocks.getApiBaseUrl,
}));

describe("BackendHealthBanner", () => {
  it("shows a checking message while health status is pending", () => {
    healthMocks.useHealthCheck.mockReturnValue({
      isHealthy: null,
      isChecking: true,
    });

    render(<BackendHealthBanner />);

    expect(screen.getByText(/Checking backend connection/i)).toBeTruthy();
    expect(screen.getByText(/http:\/\/localhost:5162/i)).toBeTruthy();
  });

  it("shows an outage banner when backend is unhealthy", () => {
    healthMocks.useHealthCheck.mockReturnValue({
      isHealthy: false,
      isChecking: false,
    });

    render(<BackendHealthBanner />);

    expect(screen.getByText(/cannot reach the backend/i)).toBeTruthy();
    expect(screen.getByText(/http:\/\/localhost:5162/i)).toBeTruthy();
  });

  it("renders nothing when backend is healthy", () => {
    healthMocks.useHealthCheck.mockReturnValue({
      isHealthy: true,
      isChecking: false,
    });

    const { container } = render(<BackendHealthBanner />);

    expect(container.textContent).toBe("");
  });
});
