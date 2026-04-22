import { describe, expect, it, beforeEach } from "vitest";
import {
  getAccessToken,
  getAuthHeader,
  getCurrentUser,
  isAuthenticated,
  logout,
  type AuthResponse,
} from "./authService";

const TOKEN_KEY = "hsms_access_token";
const USER_KEY = "hsms_auth_user";

const persistSessionManually = (response: AuthResponse) => {
  sessionStorage.setItem(TOKEN_KEY, response.accessToken);
  sessionStorage.setItem(
    USER_KEY,
    JSON.stringify({
      userId: response.userId,
      username: response.username,
      role: response.role,
      expiresAtUtc: response.expiresAtUtc,
    }),
  );
};

describe("authService session helpers", () => {
  beforeEach(() => {
    sessionStorage.clear();
  });

  it("returns false when there is no token or user session", () => {
    expect(isAuthenticated()).toBe(false);
    expect(getAccessToken()).toBeNull();
    expect(getCurrentUser()).toBeNull();
  });

  it("returns true when session exists and expiry is in the future", () => {
    persistSessionManually({
      userId: 1,
      accessToken: "token-123",
      username: "manager",
      role: "Manager",
      expiresAtUtc: new Date(Date.now() + 60_000).toISOString(),
    });

    expect(isAuthenticated()).toBe(true);
    expect(getAccessToken()).toBe("token-123");
    expect(getCurrentUser()).toMatchObject({
      userId: 1,
      username: "manager",
      role: "Manager",
    });
    expect(getAuthHeader()).toEqual({ Authorization: "Bearer token-123" });
  });

  it("returns false when expiry is in the past", () => {
    persistSessionManually({
      userId: 2,
      accessToken: "expired-token",
      username: "cashier",
      role: "Cashier",
      expiresAtUtc: new Date(Date.now() - 60_000).toISOString(),
    });

    expect(isAuthenticated()).toBe(false);
  });

  it("returns null when stored user JSON is invalid", () => {
    sessionStorage.setItem(TOKEN_KEY, "token");
    sessionStorage.setItem(USER_KEY, "{invalid-json");

    expect(getCurrentUser()).toBeNull();
    expect(isAuthenticated()).toBe(false);
  });

  it("clears session values on logout", () => {
    persistSessionManually({
      userId: 3,
      accessToken: "token-logout",
      username: "admin",
      role: "Admin",
      expiresAtUtc: new Date(Date.now() + 60_000).toISOString(),
    });

    logout();

    expect(getAccessToken()).toBeNull();
    expect(getCurrentUser()).toBeNull();
    expect(getAuthHeader()).toEqual({});
  });
});
