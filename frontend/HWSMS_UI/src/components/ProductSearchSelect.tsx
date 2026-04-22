import { useEffect, useState } from "react";
import type { Product } from "../services/productService";
import { searchProducts } from "../services/productService";
import { getApiErrorMessage } from "../services/apiError";

type ProductSearchSelectProps = {
  selectedProductIds: number[];
  onAddItem: (product: Product, quantity: number) => void;
  refreshSignal?: number;
};

const ProductSearchSelect = ({ selectedProductIds, onAddItem, refreshSignal = 0 }: ProductSearchSelectProps) => {
  const [query, setQuery] = useState("");
  const [results, setResults] = useState<Product[]>([]);
  const [quantities, setQuantities] = useState<Record<number, number>>({});
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    const search = async () => {
      const term = query.trim();
      if (term.length < 2) {
        setResults([]);
        return;
      }

      setIsLoading(true);
      setError("");

      try {
        const data = await searchProducts(term);
        setResults(data.filter((item) => item.quantity > 0));
      } catch (err) {
        setError(getApiErrorMessage(err, "Failed to search products."));
      } finally {
        setIsLoading(false);
      }
    };

    const timer = window.setTimeout(() => {
      void search();
    }, 300);

    return () => window.clearTimeout(timer);
  }, [query]);

  useEffect(() => {
    setQuantities({});

    const term = query.trim();
    if (term.length < 2) {
      return;
    }

    const refresh = async () => {
      setIsLoading(true);
      setError("");

      try {
        const data = await searchProducts(term);
        setResults(data.filter((item) => item.quantity > 0));
      } catch (err) {
        setError(getApiErrorMessage(err, "Failed to refresh product stock."));
      } finally {
        setIsLoading(false);
      }
    };

    void refresh();
  }, [refreshSignal]);

  const getQty = (productId: number) => quantities[productId] ?? 1;

  return (
    <div className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
      <h3 className="text-xl font-semibold text-slate-900">Product Search</h3>
      <p className="text-sm text-slate-500 mt-1">Type at least 2 characters by name, SKU, or category.</p>

      <input
        type="text"
        value={query}
        onChange={(e) => setQuery(e.target.value)}
        placeholder="Search product..."
        className="mt-4 w-full rounded-xl border border-slate-300 px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
      />

      {isLoading && <p className="mt-3 text-sm text-slate-500">Searching...</p>}
      {error && <p className="mt-3 text-sm text-red-600">{error}</p>}
      {!isLoading && !error && query.trim().length >= 2 && results.length === 0 && (
        <p className="mt-3 rounded-lg bg-slate-50 px-3 py-2 text-sm text-slate-600">No in-stock products match this search.</p>
      )}

      <div className="mt-4 space-y-3 max-h-80 overflow-auto">
        {results.map((product) => {
          const isSelected = selectedProductIds.includes(product.id);
          const quantity = getQty(product.id);
          const maxQty = Math.max(1, product.quantity);

          return (
            <div key={product.id} className="rounded-xl border border-slate-200 p-3">
              <div className="flex items-center justify-between gap-3">
                <div>
                  <p className="font-medium text-slate-900">{product.name}</p>
                  <p className="text-xs text-slate-500">{product.sku} | Stock: {product.quantity} | Rs. {product.price}</p>
                </div>

                <div className="flex items-center gap-2">
                  <input
                    type="number"
                    min={1}
                    max={maxQty}
                    value={quantity}
                    onChange={(e) => {
                      const parsed = Number(e.target.value);
                      const safeValue = Number.isNaN(parsed) ? 1 : Math.min(Math.max(parsed, 1), maxQty);
                      setQuantities((prev) => ({ ...prev, [product.id]: safeValue }));
                    }}
                    className="w-20 rounded-lg border border-slate-300 px-2 py-1.5 text-sm"
                  />
                  <button
                    type="button"
                    disabled={isSelected}
                    onClick={() => onAddItem(product, quantity)}
                    className="rounded-lg bg-blue-600 text-white px-3 py-1.5 text-sm hover:bg-blue-700 disabled:bg-slate-400"
                  >
                    {isSelected ? "Added" : "Add"}
                  </button>
                </div>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
};

export default ProductSearchSelect;
