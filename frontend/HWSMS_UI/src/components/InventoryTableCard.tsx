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
    <div className="hw-card">
      <h3 className="text-lg font-semibold text-slate-900 mb-6">Inventory List</h3>

      <div className="max-h-[420px] overflow-auto">
        <table className="hw-table text-left">
          <thead className="sticky top-0 z-10 bg-[#f8fbfe] text-xs uppercase tracking-[0.1em]">
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

          <tbody>
            {products.map((p) => (
              <tr key={p.id}>
                <td className="py-4 font-medium">{p.name}</td>
                <td>{p.sku}</td>
                <td>Rs. {lkrFormatter.format(p.price)}</td>
                <td>{p.quantity}</td>
                <td>{p.supplierId ? supplierNameById[p.supplierId] ?? `#${p.supplierId}` : "-"}</td>
                <td>
                  <StatusBadge quantity={p.quantity} />
                </td>
                <td className="flex gap-2">
                  <button onClick={() => onEdit(p)} className="rounded-lg border border-slate-200 p-2 hover:bg-white">
                    ✏️
                  </button>
                  <button onClick={() => onDelete(p.id)} className="rounded-lg border border-slate-200 p-2 hover:bg-white">
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
