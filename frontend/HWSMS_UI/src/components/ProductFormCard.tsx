import { useEffect, useState, type FormEvent } from "react";
import type { Product, ProductPayload } from "../services/productService";
import type { Supplier } from "../services/supplierService";

type ProductFormCardProps = {
  onSubmit: (payload: ProductPayload) => Promise<void>;
  editingProduct: Product | null;
  suppliers: Supplier[];
};

const emptyForm: ProductPayload = {
  name: "",
  sku: "",
  category: "",
  price: 0,
  quantity: 0,
  supplierId: null,
};

const ProductFormCard = ({ onSubmit, editingProduct, suppliers }: ProductFormCardProps) => {
  const [form, setForm] = useState<ProductPayload>(emptyForm);

  useEffect(() => {
    if (editingProduct) {
      setForm({
        name: editingProduct.name,
        sku: editingProduct.sku,
        category: editingProduct.category,
        price: editingProduct.price,
        quantity: editingProduct.quantity,
        supplierId: editingProduct.supplierId ?? null,
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
    <div className="hw-card">
      <h3 className="text-lg font-semibold text-slate-900 mb-6">Add / Update Product</h3>

      <form onSubmit={handleSubmit} className="space-y-4">
        <input
          value={form.name}
          placeholder="Product Name"
          className="hw-input"
          onChange={(e) => setForm({ ...form, name: e.target.value })}
        />

        <div className="grid grid-cols-2 gap-4">
          <input
            value={form.sku}
            placeholder="SKU"
            className="hw-input"
            onChange={(e) => setForm({ ...form, sku: e.target.value })}
          />
          <input
            value={form.category}
            placeholder="Category"
            className="hw-input"
            onChange={(e) => setForm({ ...form, category: e.target.value })}
          />
        </div>

        <div className="grid grid-cols-2 gap-4">
          <input
            type="number"
            value={form.price || ""}
            placeholder="Price"
            className="hw-input"
            onChange={(e) => setForm({ ...form, price: Number(e.target.value) })}
          />
          <input
            type="number"
            value={form.quantity || ""}
            placeholder="Quantity"
            className="hw-input"
            onChange={(e) => setForm({ ...form, quantity: Number(e.target.value) })}
          />
        </div>

        <select
          value={form.supplierId ?? ""}
          className="hw-input"
          onChange={(e) =>
            setForm({
              ...form,
              supplierId: e.target.value === "" ? null : Number(e.target.value),
            })
          }
        >
          <option value="">Select Supplier (Optional)</option>
          {suppliers.map((supplier) => (
            <option key={supplier.id} value={supplier.id}>
              {supplier.name}
            </option>
          ))}
        </select>

        <button className="hw-btn-primary w-full py-3">
          {editingProduct ? "Update Product" : "+ Add Product"}
        </button>
      </form>
    </div>
  );
};

export default ProductFormCard;