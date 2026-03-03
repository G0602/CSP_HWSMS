import { useState } from "react";
import type { Product } from "../services/productService";
import ConfirmModal from "./ConfirmModal";

type ProductTableProps = {
  products: Product[];
  loading: boolean;
  onEdit: (product: Product) => void;
  onDelete: (id: number) => Promise<void> | void;
};

const ProductTable = ({ products, loading, onEdit, onDelete }: ProductTableProps) => {
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

  if (loading) return <p className="text-center text-lg mt-4">Loading products...</p>;

  return (
    <div className="bg-white p-6 rounded-xl shadow-md">
      <h2 className="text-xl font-bold mb-4">Product List</h2>

      <table className="w-full border-collapse">
        <thead>
          <tr className="bg-gray-200">
            <th className="p-2">Name</th>
            <th className="p-2">SKU</th>
            <th className="p-2">Price</th>
            <th className="p-2">Quantity</th>
            <th className="p-2">Status</th>
            <th className="p-2">Actions</th>
          </tr>
        </thead>

        <tbody>
          {products.map((product) => (
            <tr key={product.id} className="text-center border-t">
              <td className="p-2">{product.name}</td>
              <td className="p-2">{product.sku}</td>
              <td className="p-2">Rs. {Number(product.price).toFixed(2)}</td>
              <td className="p-2">{product.quantity}</td>
              <td className={`p-2 font-semibold ${product.quantity < 10 ? "text-red-600" : "text-green-600"}`}>
                {product.quantity < 10 ? "Low Stock" : "In Stock"}
              </td>
              <td className="p-2 flex justify-center gap-2">
                <button onClick={() => onEdit(product)} className="bg-yellow-500 text-white px-3 py-1 rounded">
                  Edit
                </button>
                <button onClick={() => openModal(product.id)} className="bg-red-600 text-white px-3 py-1 rounded">
                  Delete
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>

      <ConfirmModal isOpen={isModalOpen} onClose={() => setIsModalOpen(false)} onConfirm={confirmDelete} />
    </div>
  );
};

export default ProductTable;
