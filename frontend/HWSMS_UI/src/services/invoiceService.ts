import axios from "axios";
import { getAuthHeader } from "./authService";
import { API_BASE_URL } from "../config/api";

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

const SALES_API_URL = `${API_BASE_URL}/api/Sales`;

export const getInvoiceByTransactionId = async (transactionId: number) => {
  const { data } = await axios.get<InvoiceResponse>(`${SALES_API_URL}/${transactionId}/invoice`, {
    headers: getAuthHeader(),
  });

  return data;
};
