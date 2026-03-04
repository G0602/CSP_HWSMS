import { useEffect, useState } from "react";
import {
  addProduct,
  deleteProduct,
  getProducts,
  updateProduct,
  type Product,
  type ProductPayload,
} from "../services/productService";
import ProductForm from "../components/ProductForm";
import ProductTable from "../components/ProductTable";

const ProductPage = () => {
  const [products, setProducts] = useState<Product[]>([]);
  const [editingProduct, setEditingProduct] = useState<Product | null>(null);
  const [loading, setLoading] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const loadProducts = async () => {
    setLoading(true);
    setError(null);
    try {
      const response = await getProducts();
      setProducts(response.data);
    } catch {
      setError("Could not load products. Check backend/API URL and try again.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadProducts();
  }, []);

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
    } catch {
      setError("Save failed. Please check your input and backend connection.");
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
    } catch {
      setError("Delete failed. Please try again.");
    }
  };

  return (
    <div className="min-h-screen bg-gray-100 p-8">
      <div className="max-w-6xl mx-auto">
        <h1 className="text-3xl font-bold mb-6 text-center">Hardware Store Product Management</h1>

        {message && <div className="mb-4 rounded-md bg-green-100 p-3 text-green-800">{message}</div>}
        {error && <div className="mb-4 rounded-md bg-red-100 p-3 text-red-700">{error}</div>}

        <ProductForm onSubmit={handleAddOrUpdate} editingProduct={editingProduct} isSubmitting={isSubmitting} />

        <ProductTable products={products} loading={loading} onEdit={setEditingProduct} onDelete={handleDelete} />
      </div>
    </div>
  );
};

export default ProductPage;
