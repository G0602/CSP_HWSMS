import axios from "axios";
import { getAuthHeader } from "./authService";

export type SupplierPayload = {
  name: string;
  contactInfo?: string;
};

export type Supplier = {
  id: number;
  name: string;
  contactInfo?: string;
  createdAt?: string;
};

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
const SUPPLIERS_API_URL = `${API_BASE_URL}/api/suppliers`;

export const addSupplier = async (payload: SupplierPayload) => {
  const { data } = await axios.post(SUPPLIERS_API_URL, payload, {
    headers: getAuthHeader(),
  });

  return data;
};

export const getSuppliers = async () => {
  const { data } = await axios.get<Supplier[]>(SUPPLIERS_API_URL, {
    headers: getAuthHeader(),
  });

  return data;
};

export const updateSupplier = async (id: number, payload: SupplierPayload) => {
  const { data } = await axios.put<string>(`${SUPPLIERS_API_URL}/${id}`, payload, {
    headers: getAuthHeader(),
  });

  return data;
};

export const deleteSupplier = async (id: number) => {
  const { data } = await axios.delete<string>(`${SUPPLIERS_API_URL}/${id}`, {
    headers: getAuthHeader(),
  });

  return data;
};
