import axios from "axios";
import { getAuthHeader } from "./authService";
import { API_BASE_URL } from "../config/api";
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
