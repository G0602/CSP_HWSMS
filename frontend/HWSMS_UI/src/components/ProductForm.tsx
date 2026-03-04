import { useEffect, useState, type ChangeEvent, type FormEvent } from "react";
import type { Product, ProductPayload } from "../services/productService";

type ProductFormProps = {
  onSubmit: (product: ProductPayload) => Promise<void> | void;
  editingProduct: Product | null;
  isSubmitting?: boolean;
};

const initialForm: ProductPayload = {
  name: "",
  sku: "",
  price: 0,
  quantity: 0,
  category: "",
};

const fieldMeta = {
  name: { placeholder: "e.g., Claw Hammer", help: "Product name customers see" },
  sku: { placeholder: "e.g., HAM-001", help: "SKU = Stock Keeping Unit (unique code)" },
  price: { placeholder: "e.g., 1500", help: "Price in LKR, must be greater than 0" },
  quantity: { placeholder: "e.g., 25", help: "Available stock count" },
  category: { placeholder: "e.g., Tools", help: "Group like Tools, Paint, Electrical" },
} as const;

const ProductForm = ({ onSubmit, editingProduct, isSubmitting = false }: ProductFormProps) => {
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
      });
    }
  }, [editingProduct]);

  const validate = () => {
    const newErrors: Record<string, string> = {};

    if (!formData.name.trim()) newErrors.name = "Name is required";
    if (!formData.sku.trim()) newErrors.sku = "SKU is required";
    if (formData.price <= 0) newErrors.price = "Price must be > 0";
    if (formData.quantity < 0) newErrors.quantity = "Quantity cannot be negative";
    if (!formData.category.trim()) newErrors.category = "Category is required";

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleChange = (e: ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;

    setFormData((prev) => ({
      ...prev,
      [name]: name === "price" || name === "quantity" ? Number(value) : value,
    }));
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!validate()) return;

    await onSubmit(formData);
    setFormData(initialForm);
    setErrors({});
  };

  return (
    <div className="bg-white p-6 rounded-xl shadow-md mb-6">
      <h2 className="text-xl font-bold mb-4">{editingProduct ? "Update Product" : "Add New Product"}</h2>

      <form onSubmit={handleSubmit} className="grid grid-cols-2 gap-4">
        {(["name", "sku", "price", "quantity", "category"] as const).map((field) => (
          <div key={field} className="flex flex-col">
            <input
              name={field}
              type={field === "price" || field === "quantity" ? "number" : "text"}
              placeholder={fieldMeta[field].placeholder}
              value={formData[field]}
              onChange={handleChange}
              className="border p-2 rounded"
            />
            <span className="text-xs text-gray-500 mt-1">{fieldMeta[field].help}</span>
            {errors[field] && <span className="text-red-500 text-sm">{errors[field]}</span>}
          </div>
        ))}

        <button
          type="submit"
          disabled={isSubmitting}
          className="col-span-2 bg-blue-600 text-white py-2 rounded hover:bg-blue-700 disabled:opacity-60"
        >
          {isSubmitting ? "Saving..." : editingProduct ? "Update" : "Add Product"}
        </button>
      </form>
    </div>
  );
};

export default ProductForm;
