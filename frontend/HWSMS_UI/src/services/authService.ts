import axios from "axios";
import { API_BASE_URL } from "../config/api";

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

export type UserRole = "Admin" | "Manager" | "Cashier";

export type ManagedUser = {
  id: number;
  username: string;
  role: UserRole;
  createdAt?: string;
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

export const getUsers = async () => {
  const { data } = await axios.get<ManagedUser[]>(USERS_API_URL, {
    headers: getAuthHeader(),
  });
  return data;
};

export const updateUserRole = async (userId: number, role: UserRole) => {
  const { data } = await axios.put<{ message?: string; auth?: AuthResponse } | string>(
    `${USERS_API_URL}/${userId}/role`,
    { role },
    { headers: getAuthHeader() },
  );

  if (typeof data === "object" && data?.auth) {
    persistSession(data.auth);
  }

  return data;
};

export const deleteUser = async (userId: number) => {
  const { data } = await axios.delete<string>(`${USERS_API_URL}/${userId}`, {
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
