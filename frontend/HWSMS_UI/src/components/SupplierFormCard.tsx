import { useState } from "react";
import type { SupplierPayload } from "../services/supplierService";

type SupplierFormCardProps = {
  onSubmit: (payload: SupplierPayload) => Promise<void>;
  isSubmitting: boolean;
};

const SupplierFormCard = ({ onSubmit, isSubmitting }: SupplierFormCardProps) => {
  const [name, setName] = useState("");
  const [contactInfo, setContactInfo] = useState("");
  const [localError, setLocalError] = useState("");

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setLocalError("");

    const trimmedName = name.trim();
    if (!trimmedName) {
      setLocalError("Supplier name is required.");
      return;
    }

    await onSubmit({
      name: trimmedName,
      contactInfo: contactInfo.trim() || undefined,
    });

    setName("");
    setContactInfo("");
  };

  return (
    <div className="rounded-2xl border border-slate-200 bg-white p-5 shadow-sm">
      <h3 className="text-lg font-semibold text-slate-900">Add Supplier</h3>
      <p className="mt-1 text-sm text-slate-600">Create a supplier record for inventory operations.</p>

      <form className="mt-4 grid gap-3" onSubmit={(e) => void handleSubmit(e)}>
        <div>
          <label htmlFor="supplier-name" className="mb-1 block text-sm font-medium text-slate-700">
            Name
          </label>
          <input
            id="supplier-name"
            type="text"
            value={name}
            onChange={(e) => setName(e.target.value)}
            className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm"
            placeholder="ABC Suppliers"
          />
        </div>

        <div>
          <label htmlFor="supplier-contact" className="mb-1 block text-sm font-medium text-slate-700">
            Contact info
          </label>
          <input
            id="supplier-contact"
            type="text"
            value={contactInfo}
            onChange={(e) => setContactInfo(e.target.value)}
            className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm"
            placeholder="Phone or email"
          />
        </div>

        {localError && <div className="rounded-lg bg-red-100 px-3 py-2 text-sm text-red-700">{localError}</div>}

        <button
          type="submit"
          disabled={isSubmitting}
          className="rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:bg-slate-400"
        >
          {isSubmitting ? "Saving..." : "Add Supplier"}
        </button>
      </form>
    </div>
  );
};

export default SupplierFormCard;
