import axios from "axios";
import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import Navbar from "../components/Navbar";
import { logout } from "../services/authService";
import { getInvoiceByTransactionId, type InvoiceResponse } from "../services/invoiceService";

const InvoicePreviewPage = () => {
  const navigate = useNavigate();
  const params = useParams();

  const [invoice, setInvoice] = useState<InvoiceResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");

  const handleLogout = () => {
    logout();
    navigate("/login", { replace: true });
  };

  useEffect(() => {
    const loadInvoice = async () => {
      setError("");
      setIsLoading(true);

      const transactionId = Number(params.transactionId);
      if (!Number.isInteger(transactionId) || transactionId <= 0) {
        setError("Invalid transaction ID.");
        setIsLoading(false);
        return;
      }

      try {
        const data = await getInvoiceByTransactionId(transactionId);
        setInvoice(data);
      } catch (err) {
        if (axios.isAxiosError(err) && err.response?.status === 401) {
          handleLogout();
          return;
        }

        if (axios.isAxiosError(err) && err.response?.status === 403) {
          navigate("/access-denied", { replace: true });
          return;
        }

        if (axios.isAxiosError(err) && err.response?.status === 404) {
          setError("Invoice not found for this transaction.");
        } else {
          setError("Failed to load invoice.");
        }
      } finally {
        setIsLoading(false);
      }
    };

    void loadInvoice();
  }, [params.transactionId]);

  return (
    <div className="hw-page">
      <Navbar onLogout={handleLogout} />

      <div className="mx-auto max-w-4xl p-6 lg:p-10">
        {isLoading && <div className="hw-card p-4">Loading invoice...</div>}

        {error && <div className="rounded-lg bg-red-100 px-4 py-3 text-red-700">{error}</div>}

        {!isLoading && !error && invoice && (
          <div className="hw-card p-6">
            <div className="flex items-start justify-between border-b border-slate-200 pb-4">
              <div>
                <h1 className="text-3xl font-bold text-slate-900">Invoice</h1>
                <p className="text-slate-600 mt-1">{invoice.invoiceNumber}</p>
              </div>

              <button
                type="button"
                onClick={() => window.print()}
                className="hw-btn-primary"
              >
                Print
              </button>
            </div>

            <div className="mt-5 grid grid-cols-1 md:grid-cols-2 gap-4 text-sm">
              <div>
                <p className="text-slate-500">Transaction ID</p>
                <p className="font-semibold text-slate-900">#{invoice.transactionId}</p>
              </div>
              <div>
                <p className="text-slate-500">Sold At</p>
                <p className="font-semibold text-slate-900">{new Date(invoice.soldAt).toLocaleString()}</p>
              </div>
              <div>
                <p className="text-slate-500">Sold By</p>
                <p className="font-semibold text-slate-900">{invoice.soldBy}</p>
              </div>
              <div>
                <p className="text-slate-500">Items</p>
                <p className="font-semibold text-slate-900">{invoice.items.length}</p>
              </div>
            </div>

            <div className="mt-6 overflow-x-auto">
              <table className="hw-table">
                <thead>
                  <tr className="text-left">
                    <th className="py-2">Product</th>
                    <th className="py-2">SKU</th>
                    <th className="py-2">Price</th>
                    <th className="py-2">Qty</th>
                    <th className="py-2">Line Total</th>
                  </tr>
                </thead>
                <tbody>
                  {invoice.items.map((item) => (
                    <tr key={`${item.productId}-${item.sku}-${item.quantity}`}>
                      <td className="py-2">{item.productName}</td>
                      <td className="py-2">{item.sku}</td>
                      <td className="py-2">Rs. {item.unitPrice.toFixed(2)}</td>
                      <td className="py-2">{item.quantity}</td>
                      <td className="py-2 font-medium">Rs. {item.lineSubtotal.toFixed(2)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <div className="mt-6 flex justify-end">
              <div className="w-full max-w-xs space-y-2 text-sm">
                <div className="flex justify-between">
                  <span className="text-slate-500">Subtotal</span>
                  <span className="font-medium">Rs. {invoice.subtotal.toFixed(2)}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-slate-500">Tax ({(invoice.taxRate * 100).toFixed(0)}%)</span>
                  <span className="font-medium">Rs. {invoice.taxAmount.toFixed(2)}</span>
                </div>
                <div className="flex justify-between border-t border-slate-200 pt-2">
                  <span className="text-slate-900 font-semibold">Grand Total</span>
                  <span className="text-[#c2500f] text-xl font-bold">Rs. {invoice.grandTotal.toFixed(2)}</span>
                </div>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default InvoicePreviewPage;
