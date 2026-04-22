import { useState } from "react";
import type { Product } from "../services/productService";
import ConfirmModal from "./ConfirmModal";

type ProductTableProps = {
  products: Product[];
  supplierNameById?: Record<number, string>;
  loading: boolean;
  onEdit: (product: Product) => void;
  onDelete: (id: number) => Promise<void> | void;
};

const ProductTable = ({ products, supplierNameById = {}, loading, onEdit, onDelete }: ProductTableProps) => {
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [selectedId, setSelectedId] = useState<number | null>(null);

  const openModal = (id: number) => {
    setSelectedId(id);
    setIsModalOpen(true);
  };

  const confirmDelete = async () => {
    if (selectedId === null) return;
    await onDelete(selectedId);
    setIsModalOpen(false);
    setSelectedId(null);
  };

  if (loading) return <div className="rounded-xl border border-slate-200 bg-white p-6 text-center text-slate-600 shadow-sm">Loading products...</div>;

  return (
    <div className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm">
      <div className="border-b border-slate-200 px-5 py-4">
        <h2 className="text-xl font-semibold text-slate-900">Product List</h2>
      </div>

      <div className="overflow-x-auto p-4">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-slate-200 text-left text-slate-500">
            <th className="py-2 pr-4">Name</th>
            <th className="py-2 pr-4">SKU</th>
            <th className="py-2 pr-4">Price</th>
            <th className="py-2 pr-4">Quantity</th>
            <th className="py-2 pr-4">Supplier</th>
            <th className="py-2 pr-4">Status</th>
            <th className="py-2 pr-4">Actions</th>
          </tr>
        </thead>

        <tbody>
          {products.length === 0 && (
            <tr>
              <td colSpan={7} className="py-6 text-slate-500">
                No products found.
              </td>
            </tr>
          )}
          {products.map((product) => (
            <tr key={product.id} className="border-b border-slate-100 hover:bg-slate-50">
              <td className="py-3 pr-4 font-medium text-slate-900">{product.name}</td>
              <td className="py-3 pr-4 text-slate-600">{product.sku}</td>
              <td className="py-3 pr-4">Rs. {Number(product.price).toFixed(2)}</td>
              <td className="py-3 pr-4 font-semibold">{product.quantity}</td>
              <td className="py-3 pr-4 text-slate-600">
                {product.supplierId ? supplierNameById[product.supplierId] ?? `#${product.supplierId}` : "-"}
              </td>
              <td className="py-3 pr-4">
                <span
                  className={`rounded-full px-2.5 py-1 text-xs font-semibold ${
                    product.quantity < 10 ? "bg-red-100 text-red-700" : "bg-emerald-100 text-emerald-700"
                  }`}
                >
                  {product.quantity < 10 ? "Low Stock" : "In Stock"}
                </span>
              </td>
              <td className="py-3 pr-4">
                <div className="flex gap-2">
                <button onClick={() => onEdit(product)} className="rounded-lg bg-amber-500 px-3 py-1.5 text-xs font-medium text-white hover:bg-amber-600">
                  Edit
                </button>
                <button onClick={() => openModal(product.id)} className="rounded-lg bg-red-600 px-3 py-1.5 text-xs font-medium text-white hover:bg-red-700">
                  Delete
                </button>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      </div>

      <ConfirmModal isOpen={isModalOpen} onClose={() => setIsModalOpen(false)} onConfirm={confirmDelete} />
    </div>
  );
};

export default ProductTable;
