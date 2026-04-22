import { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import Navbar from "../components/Navbar";
import SupplierFormCard from "../components/SupplierFormCard";
import { CRITICAL_STOCK_THRESHOLD, LOW_STOCK_THRESHOLD } from "../constants/inventory";
import { getApiErrorMessage, isForbidden, isUnauthorized } from "../services/apiError";
import { logout } from "../services/authService";
import { getInventoryProducts, updateProductStock, type InventoryProduct } from "../services/productService";
import { addSupplier, getSuppliers, type Supplier, type SupplierPayload } from "../services/supplierService";

type StockOperation = "increase" | "decrease";

const InventoryPage = () => {
  const navigate = useNavigate();
  const [inventory, setInventory] = useState<InventoryProduct[]>([]);
  const [suppliers, setSuppliers] = useState<Supplier[]>([]);
  const [search, setSearch] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState("");
  const [successMessage, setSuccessMessage] = useState("");
  const [showLowStockPopup, setShowLowStockPopup] = useState(false);
  const [stockModalProduct, setStockModalProduct] = useState<InventoryProduct | null>(null);
  const [stockOperation, setStockOperation] = useState<StockOperation>("increase");
  const [stockAmount, setStockAmount] = useState(1);
  const [stockReason, setStockReason] = useState("");
  const [stockFormError, setStockFormError] = useState("");
  const [isStockUpdating, setIsStockUpdating] = useState(false);
  const [isSupplierSubmitting, setIsSupplierSubmitting] = useState(false);

  const handleLogout = useCallback(() => {
    logout();
    navigate("/login", { replace: true });
  }, [navigate]);

  const loadInventory = useCallback(async () => {
    setError("");
    setIsLoading(true);

    try {
      const data = await getInventoryProducts();
      setInventory(data);
    } catch (err) {
      if (isUnauthorized(err)) {
        handleLogout();
        return;
      }

      if (isForbidden(err)) {
        navigate("/access-denied", { replace: true });
        return;
      }

      setError(getApiErrorMessage(err, "Failed to load inventory."));
    } finally {
      setIsLoading(false);
    }
  }, [handleLogout, navigate]);

  useEffect(() => {
    void loadInventory();
  }, [loadInventory]);

  const loadSuppliers = useCallback(async () => {
    try {
      const data = await getSuppliers();
      setSuppliers(data);
    } catch (err) {
      if (isUnauthorized(err)) {
        handleLogout();
        return;
      }

      if (isForbidden(err)) {
        navigate("/access-denied", { replace: true });
        return;
      }

      setSuppliers([]);
    }
  }, [handleLogout, navigate]);

  useEffect(() => {
    void loadSuppliers();
  }, [loadSuppliers]);

  const filteredInventory = useMemo(
    () =>
      inventory.filter((product) =>
        [product.name, product.category].join(" ").toLowerCase().includes(search.toLowerCase().trim()),
      ),
    [inventory, search],
  );

  const supplierNameById = useMemo(
    () =>
      suppliers.reduce<Record<number, string>>((acc, supplier) => {
        acc[supplier.id] = supplier.name;
        return acc;
      }, {}),
    [suppliers],
  );

  const lowStockCount = filteredInventory.filter(
    (product) => product.isLowStock || product.quantity < LOW_STOCK_THRESHOLD,
  ).length;
  const criticalStockCount = filteredInventory.filter(
    (product) => (product.isLowStock || product.quantity < LOW_STOCK_THRESHOLD) && product.quantity <= CRITICAL_STOCK_THRESHOLD,
  ).length;

  useEffect(() => {
    if (!isLoading && lowStockCount > 0) {
      setShowLowStockPopup(true);
    }
  }, [isLoading, lowStockCount]);

  const priceFormatter = new Intl.NumberFormat("en-LK", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });

  const openStockModal = (product: InventoryProduct) => {
    setStockModalProduct(product);
    setStockOperation("increase");
    setStockAmount(1);
    setStockReason("");
    setStockFormError("");
  };

  const closeStockModal = () => {
    setStockModalProduct(null);
    setStockFormError("");
  };

  const submitStockUpdate = async () => {
    if (!stockModalProduct) {
      return;
    }

    if (!Number.isFinite(stockAmount) || stockAmount <= 0) {
      setStockFormError("Enter a valid amount greater than zero.");
      return;
    }

    const nextQuantity =
      stockOperation === "increase" ? stockModalProduct.quantity + stockAmount : stockModalProduct.quantity - stockAmount;

    if (nextQuantity < 0) {
      setStockFormError("Stock cannot go below zero.");
      return;
    }

    setIsStockUpdating(true);
    setStockFormError("");
    setError("");
    setSuccessMessage("");

    try {
      await updateProductStock(stockModalProduct.id, {
        quantity: nextQuantity,
        reason: stockReason.trim() || undefined,
      });

      setSuccessMessage(`Stock updated for ${stockModalProduct.name}. New quantity: ${nextQuantity}.`);
      closeStockModal();
      await loadInventory();
    } catch (err) {
      if (isUnauthorized(err)) {
        handleLogout();
        return;
      }

      if (isForbidden(err)) {
        navigate("/access-denied", { replace: true });
        return;
      }

      setStockFormError(getApiErrorMessage(err, "Failed to update stock."));
    } finally {
      setIsStockUpdating(false);
    }
  };

  const submitSupplier = async (payload: SupplierPayload) => {
    setIsSupplierSubmitting(true);
    setError("");
    setSuccessMessage("");

    try {
      await addSupplier(payload);
      setSuccessMessage(`Supplier "${payload.name}" added successfully.`);
      await loadSuppliers();
    } catch (err) {
      if (isUnauthorized(err)) {
        handleLogout();
        return;
      }

      if (isForbidden(err)) {
        navigate("/access-denied", { replace: true });
        return;
      }

      setError(getApiErrorMessage(err, "Failed to add supplier."));
    } finally {
      setIsSupplierSubmitting(false);
    }
  };

  return (
    <div className="min-h-screen bg-slate-50">
      <Navbar search={search} onSearchChange={setSearch} onLogout={handleLogout} />

      <div className="mx-auto max-w-7xl p-6 lg:p-10">
        <div className="mb-6 flex flex-wrap items-end justify-between gap-4">
          <div>
            <div className="flex items-center gap-3">
              <h2 className="text-3xl font-bold text-slate-900">Inventory</h2>
              <span className="rounded-full bg-red-100 px-3 py-1 text-xs font-semibold text-red-700">
                {lowStockCount} low stock
              </span>
            </div>
            <p className="mt-1 text-slate-600">Track stock levels and quickly spot low-stock items.</p>
          </div>
          <div className="rounded-xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-700">
            <p>
              <span className="font-semibold text-slate-900">{lowStockCount}</span> low-stock items
            </p>
            <p className="text-xs text-slate-500">Threshold: below {LOW_STOCK_THRESHOLD}</p>
          </div>
        </div>

        {successMessage && <div className="mb-4 rounded-lg bg-green-100 px-4 py-3 text-green-800">{successMessage}</div>}
        {error && <div className="mb-4 rounded-lg bg-red-100 px-4 py-3 text-red-700">{error}</div>}
        {!error && lowStockCount > 0 && (
          <div className="mb-4 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-red-800">
            <p className="font-semibold">
              Low stock alert: {lowStockCount} item(s) are below {LOW_STOCK_THRESHOLD}.
            </p>
            {criticalStockCount > 0 && (
              <p className="text-sm text-red-700">
                {criticalStockCount} item(s) are at critical stock ({CRITICAL_STOCK_THRESHOLD} or less).
              </p>
            )}
          </div>
        )}

        <div className="mb-4 flex items-center gap-3 text-xs text-slate-600">
          <span className="inline-flex items-center gap-2 rounded-md bg-red-50 px-3 py-1">
            <span className="h-2.5 w-2.5 rounded-full bg-red-500" />
            Critical stock ({CRITICAL_STOCK_THRESHOLD} or less)
          </span>
          <span className="inline-flex items-center gap-2 rounded-md bg-amber-50 px-3 py-1">
            <span className="h-2.5 w-2.5 rounded-full bg-amber-500" />
            Low stock (below {LOW_STOCK_THRESHOLD})
          </span>
        </div>

        <div className="mb-4">
          <SupplierFormCard onSubmit={submitSupplier} isSubmitting={isSupplierSubmitting} />
        </div>

        <div className="overflow-x-auto rounded-2xl border border-slate-200 bg-white p-4 shadow-sm">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-slate-200 text-left text-slate-500">
                <th className="py-2">Product Name</th>
                <th className="py-2">Quantity</th>
                <th className="py-2">Price</th>
                <th className="py-2">Category</th>
                <th className="py-2">Supplier</th>
                <th className="py-2">Actions</th>
              </tr>
            </thead>
            <tbody>
              {!isLoading && filteredInventory.length === 0 && (
                <tr>
                  <td colSpan={6} className="py-5 text-slate-500">
                    No inventory items found.
                  </td>
                </tr>
              )}

              {filteredInventory.map((product) => {
                const isLowStock = product.isLowStock || product.quantity < LOW_STOCK_THRESHOLD;
                const isCriticalStock = isLowStock && product.quantity <= CRITICAL_STOCK_THRESHOLD;

                const rowClassName = isCriticalStock
                  ? "bg-red-50 hover:bg-red-100"
                  : isLowStock
                    ? "bg-amber-50 hover:bg-amber-100"
                    : "hover:bg-slate-50";

                return (
                  <tr key={product.id} className={`border-b border-slate-100 transition-colors ${rowClassName}`}>
                    <td className="py-3 font-medium text-slate-900">{product.name}</td>
                    <td className={`py-3 font-semibold ${isLowStock ? "text-red-700" : "text-slate-800"}`}>
                      {product.quantity}
                    </td>
                    <td className="py-3">Rs. {priceFormatter.format(product.price)}</td>
                    <td className="py-3">{product.category}</td>
                    <td className="py-3 text-slate-600">
                      {product.supplierName
                        ?? (product.supplierId ? supplierNameById[product.supplierId] ?? `Supplier #${product.supplierId}` : "-")}
                    </td>
                    <td className="py-3">
                      <button
                        type="button"
                        onClick={() => openStockModal(product)}
                        className="rounded-lg bg-blue-600 px-3 py-1.5 text-xs font-medium text-white hover:bg-blue-700"
                      >
                        Update Stock
                      </button>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </div>

      {showLowStockPopup && !error && lowStockCount > 0 && (
        <div className="fixed bottom-5 right-5 z-50 w-[320px] rounded-xl border border-amber-300 bg-amber-50 p-4 shadow-lg">
          <div className="flex items-start justify-between gap-3">
            <div>
              <p className="text-sm font-semibold text-amber-900">Inventory Notification</p>
              <p className="mt-1 text-sm text-amber-800">
                {lowStockCount} low-stock item(s) need attention.
              </p>
            </div>
            <button
              type="button"
              onClick={() => setShowLowStockPopup(false)}
              className="rounded-md px-2 py-1 text-xs font-medium text-amber-900 hover:bg-amber-100"
            >
              Dismiss
            </button>
          </div>
        </div>
      )}

      {stockModalProduct && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4">
          <div className="w-full max-w-md rounded-2xl border border-slate-200 bg-white p-6 shadow-xl">
            <h3 className="text-lg font-semibold text-slate-900">Update Stock</h3>
            <p className="mt-1 text-sm text-slate-600">
              {stockModalProduct.name} (Current: {stockModalProduct.quantity})
            </p>
            <p className="mt-1 text-sm text-slate-500">
              Supplier:{" "}
              {stockModalProduct.supplierId
                ? stockModalProduct.supplierName
                  ?? supplierNameById[stockModalProduct.supplierId]
                  ?? `Supplier #${stockModalProduct.supplierId}`
                : "Not assigned"}
            </p>

            <div className="mt-4 grid gap-3">
              <div>
                <label className="mb-1 block text-sm font-medium text-slate-700">Operation</label>
                <select
                  value={stockOperation}
                  onChange={(e) => setStockOperation(e.target.value as StockOperation)}
                  className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm"
                >
                  <option value="increase">Increase</option>
                  <option value="decrease">Decrease</option>
                </select>
              </div>

              <div>
                <label className="mb-1 block text-sm font-medium text-slate-700">Amount</label>
                <input
                  type="number"
                  min={1}
                  value={stockAmount}
                  onChange={(e) => setStockAmount(Number(e.target.value))}
                  className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm"
                />
              </div>

              <div>
                <label className="mb-1 block text-sm font-medium text-slate-700">Reason (optional)</label>
                <input
                  type="text"
                  value={stockReason}
                  onChange={(e) => setStockReason(e.target.value)}
                  placeholder="e.g., Supplier restock"
                  className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm"
                />
              </div>
            </div>

            {stockFormError && <div className="mt-3 rounded-lg bg-red-100 px-3 py-2 text-sm text-red-700">{stockFormError}</div>}

            <div className="mt-5 flex justify-end gap-3">
              <button
                type="button"
                onClick={closeStockModal}
                className="rounded-lg bg-slate-200 px-4 py-2 text-sm font-medium text-slate-800 hover:bg-slate-300"
              >
                Cancel
              </button>
              <button
                type="button"
                onClick={() => {
                  void submitStockUpdate();
                }}
                disabled={isStockUpdating}
                className="rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:bg-slate-400"
              >
                {isStockUpdating ? "Updating..." : "Save"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default InventoryPage;
