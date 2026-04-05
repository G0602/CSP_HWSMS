import axios from "axios";
import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import DashboardHeader from "../components/DashboardHeader";
import InventoryTableCard from "../components/InventoryTableCard";
import Navbar from "../components/Navbar";
import ProductFormCard from "../components/ProductFormCard";
import StatsCard from "../components/StatsCard";
import { getCurrentUser, logout } from "../services/authService";
import { getSuppliers, type Supplier } from "../services/supplierService";
import {
  addProduct,
  deleteProduct,
  getProducts,
  updateProduct,
  type Product,
  type ProductPayload,
} from "../services/productService";

const ProductDashboard = () => {
  const navigate = useNavigate();
  const [products, setProducts] = useState<Product[]>([]);
  const [suppliers, setSuppliers] = useState<Supplier[]>([]);
  const [editingProduct, setEditingProduct] = useState<Product | null>(null);
  const [search, setSearch] = useState("");
  const [error, setError] = useState("");

  const user = getCurrentUser();

  const handleLogout = () => {
    logout();
    navigate("/login", { replace: true });
  };

  const loadProducts = async () => {
    setError("");
    try {
      const response = await getProducts();
      setProducts(response.data);
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.status === 401) {
        handleLogout();
        return;
      }

      setError("Could not load products. Please try again.");
    }
  };

  const loadSuppliers = async () => {
    try {
      const data = await getSuppliers();
      setSuppliers(data);
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.status === 401) {
        handleLogout();
        return;
      }

      setSuppliers([]);
    }
  };

  useEffect(() => {
    void loadProducts();
    void loadSuppliers();
  }, []);

  const handleSubmit = async (newProduct: ProductPayload) => {
    if (editingProduct) {
      await updateProduct(editingProduct.id, newProduct);
      setEditingProduct(null);
    } else {
      const normalizedSku = newProduct.sku.trim().toLowerCase();
      const existingProduct = products.find((product) => product.sku.trim().toLowerCase() === normalizedSku);

      if (existingProduct) {
        const mergedProduct: ProductPayload = {
          name: newProduct.name || existingProduct.name,
          sku: existingProduct.sku,
          category: newProduct.category || existingProduct.category,
          price: newProduct.price > 0 ? newProduct.price : existingProduct.price,
          quantity: existingProduct.quantity + Math.max(0, newProduct.quantity),
          supplierId: newProduct.supplierId ?? existingProduct.supplierId ?? null,
        };

        await updateProduct(existingProduct.id, mergedProduct);
      } else {
        await addProduct(newProduct);
      }
    }

    await loadProducts();
  };

  const handleDelete = async (id: number) => {
    await deleteProduct(id);
    await loadProducts();
  };

  const filteredProducts = useMemo(
    () =>
      products.filter((p) =>
        [p.name, p.sku, p.category].join(" ").toLowerCase().includes(search.toLowerCase().trim()),
      ),
    [products, search],
  );

  const totalValuation = products.reduce((sum, p) => sum + p.price * p.quantity, 0);
  const supplierNameById = useMemo(
    () =>
      suppliers.reduce<Record<number, string>>((acc, supplier) => {
        acc[supplier.id] = supplier.name;
        return acc;
      }, {}),
    [suppliers],
  );
  const totalValuationFormatted = new Intl.NumberFormat("en-LK", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(totalValuation);
  const totalLowStock = filteredProducts.filter((p) => p.quantity < 10).length;

  return (
    <div className="bg-gray-50 min-h-screen">
      <Navbar search={search} onSearchChange={setSearch} username={user?.username} onLogout={handleLogout} />

      <div className="p-10">
        {error && <div className="mb-4 rounded-md bg-red-100 p-3 text-red-700">{error}</div>}

        <DashboardHeader totalValuation={totalValuationFormatted} />

        <div className="grid grid-cols-3 gap-8">
          <ProductFormCard onSubmit={handleSubmit} editingProduct={editingProduct} suppliers={suppliers} />

          <div className="col-span-2">
            <InventoryTableCard
              products={filteredProducts}
              supplierNameById={supplierNameById}
              onEdit={setEditingProduct}
              onDelete={handleDelete}
            />
          </div>
        </div>

        <div className="flex gap-6 mt-8">
          <StatsCard title="Total SKUs" value={filteredProducts.length} />
          <StatsCard title="Low Stock" value={totalLowStock} highlight />
        </div>
      </div>
    </div>
  );
};

export default ProductDashboard;
