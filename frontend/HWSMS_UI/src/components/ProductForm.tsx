import { useEffect, useState, type ChangeEvent, type FormEvent } from "react";
import type { Product, ProductPayload } from "../services/productService";
import type { Supplier } from "../services/supplierService";

type ProductFormProps = {
  onSubmit: (product: ProductPayload) => Promise<void> | void;
  editingProduct: Product | null;
  suppliers: Supplier[];
  isSubmitting?: boolean;
};

const initialForm: ProductPayload = {
  name: "",
  sku: "",
  price: 0,
  quantity: 0,
  category: "",
  supplierId: null,
};

const fieldMeta = {
  name: { placeholder: "e.g., Claw Hammer", help: "Product name customers see" },
  sku: { placeholder: "e.g., HAM-001", help: "SKU = Stock Keeping Unit (unique code)" },
  price: { placeholder: "e.g., 1500", help: "Price in LKR, must be greater than 0" },
  quantity: { placeholder: "e.g., 25", help: "Available stock count" },
  category: { placeholder: "e.g., Tools", help: "Group like Tools, Paint, Electrical" },
} as const;

const ProductForm = ({ onSubmit, editingProduct, suppliers, isSubmitting = false }: ProductFormProps) => {
  const [formData, setFormData] = useState<ProductPayload>(initialForm);
  const [errors, setErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    if (editingProduct) {
      setFormData({
        name: editingProduct.name,
        sku: editingProduct.sku,
        price: editingProduct.price,
        quantity: editingProduct.quantity,
        category: editingProduct.category,
        supplierId: editingProduct.supplierId ?? null,
      });
      setErrors({});
      return;
    }

    setFormData(initialForm);
    setErrors({});
  }, [editingProduct]);

  const validate = () => {
    const newErrors: Record<string, string> = {};

    if (!formData.name.trim()) newErrors.name = "Product name is required.";
    if (!formData.sku.trim()) newErrors.sku = "SKU is required.";
    if (!Number.isFinite(formData.price) || formData.price <= 0) newErrors.price = "Price must be greater than zero.";
    if (!Number.isInteger(formData.quantity) || formData.quantity < 0) newErrors.quantity = "Quantity cannot be negative.";
    if (!formData.category.trim()) newErrors.category = "Category is required.";
    if (formData.supplierId !== null && formData.supplierId !== undefined && formData.supplierId <= 0) {
      newErrors.supplierId = "Supplier is invalid";
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleChange = (e: ChangeEvent<HTMLInputElement | HTMLSelectElement>) => {
    const { name, value } = e.target;

    setFormData((prev) => ({
      ...prev,
      [name]:
        name === "price" || name === "quantity"
          ? Number(value)
          : name === "supplierId"
            ? value === ""
              ? null
              : Number(value)
            : value,
    }));
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!validate()) return;

    await onSubmit({
      ...formData,
      name: formData.name.trim(),
      sku: formData.sku.trim(),
      category: formData.category.trim(),
    });
    setFormData(initialForm);
    setErrors({});
  };

  return (
    <div className="mb-6 rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
      <div className="mb-4">
        <h2 className="text-xl font-semibold text-slate-900">{editingProduct ? "Update Product" : "Add New Product"}</h2>
        <p className="mt-1 text-sm text-slate-600">Required fields are validated before saving to the backend.</p>
      </div>

      <form onSubmit={handleSubmit} className="grid gap-4 md:grid-cols-2">
        {(["name", "sku", "price", "quantity", "category"] as const).map((field) => (
          <div key={field} className="flex flex-col">
            <label className="mb-1 text-sm font-medium capitalize text-slate-700">{field}</label>
            <input
              name={field}
              type={field === "price" || field === "quantity" ? "number" : "text"}
              min={field === "price" ? 0.01 : field === "quantity" ? 0 : undefined}
              step={field === "price" ? 0.01 : field === "quantity" ? 1 : undefined}
              placeholder={fieldMeta[field].placeholder}
              value={formData[field]}
              onChange={handleChange}
              className="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none"
            />
            <span className="mt-1 text-xs text-slate-500">{fieldMeta[field].help}</span>
            {errors[field] && <span className="mt-1 text-sm text-red-600">{errors[field]}</span>}
          </div>
        ))}

        <div className="flex flex-col md:col-span-2">
          <label className="mb-1 text-sm font-medium text-slate-700">Supplier</label>
          <select
            name="supplierId"
            value={formData.supplierId ?? ""}
            onChange={handleChange}
            className="rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none"
          >
            <option value="">Select supplier (optional)</option>
            {suppliers.map((supplier) => (
              <option key={supplier.id} value={supplier.id}>
                {supplier.name}
              </option>
            ))}
          </select>
          <span className="mt-1 text-xs text-slate-500">Link this product to a supplier</span>
          {errors.supplierId && <span className="mt-1 text-sm text-red-600">{errors.supplierId}</span>}
        </div>

        <button
          type="submit"
          disabled={isSubmitting}
          className="rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-blue-700 disabled:cursor-not-allowed disabled:bg-slate-400 md:col-span-2"
        >
          {isSubmitting ? "Saving..." : editingProduct ? "Update" : "Add Product"}
        </button>
      </form>
    </div>
  );
};

export default ProductForm;
