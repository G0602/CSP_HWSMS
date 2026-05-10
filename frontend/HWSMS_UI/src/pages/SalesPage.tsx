import { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import Navbar from "../components/Navbar";
import { getApiErrorMessage, isForbidden, isUnauthorized } from "../services/apiError";
import ProductSearchSelect from "../components/ProductSearchSelect";
import { logout } from "../services/authService";
import { createSale } from "../services/saleService";
import type { Product } from "../services/productService";

type CartItem = {
  productId: number;
  name: string;
  sku: string;
  unitPrice: number;
  quantity: number;
  stockAtAdd: number;
};

const SalesPage = () => {
  const navigate = useNavigate();

  const [items, setItems] = useState<CartItem[]>([]);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [successMessage, setSuccessMessage] = useState("");
  const [errorMessage, setErrorMessage] = useState("");
  const [stockRefreshSignal, setStockRefreshSignal] = useState(0);

  const handleLogout = () => {
    logout();
    navigate("/login", { replace: true });
  };

  const onAddItem = (product: Product, quantity: number) => {
    setSuccessMessage("");
    setErrorMessage("");

    if (quantity <= 0) {
      setErrorMessage("Quantity must be greater than zero.");
      return;
    }

    if (quantity > product.quantity) {
      setErrorMessage(`Requested quantity exceeds stock for ${product.name}.`);
      return;
    }

    setItems((prev) => [
      ...prev,
      {
        productId: product.id,
        name: product.name,
        sku: product.sku,
        unitPrice: product.price,
        quantity,
        stockAtAdd: product.quantity,
      },
    ]);
  };

  const onUpdateQuantity = (productId: number, quantity: number) => {
    setItems((prev) =>
      prev.map((item) => {
        if (item.productId !== productId) {
          return item;
        }

        const safeQuantity = Math.min(Math.max(quantity, 1), item.stockAtAdd);
        return { ...item, quantity: safeQuantity };
      }),
    );
  };

  const onRemoveItem = (productId: number) => {
    setItems((prev) => prev.filter((item) => item.productId !== productId));
  };

  const lineTotals = useMemo(
    () => items.map((item) => ({ ...item, lineSubtotal: item.unitPrice * item.quantity })),
    [items],
  );

  const saleTotal = lineTotals.reduce((sum, item) => sum + item.lineSubtotal, 0);

  const submitSale = async () => {
    setSuccessMessage("");
    setErrorMessage("");

    if (items.length === 0) {
      setErrorMessage("Please add at least one item to the cart.");
      return;
    }

    const hasInvalidQuantity = items.some((item) => item.quantity <= 0 || item.quantity > item.stockAtAdd);
    if (hasInvalidQuantity) {
      setErrorMessage("Please review item quantities before confirming sale.");
      return;
    }

    setIsSubmitting(true);

    try {
      const result = await createSale({
        items: items.map((item) => ({ productId: item.productId, quantity: item.quantity })),
      });

      setItems([]);
      setStockRefreshSignal((prev) => prev + 1);
      setSuccessMessage(`Sale #${result.saleId} completed successfully. Total: Rs. ${result.totalAmount.toFixed(2)}`);
    } catch (err) {
      if (isUnauthorized(err)) {
        handleLogout();
        return;
      }

      if (isForbidden(err)) {
        navigate("/access-denied", { replace: true });
        return;
      }

      setErrorMessage(getApiErrorMessage(err, "Failed to complete sale transaction."));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="hw-page">
      <Navbar onLogout={handleLogout} />

      <div className="hw-shell">
        <div className="mb-6 flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
          <div>
            <h2 className="hw-title">Sales Transaction</h2>
            <p className="hw-subtitle">Search products, add quantities, and confirm checkout.</p>
          </div>
          <div className="grid grid-cols-2 gap-3 sm:min-w-[360px]">
            <div className="hw-kpi">
              <p className="text-xs font-medium uppercase tracking-wide text-slate-500">Cart items</p>
              <p className="mt-1 text-2xl font-bold text-slate-900">{lineTotals.length}</p>
            </div>
            <div className="hw-kpi">
              <p className="text-xs font-medium uppercase tracking-wide text-slate-500">Sale total</p>
              <p className="mt-1 text-2xl font-bold text-[#c2500f]">Rs. {saleTotal.toFixed(2)}</p>
            </div>
          </div>
        </div>

        {successMessage && <div className="mb-4 rounded-lg bg-green-100 px-4 py-3 text-green-800">{successMessage}</div>}
        {errorMessage && <div className="mb-4 rounded-lg bg-red-100 px-4 py-3 text-red-700">{errorMessage}</div>}

        <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
          <ProductSearchSelect
            selectedProductIds={items.map((item) => item.productId)}
            onAddItem={onAddItem}
            refreshSignal={stockRefreshSignal}
          />

          <div className="hw-card">
            <h3 className="text-xl font-semibold text-slate-900">Cart</h3>

            <div className="mt-4 overflow-x-auto">
              <table className="hw-table">
                <thead>
                  <tr className="text-left">
                    <th className="py-2">Item</th>
                    <th className="py-2">Price</th>
                    <th className="py-2">Qty</th>
                    <th className="py-2">Subtotal</th>
                    <th className="py-2">Action</th>
                  </tr>
                </thead>
                <tbody>
                  {lineTotals.length === 0 && (
                    <tr>
                      <td colSpan={5} className="py-4 text-slate-500">
                        No items selected yet.
                      </td>
                    </tr>
                  )}

                  {lineTotals.map((item) => (
                    <tr key={item.productId}>
                      <td className="py-3">
                        <div className="font-medium text-slate-900">{item.name}</div>
                        <div className="text-xs text-slate-500">{item.sku}</div>
                      </td>
                      <td className="py-3">Rs. {item.unitPrice.toFixed(2)}</td>
                      <td className="py-3">
                        <input
                          type="number"
                          min={1}
                          max={item.stockAtAdd}
                          value={item.quantity}
                          onChange={(e) => onUpdateQuantity(item.productId, Number(e.target.value))}
                          className="hw-input w-20 px-2 py-1"
                        />
                      </td>
                      <td className="py-3 font-semibold text-slate-800">Rs. {item.lineSubtotal.toFixed(2)}</td>
                      <td className="py-3">
                        <button
                          type="button"
                          onClick={() => onRemoveItem(item.productId)}
                          className="hw-btn-ghost px-3 py-1"
                        >
                          Remove
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <div className="mt-6 flex items-center justify-between border-t border-slate-200 pt-4">
              <div>
                <p className="text-sm text-slate-500">Total Amount</p>
                <p className="text-2xl font-bold text-[#c2500f]">Rs. {saleTotal.toFixed(2)}</p>
              </div>

              <button
                type="button"
                disabled={isSubmitting || lineTotals.length === 0}
                onClick={() => {
                  void submitSale();
                }}
                className="hw-btn-primary px-5 py-2.5 disabled:bg-slate-400"
              >
                {isSubmitting ? "Saving..." : "Confirm Sale"}
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default SalesPage;
