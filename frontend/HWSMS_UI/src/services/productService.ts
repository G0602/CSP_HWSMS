import axios from "axios";
import { getAuthHeader } from "./authService";

export type ProductPayload = {
  name: string;
  sku: string;
  price: number;
  quantity: number;
  category: string;
};

export type Product = ProductPayload & {
  id: number;
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
const API_URL = `${API_BASE_URL}/api/Product`;

export const getProducts = async () => {
  return await axios.get<Product[]>(API_URL, {
    headers: getAuthHeader(),
  });
};

export const searchProducts = async (query: string) => {
  const response = await axios.get<Product[]>(`${API_URL}/search`, {
    params: { query },
    headers: getAuthHeader(),
  });
  return response.data;
};

export const addProduct = async (product: ProductPayload) => {
  return await axios.post(API_URL, product, {
    headers: getAuthHeader(),
  });
};

export const updateProduct = async (id: number, product: ProductPayload) => {
  return await axios.put(`${API_URL}/${id}`, product, {
    headers: getAuthHeader(),
  });
};

export const deleteProduct = async (id: number) => {
  return await axios.delete(`${API_URL}/${id}`, {
    headers: getAuthHeader(),
  });
};
