import axios from "axios";
import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import Navbar from "../components/Navbar";
import { getCurrentUser, logout } from "../services/authService";
import {
  exportReportCsv,
  getReportsSummary,
  type DailySalesReportItem,
  type LowStockReportItem,
  type MonthlySalesReportItem,
  type ReportExportType,
} from "../services/reportService";

const DailySalesReportPage = () => {
  const navigate = useNavigate();
  const [report, setReport] = useState<DailySalesReportItem[]>([]);
  const [monthlyReport, setMonthlyReport] = useState<MonthlySalesReportItem[]>([]);
  const [lowStockReport, setLowStockReport] = useState<LowStockReportItem[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [isExporting, setIsExporting] = useState(false);
  const [error, setError] = useState("");
  const user = getCurrentUser();

  const handleLogout = () => {
    logout();
    navigate("/login", { replace: true });
  };

  const loadReport = async () => {
    setError("");
    setIsLoading(true);

    try {
      const summary = await getReportsSummary();
      setReport(summary.daily);
      setMonthlyReport(summary.monthly);
      setLowStockReport(summary.lowStock);
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.status === 401) {
        handleLogout();
        return;
      }

      if (axios.isAxiosError(err) && err.response?.status === 403) {
        navigate("/access-denied", { replace: true });
        return;
      }

      setError("Failed to load daily sales report.");
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void loadReport();
  }, []);

  const handleExportCsv = async (type: ReportExportType) => {
    setError("");
    setIsExporting(true);

    try {
      const { blob, fileName } = await exportReportCsv(type);
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = fileName;
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.status === 401) {
        handleLogout();
        return;
      }

      if (axios.isAxiosError(err) && err.response?.status === 403) {
        navigate("/access-denied", { replace: true });
        return;
      }

      setError("Failed to export CSV report.");
    } finally {
      setIsExporting(false);
    }
  };

  const totalSales = useMemo(() => report.reduce((sum, item) => sum + item.totalAmount, 0), [report]);
  const totalMonthlySales = useMemo(
    () => monthlyReport.reduce((sum, item) => sum + item.totalAmount, 0),
    [monthlyReport],
  );
  const maxMonthlyAmount = useMemo(
    () => Math.max(...monthlyReport.map((item) => item.totalAmount), 0),
    [monthlyReport],
  );

  return (
    <div className="min-h-screen bg-slate-50">
      <Navbar username={user?.username} onLogout={handleLogout} />

      <div className="mx-auto max-w-5xl p-6 lg:p-10">
        <div className="mb-6">
          <h2 className="text-3xl font-bold text-slate-900">Sales Reports</h2>
          <p className="text-slate-600 mt-1">Daily and monthly aggregated sales totals.</p>
          <div className="mt-4 flex flex-wrap gap-3">
            <button
              type="button"
              onClick={() => void handleExportCsv("daily")}
              disabled={isExporting}
              className="rounded-lg bg-emerald-600 px-4 py-2 text-white text-sm font-medium hover:bg-emerald-700 disabled:opacity-60 disabled:cursor-not-allowed"
            >
              {isExporting ? "Exporting..." : "Export Daily CSV"}
            </button>
            <button
              type="button"
              onClick={() => void handleExportCsv("monthly")}
              disabled={isExporting}
              className="rounded-lg bg-blue-600 px-4 py-2 text-white text-sm font-medium hover:bg-blue-700 disabled:opacity-60 disabled:cursor-not-allowed"
            >
              {isExporting ? "Exporting..." : "Export Monthly CSV"}
            </button>
            <button
              type="button"
              onClick={() => void handleExportCsv("low-stock")}
              disabled={isExporting}
              className="rounded-lg bg-amber-600 px-4 py-2 text-white text-sm font-medium hover:bg-amber-700 disabled:opacity-60 disabled:cursor-not-allowed"
            >
              {isExporting ? "Exporting..." : "Export Low-Stock CSV"}
            </button>
          </div>
        </div>

        {error && <div className="mb-4 rounded-lg bg-red-100 px-4 py-3 text-red-700">{error}</div>}

        <div className="mb-6 rounded-2xl border border-slate-200 bg-white p-4 shadow-sm">
          <h3 className="text-lg font-semibold text-slate-800 mb-4">Monthly Sales (Bar Chart)</h3>

          {!isLoading && monthlyReport.length === 0 ? (
            <p className="text-slate-500 text-sm">No monthly sales data found.</p>
          ) : (
            <div className="h-72 border border-slate-100 rounded-xl p-4">
              <div className="h-full flex items-end gap-3 overflow-x-auto">
                {monthlyReport.map((item) => {
                  const heightPercent = maxMonthlyAmount > 0 ? (item.totalAmount / maxMonthlyAmount) * 100 : 0;
                  return (
                    <div key={item.month} className="min-w-[92px] h-full flex flex-col justify-end items-center gap-2">
                      <span className="text-xs text-slate-500 whitespace-nowrap">Rs. {item.totalAmount.toFixed(0)}</span>
                      <div className="w-12 bg-blue-500 rounded-t-md" style={{ height: `${Math.max(heightPercent, 2)}%` }} />
                      <span className="text-xs text-slate-600">
                        {new Date(item.month).toLocaleDateString(undefined, {
                          year: "numeric",
                          month: "short",
                        })}
                      </span>
                    </div>
                  );
                })}
              </div>
            </div>
          )}
        </div>

        <div className="mb-6 rounded-2xl border border-slate-200 bg-white p-4 shadow-sm overflow-x-auto">
          <h3 className="text-lg font-semibold text-slate-800 mb-4">Monthly Totals</h3>
          <table className="w-full text-sm">
            <thead>
              <tr className="text-left border-b border-slate-200 text-slate-500">
                <th className="py-2">Month</th>
                <th className="py-2">Total sales</th>
              </tr>
            </thead>
            <tbody>
              {!isLoading && monthlyReport.length === 0 && (
                <tr>
                  <td colSpan={2} className="py-5 text-slate-500">
                    No monthly totals found.
                  </td>
                </tr>
              )}
              {monthlyReport.map((item) => (
                <tr key={`table-${item.month}`} className="border-b border-slate-100">
                  <td className="py-2">
                    {new Date(item.month).toLocaleDateString(undefined, {
                      year: "numeric",
                      month: "long",
                    })}
                  </td>
                  <td className="py-2 font-medium">Rs. {item.totalAmount.toFixed(2)}</td>
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr className="border-t border-slate-200">
                <td className="py-2 font-semibold">Grand Total</td>
                <td className="py-2 font-semibold">Rs. {totalMonthlySales.toFixed(2)}</td>
              </tr>
            </tfoot>
          </table>
        </div>

        <div className="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm overflow-x-auto">
          <h3 className="text-lg font-semibold text-slate-800 mb-4">Daily Totals</h3>
          <table className="w-full text-sm">
            <thead>
              <tr className="text-left border-b border-slate-200 text-slate-500">
                <th className="py-2">Date</th>
                <th className="py-2">Total sales</th>
              </tr>
            </thead>
            <tbody>
              {!isLoading && report.length === 0 && (
                <tr>
                  <td colSpan={2} className="py-5 text-slate-500">
                    No sales data found.
                  </td>
                </tr>
              )}

              {report.map((item) => (
                <tr key={item.date} className="border-b border-slate-100">
                  <td className="py-2">{new Date(item.date).toLocaleDateString()}</td>
                  <td className="py-2 font-medium">Rs. {item.totalAmount.toFixed(2)}</td>
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr className="border-t border-slate-200">
                <td className="py-2 font-semibold">Grand Total</td>
                <td className="py-2 font-semibold">Rs. {totalSales.toFixed(2)}</td>
              </tr>
            </tfoot>
          </table>
        </div>

        <div className="mt-6 rounded-2xl border border-slate-200 bg-white p-4 shadow-sm overflow-x-auto">
          <h3 className="text-lg font-semibold text-slate-800 mb-4">Low-Stock Products</h3>
          <table className="w-full text-sm">
            <thead>
              <tr className="text-left border-b border-slate-200 text-slate-500">
                <th className="py-2">Product</th>
                <th className="py-2">SKU</th>
                <th className="py-2">Category</th>
                <th className="py-2">Quantity</th>
              </tr>
            </thead>
            <tbody>
              {!isLoading && lowStockReport.length === 0 && (
                <tr>
                  <td colSpan={4} className="py-5 text-slate-500">
                    No low-stock products found.
                  </td>
                </tr>
              )}
              {lowStockReport.map((item) => (
                <tr key={item.id} className="border-b border-slate-100">
                  <td className="py-2">{item.name}</td>
                  <td className="py-2">{item.sku}</td>
                  <td className="py-2">{item.category}</td>
                  <td className="py-2 font-medium text-red-600">{item.quantity}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};

export default DailySalesReportPage;
