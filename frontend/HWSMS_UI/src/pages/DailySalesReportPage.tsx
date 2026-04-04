import axios from "axios";
import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import Navbar from "../components/Navbar";
import { getCurrentUser, logout } from "../services/authService";
import { getDailySalesReport, type DailySalesReportItem } from "../services/reportService";

const DailySalesReportPage = () => {
  const navigate = useNavigate();
  const [report, setReport] = useState<DailySalesReportItem[]>([]);
  const [isLoading, setIsLoading] = useState(false);
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
      const data = await getDailySalesReport();
      setReport(data);
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

  const totalSales = useMemo(() => report.reduce((sum, item) => sum + item.totalAmount, 0), [report]);

  return (
    <div className="min-h-screen bg-slate-50">
      <Navbar username={user?.username} onLogout={handleLogout} />

      <div className="mx-auto max-w-5xl p-6 lg:p-10">
        <div className="mb-6">
          <h2 className="text-3xl font-bold text-slate-900">Daily Sales Report</h2>
          <p className="text-slate-600 mt-1">Total sales grouped by date.</p>
        </div>

        {error && <div className="mb-4 rounded-lg bg-red-100 px-4 py-3 text-red-700">{error}</div>}

        <div className="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm overflow-x-auto">
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
      </div>
    </div>
  );
};

export default DailySalesReportPage;
