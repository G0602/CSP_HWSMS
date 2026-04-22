import axios from "axios";
import { getAuthHeader } from "./authService";
import { API_BASE_URL } from "../config/api";

export type DailySalesReportItem = {
  date: string;
  totalAmount: number;
};

export type MonthlySalesReportItem = {
  month: string;
  totalAmount: number;
};

export type LowStockReportItem = {
  id: number;
  name: string;
  sku: string;
  quantity: number;
  category: string;
  price: number;
  supplierId?: number | null;
  supplierName?: string | null;
  isLowStock: boolean;
};

export type ReportExportType = "daily" | "monthly" | "low-stock";

export type ReportsSummary = {
  daily: DailySalesReportItem[];
  monthly: MonthlySalesReportItem[];
  lowStock: LowStockReportItem[];
};

export type DailySalesAnalyticsItem = {
  date: string;
  sales: number;
  cost: number;
  profit: number;
};

export type MonthlySalesAnalyticsItem = {
  month: string;
  sales: number;
  cost: number;
  profit: number;
};

export type SalesAnalytics = {
  totalSales: number;
  totalCost: number;
  totalProfit: number;
  dailyTrends: DailySalesAnalyticsItem[];
  monthlyTrends: MonthlySalesAnalyticsItem[];
};

export type SalesAnalyticsFilters = {
  fromDate?: string;
  toDate?: string;
  productId?: number;
  category?: string;
};

const REPORTS_API_URL = `${API_BASE_URL}/api/reports`;

export const getDailySalesReport = async () => {
  const { data } = await axios.get<DailySalesReportItem[]>(`${REPORTS_API_URL}/daily`, {
    headers: getAuthHeader(),
  });
  return data;
};

export const getMonthlySalesReport = async () => {
  const { data } = await axios.get<MonthlySalesReportItem[]>(`${REPORTS_API_URL}/monthly`, {
    headers: getAuthHeader(),
  });
  return data;
};

export const getLowStockReport = async () => {
  const { data } = await axios.get<LowStockReportItem[]>(`${REPORTS_API_URL}/low-stock`, {
    headers: getAuthHeader(),
  });
  return data;
};

export const getReportsSummary = async () => {
  const { data } = await axios.get<ReportsSummary>(`${REPORTS_API_URL}/summary`, {
    headers: getAuthHeader(),
  });
  return data;
};

export const exportReportCsv = async (type: ReportExportType) => {
  const response = await axios.get(`${REPORTS_API_URL}/export`, {
    headers: getAuthHeader(),
    params: { type },
    responseType: "blob",
  });

  const disposition = response.headers["content-disposition"] as string | undefined;
  const fileNameMatch = disposition?.match(/filename="?([^"]+)"?/i);
  const fileName = fileNameMatch?.[1] ?? `${type}-report-${new Date().toISOString().slice(0, 10)}.csv`;

  return {
    blob: response.data as Blob,
    fileName,
  };
};

export const getSalesAnalytics = async (filters: SalesAnalyticsFilters) => {
  const { data } = await axios.get<SalesAnalytics>(`${REPORTS_API_URL}/analytics`, {
    headers: getAuthHeader(),
    params: filters,
  });

  return data;
};
