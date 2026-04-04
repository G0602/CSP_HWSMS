import type { Product } from "../services/productService";
import StatusBadge from "./StatusBadge";

type InventoryTableCardProps = {
  products: Product[];
  supplierNameById?: Record<number, string>;
  onEdit: (product: Product) => void;
  onDelete: (id: number) => Promise<void>;
};

const InventoryTableCard = ({ products, supplierNameById = {}, onEdit, onDelete }: InventoryTableCardProps) => {
  const lkrFormatter = new Intl.NumberFormat("en-LK", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });

  return (
    <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-6">
      <h3 className="text-lg font-semibold mb-6">Inventory List</h3>

      <div className="max-h-[420px] overflow-auto">
        <table className="w-full text-left">
          <thead className="text-sm text-gray-500 uppercase sticky top-0 bg-white z-10">
            <tr>
              <th>Name</th>
              <th>SKU</th>
              <th>Price (Rs.)</th>
              <th>Quantity</th>
              <th>Supplier</th>
              <th>Status</th>
              <th></th>
            </tr>
          </thead>

          <tbody className="divide-y divide-gray-100">
            {products.map((p) => (
              <tr key={p.id} className="hover:bg-gray-50 transition">
                <td className="py-4 font-medium">{p.name}</td>
                <td>{p.sku}</td>
                <td>Rs. {lkrFormatter.format(p.price)}</td>
                <td>{p.quantity}</td>
                <td>{p.supplierId ? supplierNameById[p.supplierId] ?? `#${p.supplierId}` : "-"}</td>
                <td>
                  <StatusBadge quantity={p.quantity} />
                </td>
                <td className="flex gap-2">
                  <button onClick={() => onEdit(p)} className="p-2 rounded-lg hover:bg-gray-100">
                    ✏️
                  </button>
                  <button onClick={() => onDelete(p.id)} className="p-2 rounded-lg hover:bg-gray-100">
                    🗑️
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default InventoryTableCard;
