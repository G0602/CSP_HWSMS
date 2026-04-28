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
    <div className="hw-page">
      <Navbar onLogout={handleLogout} />

      <div className="hw-shell">
        <div className="mb-6">
          <h2 className="hw-title">Transaction History</h2>
          <p className="hw-subtitle">Review completed sales and inspect detailed line items.</p>
        </div>

        {error && <div className="mb-4 rounded-lg bg-red-100 px-4 py-3 text-red-700">{error}</div>}

        <div className="hw-card mb-4 p-4">
          <div className="grid grid-cols-1 gap-3 md:grid-cols-4">
            <input
              type="number"
              placeholder="Transaction ID"
              value={transactionIdFilter}
              onChange={(e) => setTransactionIdFilter(e.target.value)}
              className="hw-input"
            />
            <input
              type="date"
              value={fromDateFilter}
              onChange={(e) => setFromDateFilter(e.target.value)}
              className="hw-input"
            />
            <input
              type="date"
              value={toDateFilter}
              onChange={(e) => setToDateFilter(e.target.value)}
              className="hw-input"
            />
            <button
              type="button"
              onClick={() => {
                void loadHistory();
              }}
              className="hw-btn-primary"
            >
              Apply Filters
            </button>
          </div>
        </div>

        <div className="hw-card overflow-x-auto p-4">
          <table className="hw-table">
            <thead>
              <tr className="text-left">
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
                <tr key={transaction.saleId}>
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
                        className="hw-btn-ghost px-3 py-1.5"
                      >
                        View
                      </button>
                      <button
                        type="button"
                        onClick={() => navigate(`/transactions/${transaction.saleId}/invoice`)}
                        className="hw-btn-primary px-3 py-1.5"
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
