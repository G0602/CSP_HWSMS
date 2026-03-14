import axios from "axios";

export type LoginPayload = {
  username: string;
  password: string;
};

export type RegisterPayload = {
  username: string;
  password: string;
  role?: "User" | "Admin";
};

export type AuthResponse = {
  accessToken: string;
  expiresAtUtc: string;
  username: string;
  role: string;
};

export type AuthUser = {
  username: string;
  role: string;
  expiresAtUtc: string;
};

const TOKEN_KEY = "hsms_access_token";
const USER_KEY = "hsms_auth_user";

const resolveApiBaseUrl = () => {
  const explicitBaseUrl = import.meta.env.VITE_API_BASE_URL as string | undefined;
  if (explicitBaseUrl) {
    return explicitBaseUrl.replace(/\/$/, "");
  }

  const legacyProductUrl = import.meta.env.VITE_API_URL as string | undefined;
  if (legacyProductUrl) {
    return legacyProductUrl.replace(/\/api\/Product\/?$/i, "").replace(/\/$/, "");
  }

  return "http://localhost:5162";
};

const API_BASE_URL = resolveApiBaseUrl();
const AUTH_API_URL = `${API_BASE_URL}/api/Auth`;

const persistSession = (response: AuthResponse) => {
  localStorage.setItem(TOKEN_KEY, response.accessToken);
  localStorage.setItem(
    USER_KEY,
    JSON.stringify({
      username: response.username,
      role: response.role,
      expiresAtUtc: response.expiresAtUtc,
    }),
  );
};

export const register = async (payload: RegisterPayload) => {
  const { data } = await axios.post<AuthResponse>(`${AUTH_API_URL}/register`, payload);
  persistSession(data);
  return data;
};

export const login = async (payload: LoginPayload) => {
  const { data } = await axios.post<AuthResponse>(`${AUTH_API_URL}/login`, payload);
  persistSession(data);
  return data;
};

export const logout = () => {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(USER_KEY);
};

export const getAccessToken = () => localStorage.getItem(TOKEN_KEY);

export const getCurrentUser = (): AuthUser | null => {
  const raw = localStorage.getItem(USER_KEY);
  if (!raw) {
    return null;
  }

  try {
    return JSON.parse(raw) as AuthUser;
  } catch {
    return null;
  }
};

export const isAuthenticated = () => {
  const token = getAccessToken();
  const user = getCurrentUser();

  if (!token || !user?.expiresAtUtc) {
    return false;
  }

  const expiry = Date.parse(user.expiresAtUtc);
  if (Number.isNaN(expiry)) {
    return false;
  }

  return expiry > Date.now();
};

export const getAuthHeader = () => {
  const token = getAccessToken();
  return token ? { Authorization: `Bearer ${token}` } : {};
};
