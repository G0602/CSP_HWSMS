import { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import Navbar from "../components/Navbar";
import { getApiErrorMessage, isForbidden, isUnauthorized } from "../services/apiError";
import { logout } from "../services/authService";
import { deleteSupplier, getSuppliers, updateSupplier, type Supplier } from "../services/supplierService";

const SupplierPage = () => {
  const navigate = useNavigate();
  const [suppliers, setSuppliers] = useState<Supplier[]>([]);
  const [search, setSearch] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  const [editingSupplier, setEditingSupplier] = useState<Supplier | null>(null);
  const [editName, setEditName] = useState("");
  const [editContactInfo, setEditContactInfo] = useState("");
  const [isUpdating, setIsUpdating] = useState(false);
  const [editError, setEditError] = useState("");

  const [deletingSupplier, setDeletingSupplier] = useState<Supplier | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);

  const handleLogout = useCallback(() => {
    logout();
    navigate("/login", { replace: true });
  }, [navigate]);

  const loadSuppliers = useCallback(async () => {
    setIsLoading(true);
    setError("");

    try {
      const data = await getSuppliers();
      setSuppliers(data);
    } catch (err) {
      if (isUnauthorized(err)) {
        handleLogout();
        return;
      }

      if (isForbidden(err)) {
        navigate("/access-denied", { replace: true });
        return;
      }

      setError(getApiErrorMessage(err, "Failed to load suppliers."));
    } finally {
      setIsLoading(false);
    }
  }, [handleLogout, navigate]);

  useEffect(() => {
    void loadSuppliers();
  }, [loadSuppliers]);

  const filteredSuppliers = useMemo(
    () => suppliers.filter((supplier) => supplier.name.toLowerCase().includes(search.toLowerCase().trim())),
    [suppliers, search],
  );

  const openEditModal = (supplier: Supplier) => {
    setEditingSupplier(supplier);
    setEditName(supplier.name);
    setEditContactInfo(supplier.contactInfo || "");
    setEditError("");
  };

  const submitEdit = async () => {
    if (!editingSupplier) {
      return;
    }

    const name = editName.trim();
    if (!name) {
      setEditError("Name is required.");
      return;
    }

    setIsUpdating(true);
    setEditError("");
    setError("");
    setSuccessMessage("");

    try {
      await updateSupplier(editingSupplier.id, { 
        name,
        contactInfo: editContactInfo.trim() || undefined
      });
      setSuccessMessage("Supplier updated successfully.");
      setEditingSupplier(null);
      await loadSuppliers();
    } catch (err) {
      if (isUnauthorized(err)) {
        handleLogout();
        return;
      }

      if (isForbidden(err)) {
        navigate("/access-denied", { replace: true });
        return;
      }

      setEditError(getApiErrorMessage(err, "Failed to update supplier."));
    } finally {
      setIsUpdating(false);
    }
  };

  const confirmDelete = async () => {
    if (!deletingSupplier) {
      return;
    }

    setIsDeleting(true);
    setError("");
    setSuccessMessage("");

    try {
      await deleteSupplier(deletingSupplier.id);
      setSuccessMessage("Supplier deleted successfully.");
      setDeletingSupplier(null);
      await loadSuppliers();
    } catch (err) {
      if (isUnauthorized(err)) {
        handleLogout();
        return;
      }

      if (isForbidden(err)) {
        navigate("/access-denied", { replace: true });
        return;
      }

      setError(getApiErrorMessage(err, "Failed to delete supplier."));
    } finally {
      setIsDeleting(false);
    }
  };

  return (
    <div className="min-h-screen bg-slate-50">
      <Navbar search={search} onSearchChange={setSearch} onLogout={handleLogout} />

      <div className="mx-auto max-w-7xl p-6 lg:p-10">
        <div className="mb-6">
          <h2 className="text-3xl font-bold text-slate-900">Suppliers</h2>
          <p className="mt-1 text-slate-600">Manage supplier records for procurement operations.</p>
        </div>

        {successMessage && <div className="mb-4 rounded-lg bg-green-100 px-4 py-3 text-green-800">{successMessage}</div>}
        {error && <div className="mb-4 rounded-lg bg-red-100 px-4 py-3 text-red-700">{error}</div>}

        <div className="overflow-x-auto rounded-2xl border border-slate-200 bg-white p-4 shadow-sm">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-slate-200 text-left text-slate-500">
                <th className="py-2">Name</th>
                <th className="py-2">Contact Info</th>
                <th className="py-2">Created</th>
                <th className="py-2">Actions</th>
              </tr>
            </thead>
            <tbody>
              {!isLoading && filteredSuppliers.length === 0 && (
                <tr>
                  <td colSpan={4} className="py-5 text-slate-500">
                    No suppliers found.
                  </td>
                </tr>
              )}

              {filteredSuppliers.map((supplier) => (
                <tr key={supplier.id} className="border-b border-slate-100">
                  <td className="py-3 font-medium text-slate-900">{supplier.name}</td>
                  <td className="py-3 text-slate-600">{supplier.contactInfo || "-"}</td>
                  <td className="py-3 text-slate-600">
                    {supplier.createdAt ? new Date(supplier.createdAt).toLocaleString() : "-"}
                  </td>
                  <td className="py-3">
                    <div className="flex gap-2">
                      <button
                        type="button"
                        onClick={() => openEditModal(supplier)}
                        className="rounded-lg bg-amber-500 px-3 py-1.5 text-xs font-medium text-white hover:bg-amber-600"
                      >
                        Edit
                      </button>
                      <button
                        type="button"
                        onClick={() => setDeletingSupplier(supplier)}
                        className="rounded-lg bg-red-600 px-3 py-1.5 text-xs font-medium text-white hover:bg-red-700"
                      >
                        Delete
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {editingSupplier && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4">
          <div className="w-full max-w-md rounded-2xl border border-slate-200 bg-white p-6 shadow-xl">
            <h3 className="text-lg font-semibold text-slate-900">Edit Supplier</h3>
            <div className="mt-4 space-y-3">
              <div>
                <label className="mb-1 block text-sm font-medium text-slate-700">Name</label>
                <input
                  type="text"
                  value={editName}
                  onChange={(e) => setEditName(e.target.value)}
                  className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm"
                />
              </div>
              <div>
                <label className="mb-1 block text-sm font-medium text-slate-700">Contact Info</label>
                <input
                  type="text"
                  value={editContactInfo}
                  onChange={(e) => setEditContactInfo(e.target.value)}
                  placeholder="Phone or email"
                  className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm"
                />
              </div>
            </div>
            {editError && <div className="mt-3 rounded-lg bg-red-100 px-3 py-2 text-sm text-red-700">{editError}</div>}
            <div className="mt-5 flex justify-end gap-3">
              <button
                type="button"
                onClick={() => setEditingSupplier(null)}
                className="rounded-lg bg-slate-200 px-4 py-2 text-sm font-medium text-slate-800 hover:bg-slate-300"
              >
                Cancel
              </button>
              <button
                type="button"
                onClick={() => {
                  void submitEdit();
                }}
                disabled={isUpdating}
                className="rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:bg-slate-400"
              >
                {isUpdating ? "Saving..." : "Save"}
              </button>
            </div>
          </div>
        </div>
      )}

      {deletingSupplier && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4">
          <div className="w-full max-w-md rounded-2xl border border-slate-200 bg-white p-6 shadow-xl">
            <h3 className="text-lg font-semibold text-slate-900">Confirm Deletion</h3>
            <p className="mt-2 text-sm text-slate-600">
              Delete supplier "{deletingSupplier.name}"? This action cannot be undone.
            </p>
            <div className="mt-5 flex justify-end gap-3">
              <button
                type="button"
                onClick={() => setDeletingSupplier(null)}
                className="rounded-lg bg-slate-200 px-4 py-2 text-sm font-medium text-slate-800 hover:bg-slate-300"
              >
                Cancel
              </button>
              <button
                type="button"
                onClick={() => {
                  void confirmDelete();
                }}
                disabled={isDeleting}
                className="rounded-lg bg-red-600 px-4 py-2 text-sm font-medium text-white hover:bg-red-700 disabled:bg-slate-400"
              >
                {isDeleting ? "Deleting..." : "Delete"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default SupplierPage;
