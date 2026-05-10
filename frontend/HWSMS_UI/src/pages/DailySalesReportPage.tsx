import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import Navbar from "../components/Navbar";
import { getApiErrorMessage, isForbidden, isUnauthorized } from "../services/apiError";
import { getCurrentUser, logout } from "../services/authService";
import { getProducts, type Product } from "../services/productService";
import {
  exportReportCsv,
  getSalesAnalytics,
  getReportsSummary,
  type LowStockReportItem,
  type ReportExportType,
  type SalesAnalytics,
} from "../services/reportService";

type AnalyticsFilters = {
  fromDate: string;
  toDate: string;
  productId: string;
  category: string;
};

const emptyAnalytics: SalesAnalytics = {
  totalSales: 0,
  totalCost: 0,
  totalProfit: 0,
  dailyTrends: [],
  monthlyTrends: [],
};

const formatCurrency = (amount: number) => `Rs. ${amount.toFixed(2)}`;

const DailySalesReportPage = () => {
  const navigate = useNavigate();
  const [lowStockReport, setLowStockReport] = useState<LowStockReportItem[]>([]);
  const [analytics, setAnalytics] = useState<SalesAnalytics>(emptyAnalytics);
  const [products, setProducts] = useState<Product[]>([]);
  const [filters, setFilters] = useState<AnalyticsFilters>({
    fromDate: "",
    toDate: "",
    productId: "",
    category: "",
  });
  const [isLoading, setIsLoading] = useState(false);
  const [isExporting, setIsExporting] = useState(false);
  const [error, setError] = useState("");
  const user = getCurrentUser();

  const handleLogout = () => {
    logout();
    navigate("/login", { replace: true });
  };

  const handleRequestError = (err: unknown, fallbackMessage: string) => {
    if (isUnauthorized(err)) {
      handleLogout();
      return true;
    }

    if (isForbidden(err)) {
      navigate("/access-denied", { replace: true });
      return true;
    }

    setError(getApiErrorMessage(err, fallbackMessage));
    return false;
  };

  const loadReport = async (nextFilters: AnalyticsFilters) => {
    setError("");
    setIsLoading(true);

    try {
      const [summary, analyticsResponse] = await Promise.all([
        getReportsSummary(),
        getSalesAnalytics({
          fromDate: nextFilters.fromDate || undefined,
          toDate: nextFilters.toDate || undefined,
          productId: nextFilters.productId ? Number(nextFilters.productId) : undefined,
          category: nextFilters.category || undefined,
        }),
      ]);

      setLowStockReport(summary.lowStock);
      setAnalytics(analyticsResponse);
    } catch (err) {
      handleRequestError(err, "Failed to load sales analytics dashboard.");
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    const loadProducts = async () => {
      try {
        const response = await getProducts();
        setProducts(response.data);
      } catch (err) {
        handleRequestError(err, "Failed to load product filters.");
      }
    };

    void loadProducts();
  }, []);

  useEffect(() => {
    void loadReport(filters);
  }, [filters]);

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
      handleRequestError(err, "Failed to export CSV report.");
    } finally {
      setIsExporting(false);
    }
  };

  const categories = useMemo(
    () => Array.from(new Set(products.map((product) => product.category).filter(Boolean))).sort(),
    [products],
  );
  const maxDailySales = useMemo(
    () => Math.max(...analytics.dailyTrends.map((item) => item.sales), 0),
    [analytics.dailyTrends],
  );
  const maxAnalyticsMonthlySales = useMemo(
    () => Math.max(...analytics.monthlyTrends.map((item) => item.sales), 0),
    [analytics.monthlyTrends],
  );
  const profitMargin = analytics.totalSales > 0 ? (analytics.totalProfit / analytics.totalSales) * 100 : 0;
  const dailyPoints = useMemo(() => {
    if (analytics.dailyTrends.length === 0 || maxDailySales <= 0) {
      return "";
    }

    return analytics.dailyTrends
      .map((item, index) => {
        const x = analytics.dailyTrends.length === 1 ? 50 : (index / (analytics.dailyTrends.length - 1)) * 100;
        const y = 92 - (item.sales / maxDailySales) * 76;
        return `${x},${y}`;
      })
      .join(" ");
  }, [analytics.dailyTrends, maxDailySales]);

  const handleFilterChange = (name: keyof AnalyticsFilters, value: string) => {
    setFilters((current) => ({
      ...current,
      [name]: value,
    }));
  };

  const clearFilters = () => {
    setFilters({
      fromDate: "",
      toDate: "",
      productId: "",
      category: "",
    });
  };

  return (
    <div className="hw-page">
      <Navbar username={user?.username} onLogout={handleLogout} />

      <div className="hw-shell">
        <div className="mb-6 flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
          <div>
            <h2 className="hw-title">Sales Analytics Dashboard</h2>
            <p className="hw-subtitle">Business performance, profit, and sales trends for managers and admins.</p>
          </div>
          <div className="flex flex-wrap gap-3">
            <button
              type="button"
              onClick={() => void handleExportCsv("daily")}
              disabled={isExporting}
              className="hw-btn-secondary disabled:cursor-not-allowed disabled:opacity-60"
            >
              {isExporting ? "Exporting..." : "Export Daily CSV"}
            </button>
            <button
              type="button"
              onClick={() => void handleExportCsv("monthly")}
              disabled={isExporting}
              className="hw-btn-primary disabled:cursor-not-allowed disabled:opacity-60"
            >
              {isExporting ? "Exporting..." : "Export Monthly CSV"}
            </button>
            <button
              type="button"
              onClick={() => void handleExportCsv("low-stock")}
              disabled={isExporting}
              className="hw-btn-ghost disabled:cursor-not-allowed disabled:opacity-60"
            >
              {isExporting ? "Exporting..." : "Export Low-Stock CSV"}
            </button>
          </div>
        </div>

        {error && <div className="mb-4 rounded-lg bg-red-100 px-4 py-3 text-red-700">{error}</div>}

        <div className="hw-card mb-6 p-4">
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-5">
            <label className="text-sm font-medium text-slate-700">
              From date
              <input
                type="date"
                value={filters.fromDate}
                onChange={(event) => handleFilterChange("fromDate", event.target.value)}
                className="hw-input mt-1"
              />
            </label>
            <label className="text-sm font-medium text-slate-700">
              To date
              <input
                type="date"
                value={filters.toDate}
                onChange={(event) => handleFilterChange("toDate", event.target.value)}
                className="hw-input mt-1"
              />
            </label>
            <label className="text-sm font-medium text-slate-700">
              Product
              <select
                value={filters.productId}
                onChange={(event) => handleFilterChange("productId", event.target.value)}
                className="hw-input mt-1"
              >
                <option value="">All products</option>
                {products.map((product) => (
                  <option key={product.id} value={product.id}>
                    {product.name}
                  </option>
                ))}
              </select>
            </label>
            <label className="text-sm font-medium text-slate-700">
              Category
              <select
                value={filters.category}
                onChange={(event) => handleFilterChange("category", event.target.value)}
                className="hw-input mt-1"
              >
                <option value="">All categories</option>
                {categories.map((category) => (
                  <option key={category} value={category}>
                    {category}
                  </option>
                ))}
              </select>
            </label>
            <div className="flex items-end">
              <button
                type="button"
                onClick={clearFilters}
                className="hw-btn-ghost w-full"
              >
                Clear filters
              </button>
            </div>
          </div>
        </div>

        <div className="mb-6 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          <div className="hw-kpi">
            <p className="text-sm font-medium text-slate-500">Total sales</p>
            <p className="mt-2 text-2xl font-bold text-slate-900">{formatCurrency(analytics.totalSales)}</p>
          </div>
          <div className="hw-kpi">
            <p className="text-sm font-medium text-slate-500">Estimated cost</p>
            <p className="mt-2 text-2xl font-bold text-amber-700">{formatCurrency(analytics.totalCost)}</p>
          </div>
          <div className="hw-kpi">
            <p className="text-sm font-medium text-slate-500">Profit</p>
            <p className="mt-2 text-2xl font-bold text-emerald-700">{formatCurrency(analytics.totalProfit)}</p>
          </div>
          <div className="hw-kpi">
            <p className="text-sm font-medium text-slate-500">Profit margin</p>
            <p className="mt-2 text-2xl font-bold text-[#1f6b8c]">{profitMargin.toFixed(1)}%</p>
          </div>
        </div>

        <div className="mb-6 grid gap-6 xl:grid-cols-2">
          <div className="hw-card p-4">
            <div className="mb-4 flex items-center justify-between">
              <h3 className="text-lg font-semibold text-slate-800">Daily Sales Trend</h3>
              {isLoading && <span className="text-sm text-slate-500">Loading...</span>}
            </div>

            {!isLoading && analytics.dailyTrends.length === 0 ? (
              <p className="text-sm text-slate-500">No sales found for the selected filters.</p>
            ) : (
              <div className="h-72 rounded-xl border border-slate-100 p-4">
                <svg viewBox="0 0 100 100" preserveAspectRatio="none" className="h-52 w-full overflow-visible">
                  <line x1="0" y1="92" x2="100" y2="92" stroke="#cbd5e1" strokeWidth="0.6" />
                  <line x1="0" y1="16" x2="100" y2="16" stroke="#e2e8f0" strokeWidth="0.4" />
                  <polyline fill="none" stroke="#2563eb" strokeWidth="2.8" points={dailyPoints} />
                  {analytics.dailyTrends.map((item, index) => {
                    const x = analytics.dailyTrends.length === 1 ? 50 : (index / (analytics.dailyTrends.length - 1)) * 100;
                    const y = maxDailySales > 0 ? 92 - (item.sales / maxDailySales) * 76 : 92;
                    return <circle key={item.date} cx={x} cy={y} r="1.8" fill="#2563eb" />;
                  })}
                </svg>
                <div className="mt-3 flex justify-between gap-3 text-xs text-slate-500">
                  <span>{analytics.dailyTrends[0] ? new Date(analytics.dailyTrends[0].date).toLocaleDateString() : ""}</span>
                  <span>
                    {analytics.dailyTrends.at(-1)
                      ? new Date(analytics.dailyTrends.at(-1)!.date).toLocaleDateString()
                      : ""}
                  </span>
                </div>
              </div>
            )}
          </div>

          <div className="hw-card p-4">
            <div className="mb-4 flex items-center justify-between">
              <h3 className="text-lg font-semibold text-slate-800">Monthly Sales vs Profit</h3>
              {isLoading && <span className="text-sm text-slate-500">Loading...</span>}
            </div>

            {!isLoading && analytics.monthlyTrends.length === 0 ? (
              <p className="text-sm text-slate-500">No monthly analytics found for the selected filters.</p>
            ) : (
              <div className="h-72 rounded-xl border border-slate-100 p-4">
                <div className="flex h-full items-end gap-4 overflow-x-auto">
                  {analytics.monthlyTrends.map((item) => {
                    const salesHeight = maxAnalyticsMonthlySales > 0 ? (item.sales / maxAnalyticsMonthlySales) * 100 : 0;
                    const profitHeight = maxAnalyticsMonthlySales > 0 ? (item.profit / maxAnalyticsMonthlySales) * 100 : 0;

                    return (
                      <div key={item.month} className="flex h-full min-w-[100px] flex-col items-center justify-end gap-2">
                        <span className="text-xs text-slate-500">{formatCurrency(item.sales)}</span>
                        <div className="flex h-44 items-end gap-1">
                          <div className="w-6 rounded-t bg-blue-500" style={{ height: `${Math.max(salesHeight, 2)}%` }} />
                          <div className="w-6 rounded-t bg-emerald-500" style={{ height: `${Math.max(profitHeight, 2)}%` }} />
                        </div>
                        <span className="text-xs text-slate-600">
                          {new Date(item.month).toLocaleDateString(undefined, { month: "short", year: "numeric" })}
                        </span>
                      </div>
                    );
                  })}
                </div>
              </div>
            )}
            <div className="mt-3 flex gap-4 text-xs text-slate-600">
              <span><span className="mr-1 inline-block h-3 w-3 rounded-sm bg-blue-500" />Sales</span>
              <span><span className="mr-1 inline-block h-3 w-3 rounded-sm bg-emerald-500" />Profit</span>
            </div>
          </div>
        </div>

        <div className="hw-card mb-6 overflow-x-auto p-4">
          <h3 className="text-lg font-semibold text-slate-800 mb-4">Monthly Sales and Profit</h3>
          <table className="hw-table">
            <thead>
              <tr className="text-left">
                <th className="py-2">Month</th>
                <th className="py-2">Sales</th>
                <th className="py-2">Cost</th>
                <th className="py-2">Profit</th>
              </tr>
            </thead>
            <tbody>
              {!isLoading && analytics.monthlyTrends.length === 0 && (
                <tr>
                  <td colSpan={4} className="py-5 text-slate-500">
                    No monthly totals found.
                  </td>
                </tr>
              )}
              {analytics.monthlyTrends.map((item) => (
                <tr key={`table-${item.month}`}>
                  <td className="py-2">
                    {new Date(item.month).toLocaleDateString(undefined, {
                      year: "numeric",
                      month: "long",
                    })}
                  </td>
                  <td className="py-2 font-medium">{formatCurrency(item.sales)}</td>
                  <td className="py-2">{formatCurrency(item.cost)}</td>
                  <td className="py-2 font-semibold text-emerald-700">{formatCurrency(item.profit)}</td>
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr className="border-t border-slate-200">
                <td className="py-2 font-semibold">Grand Total</td>
                <td className="py-2 font-semibold">{formatCurrency(analytics.totalSales)}</td>
                <td className="py-2 font-semibold">{formatCurrency(analytics.totalCost)}</td>
                <td className="py-2 font-semibold text-emerald-700">{formatCurrency(analytics.totalProfit)}</td>
              </tr>
            </tfoot>
          </table>
        </div>

        <div className="hw-card overflow-x-auto p-4">
          <h3 className="text-lg font-semibold text-slate-800 mb-4">Daily Sales and Profit</h3>
          <table className="hw-table">
            <thead>
              <tr className="text-left">
                <th className="py-2">Date</th>
                <th className="py-2">Sales</th>
                <th className="py-2">Cost</th>
                <th className="py-2">Profit</th>
              </tr>
            </thead>
            <tbody>
              {!isLoading && analytics.dailyTrends.length === 0 && (
                <tr>
                  <td colSpan={4} className="py-5 text-slate-500">
                    No sales data found.
                  </td>
                </tr>
              )}

              {analytics.dailyTrends.map((item) => (
                <tr key={item.date}>
                  <td className="py-2">{new Date(item.date).toLocaleDateString()}</td>
                  <td className="py-2 font-medium">{formatCurrency(item.sales)}</td>
                  <td className="py-2">{formatCurrency(item.cost)}</td>
                  <td className="py-2 font-semibold text-emerald-700">{formatCurrency(item.profit)}</td>
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr className="border-t border-slate-200">
                <td className="py-2 font-semibold">Grand Total</td>
                <td className="py-2 font-semibold">{formatCurrency(analytics.totalSales)}</td>
                <td className="py-2 font-semibold">{formatCurrency(analytics.totalCost)}</td>
                <td className="py-2 font-semibold text-emerald-700">{formatCurrency(analytics.totalProfit)}</td>
              </tr>
            </tfoot>
          </table>
        </div>

        <div className="hw-card mt-6 overflow-x-auto p-4">
          <h3 className="text-lg font-semibold text-slate-800 mb-4">Low-Stock Products</h3>
          <table className="hw-table">
            <thead>
              <tr className="text-left">
                <th className="py-2">Product</th>
                <th className="py-2">SKU</th>
                <th className="py-2">Category</th>
                <th className="py-2">Supplier</th>
                <th className="py-2">Quantity</th>
              </tr>
            </thead>
            <tbody>
              {!isLoading && lowStockReport.length === 0 && (
                <tr>
                  <td colSpan={5} className="py-5 text-slate-500">
                    No low-stock products found.
                  </td>
                </tr>
              )}
              {lowStockReport.map((item) => (
                <tr key={item.id}>
                  <td className="py-2">{item.name}</td>
                  <td className="py-2">{item.sku}</td>
                  <td className="py-2">{item.category}</td>
                  <td className="py-2 text-slate-600">{item.supplierName ?? "-"}</td>
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
