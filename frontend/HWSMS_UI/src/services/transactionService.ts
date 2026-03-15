import axios from "axios";
import { getAuthHeader } from "./authService";
import type { SaleItemResponse, SaleResponse } from "./saleService";

export type TransactionHistoryItem = {
  saleId: number;
  soldAt: string;
  totalAmount: number;
  soldBy: string;
  itemCount: number;
};

export type TransactionDetails = SaleResponse & {
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

export const getTransactionHistory = async (params?: {
  transactionId?: number;
  fromDate?: string;
  toDate?: string;
  limit?: number;
}) => {
  const { data } = await axios.get<TransactionHistoryItem[]>(`${SALES_API_URL}/history`, {
    headers: getAuthHeader(),
    params,
  });

  return data;
};

export const getTransactionDetails = async (saleId: number) => {
  const { data } = await axios.get<TransactionDetails>(`${SALES_API_URL}/${saleId}`, {
    headers: getAuthHeader(),
  });

  return data;
};
