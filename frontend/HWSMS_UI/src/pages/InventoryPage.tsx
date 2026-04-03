import axios from "axios";
import { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import Navbar from "../components/Navbar";
import { CRITICAL_STOCK_THRESHOLD, LOW_STOCK_THRESHOLD } from "../constants/inventory";
import { logout } from "../services/authService";
import { getInventoryProducts, type InventoryProduct } from "../services/productService";

const InventoryPage = () => {
  const navigate = useNavigate();
  const [inventory, setInventory] = useState<InventoryProduct[]>([]);
  const [search, setSearch] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState("");

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
      if (axios.isAxiosError(err) && err.response?.status === 401) {
        handleLogout();
        return;
      }

      if (axios.isAxiosError(err) && err.response?.status === 403) {
        navigate("/access-denied", { replace: true });
        return;
      }

      setError("Failed to load inventory.");
    } finally {
      setIsLoading(false);
    }
  }, [handleLogout, navigate]);

  useEffect(() => {
    void loadInventory();
  }, [loadInventory]);

  const filteredInventory = useMemo(
    () =>
      inventory.filter((product) =>
        [product.name, product.category].join(" ").toLowerCase().includes(search.toLowerCase().trim()),
      ),
    [inventory, search],
  );

  const lowStockCount = filteredInventory.filter(
    (product) => product.isLowStock || product.quantity < LOW_STOCK_THRESHOLD,
  ).length;

  const priceFormatter = new Intl.NumberFormat("en-LK", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });

  return (
    <div className="min-h-screen bg-slate-50">
      <Navbar search={search} onSearchChange={setSearch} onLogout={handleLogout} />

      <div className="mx-auto max-w-7xl p-6 lg:p-10">
        <div className="mb-6 flex flex-wrap items-end justify-between gap-4">
          <div>
            <h2 className="text-3xl font-bold text-slate-900">Inventory</h2>
            <p className="mt-1 text-slate-600">Track stock levels and quickly spot low-stock items.</p>
          </div>
          <div className="rounded-xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-700">
            <p>
              <span className="font-semibold text-slate-900">{lowStockCount}</span> low-stock items
            </p>
            <p className="text-xs text-slate-500">Threshold: below {LOW_STOCK_THRESHOLD}</p>
          </div>
        </div>

        {error && <div className="mb-4 rounded-lg bg-red-100 px-4 py-3 text-red-700">{error}</div>}

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

        <div className="overflow-x-auto rounded-2xl border border-slate-200 bg-white p-4 shadow-sm">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-slate-200 text-left text-slate-500">
                <th className="py-2">Product Name</th>
                <th className="py-2">Quantity</th>
                <th className="py-2">Price</th>
                <th className="py-2">Category</th>
              </tr>
            </thead>
            <tbody>
              {!isLoading && filteredInventory.length === 0 && (
                <tr>
                  <td colSpan={4} className="py-5 text-slate-500">
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
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
};

export default InventoryPage;
