import axios from "axios";
import { getAuthHeader } from "./authService";

export type InvoiceItem = {
  productId: number;
  productName: string;
  sku: string;
  unitPrice: number;
  quantity: number;
  lineSubtotal: number;
};

export type InvoiceResponse = {
  transactionId: number;
  invoiceNumber: string;
  soldAt: string;
  soldBy: string;
  subtotal: number;
  taxRate: number;
  taxAmount: number;
  grandTotal: number;
  items: InvoiceItem[];
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

export const getInvoiceByTransactionId = async (transactionId: number) => {
  const { data } = await axios.get<InvoiceResponse>(`${SALES_API_URL}/${transactionId}/invoice`, {
    headers: getAuthHeader(),
  });

  return data;
};
