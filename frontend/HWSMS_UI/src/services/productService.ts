import axios from "axios";
import { getAuthHeader } from "./authService";
import { API_BASE_URL } from "../config/api";

export type ProductPayload = {
  name: string;
  sku: string;
  price: number;
  quantity: number;
  category: string;
  supplierId?: number | null;
};

export type Product = ProductPayload & {
  id: number;
  createdAt?: string;
};

export type InventoryProduct = {
  id: number;
  name: string;
  sku: string;
  quantity: number;
  category: string;
  price: number;
  supplierId?: number | null;
  isLowStock: boolean;
};

export type ProductStockUpdatePayload = {
  quantity: number;
  reason?: string;
};

const API_URL = `${API_BASE_URL}/api/Product`;

export const getProducts = async () => {
  return await axios.get<Product[]>(API_URL, {
    headers: getAuthHeader(),
  });
};

export const getInventoryProducts = async () => {
  const response = await axios.get<InventoryProduct[]>(`${API_URL}/inventory`, {
    headers: getAuthHeader(),
  });
  return response.data;
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

export const updateProductStock = async (id: number, payload: ProductStockUpdatePayload) => {
  return await axios.put(`${API_URL}/${id}/stock`, payload, {
    headers: getAuthHeader(),
  });
};

export const deleteProduct = async (id: number) => {
  return await axios.delete(`${API_URL}/${id}`, {
    headers: getAuthHeader(),
  });
};
