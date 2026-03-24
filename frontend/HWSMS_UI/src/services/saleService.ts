import axios from "axios";
import { getAuthHeader } from "./authService";

export type SaleItemPayload = {
  productId: number;
  quantity: number;
};

export type SalePayload = {
  items: SaleItemPayload[];
};

export type SaleItemResponse = {
  productId: number;
  productName: string;
  sku: string;
  unitPrice: number;
  quantity: number;
  lineSubtotal: number;
};

export type SaleResponse = {
  saleId: number;
  soldAt: string;
  totalAmount: number;
  soldBy: string;
  items: SaleItemResponse[];
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
const SALES_API_URL = `${API_BASE_URL}/api/Sales`;

export const createSale = async (payload: SalePayload) => {
  const { data } = await axios.post<SaleResponse>(SALES_API_URL, payload, {
    headers: getAuthHeader(),
  });

  return data;
};
