import axios from "axios";

export type LoginPayload = {
  username: string;
  password: string;
};

export type RegisterPayload = {
  username: string;
  password: string;
  role?: "Admin" | "Manager" | "Cashier";
};

export type CreateUserPayload = {
  username: string;
  password: string;
  role: "Admin" | "Manager" | "Cashier";
};

export type AuthResponse = {
  userId: number;
  accessToken: string;
  expiresAtUtc: string;
  username: string;
  role: string;
};

export type AuthUser = {
  userId: number;
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
const USERS_API_URL = `${API_BASE_URL}/api/users`;

const persistSession = (response: AuthResponse) => {
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

export const createUser = async (payload: CreateUserPayload) => {
  const { data } = await axios.post(USERS_API_URL, payload, {
    headers: getAuthHeader(),
  });
  return data;
};

export const logout = () => {
  sessionStorage.removeItem(TOKEN_KEY);
  sessionStorage.removeItem(USER_KEY);
};

export const getAccessToken = () => sessionStorage.getItem(TOKEN_KEY);

export const getCurrentUser = (): AuthUser | null => {
  const raw = sessionStorage.getItem(USER_KEY);
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
