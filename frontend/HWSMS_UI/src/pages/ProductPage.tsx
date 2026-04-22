import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  addProduct,
  deleteProduct,
  getProducts,
  updateProduct,
  type Product,
  type ProductPayload,
} from "../services/productService";
import { getApiErrorMessage, isForbidden, isUnauthorized } from "../services/apiError";
import { logout } from "../services/authService";
import Navbar from "../components/Navbar";
import ProductForm from "../components/ProductForm";
import ProductTable from "../components/ProductTable";
import { getSuppliers, type Supplier } from "../services/supplierService";

const ProductPage = () => {
  const navigate = useNavigate();
  const [products, setProducts] = useState<Product[]>([]);
  const [suppliers, setSuppliers] = useState<Supplier[]>([]);
  const [editingProduct, setEditingProduct] = useState<Product | null>(null);
  const [loading, setLoading] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const handleLogout = () => {
    logout();
    navigate("/login", { replace: true });
  };

  const handleProtectedError = (err: unknown, fallback: string) => {
    if (isUnauthorized(err)) {
      handleLogout();
      return true;
    }

    if (isForbidden(err)) {
      navigate("/access-denied", { replace: true });
      return true;
    }

    setError(getApiErrorMessage(err, fallback));
    return false;
  };

  const loadProducts = async () => {
    setLoading(true);
    setError(null);
    try {
      const response = await getProducts();
      setProducts(response.data);
    } catch (err) {
      handleProtectedError(err, "Could not load products. Check backend/API URL and try again.");
    } finally {
      setLoading(false);
    }
  };

  const loadSuppliers = async () => {
    try {
      const data = await getSuppliers();
      setSuppliers(data);
    } catch (err) {
      handleProtectedError(err, "Could not load suppliers.");
      setSuppliers([]);
    }
  };

  useEffect(() => {
    void loadProducts();
    void loadSuppliers();
  }, []);

  const supplierNameById = suppliers.reduce<Record<number, string>>((acc, supplier) => {
    acc[supplier.id] = supplier.name;
    return acc;
  }, {});

  const handleAddOrUpdate = async (product: ProductPayload) => {
    setIsSubmitting(true);
    setError(null);
    setMessage(null);

    try {
      if (editingProduct) {
        await updateProduct(editingProduct.id, product);
        setEditingProduct(null);
        setMessage("Product updated successfully.");
      } else {
        await addProduct(product);
        setMessage("Product added successfully.");
      }

      await loadProducts();
    } catch (err) {
      handleProtectedError(err, "Save failed. Please check your input and backend connection.");
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleDelete = async (id: number) => {
    setError(null);
    setMessage(null);

    try {
      await deleteProduct(id);
      setMessage("Product deleted successfully.");
      await loadProducts();
    } catch (err) {
      handleProtectedError(err, "Delete failed. Please try again.");
    }
  };

  return (
    <div className="min-h-screen bg-slate-50">
      <Navbar onLogout={handleLogout} />
      <div className="mx-auto max-w-7xl p-6 lg:p-10">
        <div className="mb-6 flex flex-col gap-2 sm:flex-row sm:items-end sm:justify-between">
          <div>
            <h1 className="text-3xl font-bold text-slate-900">Products</h1>
            <p className="mt-1 text-slate-600">Create, link, and maintain sellable inventory items.</p>
          </div>
          <div className="rounded-xl border border-slate-200 bg-white px-4 py-3 text-sm shadow-sm">
            <span className="font-semibold text-slate-900">{products.length}</span> products loaded
          </div>
        </div>

        {message && <div className="mb-4 rounded-lg bg-green-100 px-4 py-3 text-green-800">{message}</div>}
        {error && <div className="mb-4 rounded-lg bg-red-100 px-4 py-3 text-red-700">{error}</div>}

        <ProductForm
          onSubmit={handleAddOrUpdate}
          editingProduct={editingProduct}
          suppliers={suppliers}
          isSubmitting={isSubmitting}
        />

        <ProductTable
          products={products}
          supplierNameById={supplierNameById}
          loading={loading}
          onEdit={setEditingProduct}
          onDelete={handleDelete}
        />
      </div>
    </div>
  );
};

export default ProductPage;
