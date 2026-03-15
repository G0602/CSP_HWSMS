import axios from "axios";
import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import Navbar from "../components/Navbar";
import TransactionDetailModal from "../components/TransactionDetailModal";
import { logout } from "../services/authService";
import {
  getTransactionDetails,
  getTransactionHistory,
  type TransactionDetails,
  type TransactionHistoryItem,
} from "../services/transactionService";

const TransactionHistoryPage = () => {
  const navigate = useNavigate();

  const [history, setHistory] = useState<TransactionHistoryItem[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState("");

  const [transactionIdFilter, setTransactionIdFilter] = useState("");
  const [fromDateFilter, setFromDateFilter] = useState("");
  const [toDateFilter, setToDateFilter] = useState("");

  const [selectedTransaction, setSelectedTransaction] = useState<TransactionDetails | null>(null);
  const [isDetailOpen, setIsDetailOpen] = useState(false);

  const handleLogout = () => {
    logout();
    navigate("/login", { replace: true });
  };

  const loadHistory = async () => {
    setError("");
    setIsLoading(true);

    try {
      const transactionId = transactionIdFilter.trim() ? Number(transactionIdFilter.trim()) : undefined;

      const data = await getTransactionHistory({
        transactionId: Number.isFinite(transactionId ?? NaN) ? transactionId : undefined,
        fromDate: fromDateFilter || undefined,
        toDate: toDateFilter || undefined,
        limit: 200,
      });

      setHistory(data);
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.status === 401) {
        handleLogout();
        return;
      }

      if (axios.isAxiosError(err) && err.response?.status === 403) {
        navigate("/access-denied", { replace: true });
        return;
      }

      setError("Failed to load transaction history.");
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void loadHistory();
  }, []);

  const openDetails = async (saleId: number) => {
    setError("");
    try {
      const details = await getTransactionDetails(saleId);
      setSelectedTransaction(details);
      setIsDetailOpen(true);
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.status === 401) {
        handleLogout();
        return;
      }

      if (axios.isAxiosError(err) && err.response?.status === 403) {
        navigate("/access-denied", { replace: true });
        return;
      }

      setError("Failed to load transaction details.");
    }
  };

  return (
    <div className="min-h-screen bg-slate-50">
      <Navbar onLogout={handleLogout} />

      <div className="mx-auto max-w-7xl p-6 lg:p-10">
        <div className="mb-6">
          <h2 className="text-3xl font-bold text-slate-900">Transaction History</h2>
          <p className="text-slate-600 mt-1">Review completed sales and inspect detailed line items.</p>
        </div>

        {error && <div className="mb-4 rounded-lg bg-red-100 px-4 py-3 text-red-700">{error}</div>}

        <div className="mb-4 rounded-2xl border border-slate-200 bg-white p-4">
          <div className="grid grid-cols-1 gap-3 md:grid-cols-4">
            <input
              type="number"
              placeholder="Transaction ID"
              value={transactionIdFilter}
              onChange={(e) => setTransactionIdFilter(e.target.value)}
              className="rounded-lg border border-slate-300 px-3 py-2 text-sm"
            />
            <input
              type="date"
              value={fromDateFilter}
              onChange={(e) => setFromDateFilter(e.target.value)}
              className="rounded-lg border border-slate-300 px-3 py-2 text-sm"
            />
            <input
              type="date"
              value={toDateFilter}
              onChange={(e) => setToDateFilter(e.target.value)}
              className="rounded-lg border border-slate-300 px-3 py-2 text-sm"
            />
            <button
              type="button"
              onClick={() => {
                void loadHistory();
              }}
              className="rounded-lg bg-blue-600 px-4 py-2 text-white font-medium hover:bg-blue-700"
            >
              Apply Filters
            </button>
          </div>
        </div>

        <div className="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="text-left border-b border-slate-200 text-slate-500">
                <th className="py-2">ID</th>
                <th className="py-2">Date/Time</th>
                <th className="py-2">Sold By</th>
                <th className="py-2">Items</th>
                <th className="py-2">Total</th>
                <th className="py-2">Action</th>
              </tr>
            </thead>
            <tbody>
              {!isLoading && history.length === 0 && (
                <tr>
                  <td colSpan={6} className="py-5 text-slate-500">
                    No transactions found.
                  </td>
                </tr>
              )}

              {history.map((transaction) => (
                <tr key={transaction.saleId} className="border-b border-slate-100">
                  <td className="py-2 font-medium">#{transaction.saleId}</td>
                  <td className="py-2">{new Date(transaction.soldAt).toLocaleString()}</td>
                  <td className="py-2">{transaction.soldBy}</td>
                  <td className="py-2">{transaction.itemCount}</td>
                  <td className="py-2">Rs. {transaction.totalAmount.toFixed(2)}</td>
                  <td className="py-2">
                    <div className="flex gap-2">
                      <button
                        type="button"
                        onClick={() => {
                          void openDetails(transaction.saleId);
                        }}
                        className="rounded-lg bg-slate-100 px-3 py-1.5 hover:bg-slate-200"
                      >
                        View
                      </button>
                      <button
                        type="button"
                        onClick={() => navigate(`/transactions/${transaction.saleId}/invoice`)}
                        className="rounded-lg bg-blue-600 text-white px-3 py-1.5 hover:bg-blue-700"
                      >
                        Invoice
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      <TransactionDetailModal
        transaction={selectedTransaction}
        isOpen={isDetailOpen}
        onClose={() => setIsDetailOpen(false)}
      />
    </div>
  );
};

export default TransactionHistoryPage;
