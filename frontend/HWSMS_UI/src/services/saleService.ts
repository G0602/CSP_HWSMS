import axios from "axios";
import { getAuthHeader } from "./authService";
import { API_BASE_URL } from "../config/api";

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

const SALES_API_URL = `${API_BASE_URL}/api/Sales`;

export const createSale = async (payload: SalePayload) => {
  const { data } = await axios.post<SaleResponse>(SALES_API_URL, payload, {
    headers: getAuthHeader(),
  });

  return data;
};
