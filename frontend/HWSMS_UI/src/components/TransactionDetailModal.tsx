import type { TransactionDetails } from "../services/transactionService";

type TransactionDetailModalProps = {
  transaction: TransactionDetails | null;
  isOpen: boolean;
  onClose: () => void;
};

const TransactionDetailModal = ({ transaction, isOpen, onClose }: TransactionDetailModalProps) => {
  if (!isOpen || !transaction) {
    return null;
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/40 p-4">
      <div className="w-full max-w-3xl rounded-2xl bg-white shadow-xl">
        <div className="border-b border-slate-200 px-6 py-4 flex items-center justify-between">
          <div>
            <h3 className="text-xl font-semibold text-slate-900">Transaction #{transaction.saleId}</h3>
            <p className="text-sm text-slate-500">
              {new Date(transaction.soldAt).toLocaleString()} | Sold by {transaction.soldBy}
            </p>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg bg-slate-100 px-3 py-1.5 text-sm hover:bg-slate-200"
          >
            Close
          </button>
        </div>

        <div className="p-6">
          <table className="w-full text-sm">
            <thead>
              <tr className="text-left border-b border-slate-200 text-slate-500">
                <th className="py-2">Product</th>
                <th className="py-2">SKU</th>
                <th className="py-2">Price</th>
                <th className="py-2">Qty</th>
                <th className="py-2">Subtotal</th>
              </tr>
            </thead>
            <tbody>
              {transaction.items.map((item) => (
                <tr key={`${transaction.saleId}-${item.productId}-${item.sku}`} className="border-b border-slate-100">
                  <td className="py-2">{item.productName}</td>
                  <td className="py-2">{item.sku}</td>
                  <td className="py-2">Rs. {item.unitPrice.toFixed(2)}</td>
                  <td className="py-2">{item.quantity}</td>
                  <td className="py-2 font-medium">Rs. {item.lineSubtotal.toFixed(2)}</td>
                </tr>
              ))}
            </tbody>
          </table>

          <div className="mt-4 text-right">
            <p className="text-sm text-slate-500">Total</p>
            <p className="text-2xl font-bold text-blue-700">Rs. {transaction.totalAmount.toFixed(2)}</p>
          </div>
        </div>
      </div>
    </div>
  );
};

export default TransactionDetailModal;
