import { useEffect, useState, type FormEvent } from "react";
import type { Product, ProductPayload } from "../services/productService";

type ProductFormCardProps = {
  onSubmit: (payload: ProductPayload) => Promise<void>;
  editingProduct: Product | null;
};

const emptyForm: ProductPayload = {
  name: "",
  sku: "",
  category: "",
  price: 0,
  quantity: 0,
};

const ProductFormCard = ({ onSubmit, editingProduct }: ProductFormCardProps) => {
  const [form, setForm] = useState<ProductPayload>(emptyForm);

  useEffect(() => {
    if (editingProduct) {
      setForm({
        name: editingProduct.name,
        sku: editingProduct.sku,
        category: editingProduct.category,
        price: editingProduct.price,
        quantity: editingProduct.quantity,
      });
      return;
    }

    setForm(emptyForm);
  }, [editingProduct]);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    await onSubmit(form);
    if (!editingProduct) {
      setForm(emptyForm);
    }
  };

  return (
    <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-6">
      <h3 className="text-lg font-semibold mb-6">Add / Update Product</h3>

      <form onSubmit={handleSubmit} className="space-y-4">
        <input
          value={form.name}
          placeholder="Product Name"
          className="w-full bg-gray-50 p-3 rounded-xl border border-gray-200"
          onChange={(e) => setForm({ ...form, name: e.target.value })}
        />

        <div className="grid grid-cols-2 gap-4">
          <input
            value={form.sku}
            placeholder="SKU"
            className="bg-gray-50 p-3 rounded-xl border border-gray-200"
            onChange={(e) => setForm({ ...form, sku: e.target.value })}
          />
          <input
            value={form.category}
            placeholder="Category"
            className="bg-gray-50 p-3 rounded-xl border border-gray-200"
            onChange={(e) => setForm({ ...form, category: e.target.value })}
          />
        </div>

        <div className="grid grid-cols-2 gap-4">
          <input
            type="number"
            value={form.price || ""}
            placeholder="Price"
            className="bg-gray-50 p-3 rounded-xl border border-gray-200"
            onChange={(e) => setForm({ ...form, price: Number(e.target.value) })}
          />
          <input
            type="number"
            value={form.quantity || ""}
            placeholder="Quantity"
            className="bg-gray-50 p-3 rounded-xl border border-gray-200"
            onChange={(e) => setForm({ ...form, quantity: Number(e.target.value) })}
          />
        </div>

        <button className="w-full bg-blue-600 hover:bg-blue-700 text-white py-3 rounded-xl transition">
          {editingProduct ? "Update Product" : "+ Add Product"}
        </button>
      </form>
    </div>
  );
};

export default ProductFormCard;
