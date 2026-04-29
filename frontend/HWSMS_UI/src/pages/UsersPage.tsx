import axios from "axios";
import { useEffect, useMemo, useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { AppRoles } from "../auth/roles";
import Navbar from "../components/Navbar";
import {
  createUser,
  deleteUser,
  getCurrentUser,
  getUsers,
  logout,
  updateUserRole,
  type CreateUserPayload,
  type ManagedUser,
  type UserRole,
} from "../services/authService";

const UsersPage = () => {
  const navigate = useNavigate();
  const [form, setForm] = useState<CreateUserPayload>({
    username: "",
    password: "",
    role: AppRoles.Cashier,
  });
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [users, setUsers] = useState<ManagedUser[]>([]);
  const [loadingUsers, setLoadingUsers] = useState(false);
  const [roleDraftByUserId, setRoleDraftByUserId] = useState<Record<number, UserRole>>({});
  const [editingUserId, setEditingUserId] = useState<number | null>(null);
  const [savingUserId, setSavingUserId] = useState<number | null>(null);
  const [deletingUserId, setDeletingUserId] = useState<number | null>(null);

  const user = getCurrentUser();

  const handleLogout = () => {
    logout();
    navigate("/login", { replace: true });
  };

  const loadUsers = async () => {
    setLoadingUsers(true);

    try {
      const data = await getUsers();
      setUsers(data);
      const drafts = data.reduce<Record<number, UserRole>>((acc, item) => {
        acc[item.id] = item.role;
        return acc;
      }, {});
      setRoleDraftByUserId(drafts);
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.status === 401) {
        handleLogout();
        return;
      }

      setError("Could not load users. Please try again.");
    } finally {
      setLoadingUsers(false);
    }
  };

  useEffect(() => {
    void loadUsers();
  }, []);

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError("");
    setMessage("");

    const username = form.username.trim();
    if (!username) {
      setError("Username is required.");
      return;
    }

    if (form.password.length < 8) {
      setError("Password must be at least 8 characters.");
      return;
    }

    setIsSubmitting(true);

    try {
      await createUser({
        username,
        password: form.password,
        role: form.role,
      });
      setMessage("User created successfully.");
      setForm({
        username: "",
        password: "",
        role: AppRoles.Cashier,
      });
      await loadUsers();
    } catch (err) {
      if (axios.isAxiosError(err) && typeof err.response?.data === "string") {
        setError(err.response.data);
      } else {
        setError("User creation failed. Please try again.");
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  const sortedUsers = useMemo(() => [...users].sort((a, b) => a.username.localeCompare(b.username)), [users]);

  const handleSaveRole = async (item: ManagedUser) => {
    const nextRole = roleDraftByUserId[item.id];
    if (!nextRole || nextRole === item.role) {
      setEditingUserId(null);
      return;
    }

    setError("");
    setMessage("");
    setSavingUserId(item.id);

    try {
      await updateUserRole(item.id, nextRole);
      setMessage(`Role updated for ${item.username}.`);
      await loadUsers();
      setEditingUserId(null);
    } catch (err) {
      if (axios.isAxiosError(err) && typeof err.response?.data === "string") {
        setError(err.response.data);
      } else {
        setError("Role update failed. Please try again.");
      }
    } finally {
      setSavingUserId(null);
    }
  };

  const handleDeleteUser = async (item: ManagedUser) => {
    const confirmed = window.confirm(`Delete user "${item.username}"?`);
    if (!confirmed) {
      return;
    }

    setError("");
    setMessage("");
    setDeletingUserId(item.id);

    try {
      await deleteUser(item.id);
      setMessage(`User ${item.username} deleted successfully.`);
      await loadUsers();
      if (editingUserId === item.id) {
        setEditingUserId(null);
      }
    } catch (err) {
      if (axios.isAxiosError(err) && typeof err.response?.data === "string") {
        setError(err.response.data);
      } else {
        setError("User deletion failed. Please try again.");
      }
    } finally {
      setDeletingUserId(null);
    }
  };

  return (
    <div className="hw-page">
      <Navbar username={user?.username} onLogout={handleLogout} />

      <div className="mx-auto max-w-6xl space-y-6 px-6 py-10">
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          <div className="hw-card">
            <h2 className="text-2xl font-bold text-slate-900 mb-1">Create User</h2>
            <p className="text-sm text-slate-600 mb-6">Admin can create Admin, Manager, or Cashier accounts.</p>

            <form onSubmit={handleSubmit} className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">Username</label>
                <input
                  type="text"
                  value={form.username}
                  onChange={(e) => setForm((prev) => ({ ...prev, username: e.target.value }))}
                  placeholder="Enter username"
                  className="hw-input px-4 py-2.5"
                  autoComplete="username"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">Password</label>
                <input
                  type="password"
                  value={form.password}
                  onChange={(e) => setForm((prev) => ({ ...prev, password: e.target.value }))}
                  placeholder="Minimum 8 characters"
                  className="hw-input px-4 py-2.5"
                  autoComplete="new-password"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">Role</label>
                <select
                  value={form.role}
                  onChange={(e) => setForm((prev) => ({ ...prev, role: e.target.value as CreateUserPayload["role"] }))}
                  className="hw-input px-4 py-2.5"
                >
                  <option value={AppRoles.Admin}>Admin</option>
                  <option value={AppRoles.Manager}>Manager</option>
                  <option value={AppRoles.Cashier}>Cashier</option>
                </select>
              </div>

              <button
                type="submit"
                disabled={isSubmitting}
                className="hw-btn-primary w-full py-2.5 disabled:cursor-not-allowed disabled:opacity-60"
              >
                {isSubmitting ? "Creating user..." : "Create User"}
              </button>
            </form>
          </div>

          <div className="hw-card">
            <h2 className="text-2xl font-bold text-slate-900 mb-1">User Roles</h2>
            <p className="text-sm text-slate-600 mb-6">Change role from dropdown and save.</p>

            {loadingUsers ? (
              <p className="text-slate-600 text-sm">Loading users...</p>
            ) : (
              <div className="max-h-[420px] overflow-auto">
                <table className="hw-table text-left">
                  <thead className="sticky top-0 bg-[#f8fbfe] text-xs uppercase tracking-[0.1em]">
                    <tr>
                      <th className="py-2">Username</th>
                      <th className="py-2">Role</th>
                      <th className="py-2 text-right">Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {sortedUsers.map((item) => (
                      <tr key={item.id}>
                        <td className="py-3 font-medium">{item.username}</td>
                        <td className="py-3">
                          {editingUserId === item.id ? (
                            <select
                              value={roleDraftByUserId[item.id] ?? item.role}
                              onChange={(e) =>
                                setRoleDraftByUserId((prev) => ({
                                  ...prev,
                                  [item.id]: e.target.value as UserRole,
                                }))
                              }
                              className="hw-input px-3 py-1.5"
                            >
                              <option value={AppRoles.Admin}>Admin</option>
                              <option value={AppRoles.Manager}>Manager</option>
                              <option value={AppRoles.Cashier}>Cashier</option>
                            </select>
                          ) : (
                            <span>{item.role}</span>
                          )}
                        </td>
                        <td className="py-3 text-right">
                          <div className="flex items-center justify-end gap-2">
                            {editingUserId === item.id ? (
                              <>
                                <button
                                  type="button"
                                  disabled={savingUserId === item.id}
                                  onClick={() => void handleSaveRole(item)}
                                  className="hw-btn-secondary px-3 py-1.5 disabled:cursor-not-allowed disabled:opacity-50"
                                >
                                  {savingUserId === item.id ? "Saving..." : "Save"}
                                </button>
                                <button
                                  type="button"
                                  onClick={() => {
                                    setEditingUserId(null);
                                    setRoleDraftByUserId((prev) => ({
                                      ...prev,
                                      [item.id]: item.role,
                                    }));
                                  }}
                                  className="hw-btn-ghost px-3 py-1.5"
                                >
                                  Cancel
                                </button>
                              </>
                            ) : (
                              <button
                                type="button"
                                onClick={() => setEditingUserId(item.id)}
                                className="hw-btn-primary px-3 py-1.5"
                              >
                                Edit
                              </button>
                            )}
                            <button
                              type="button"
                              disabled={deletingUserId === item.id}
                              onClick={() => void handleDeleteUser(item)}
                              className="hw-btn-danger px-3 py-1.5 disabled:cursor-not-allowed disabled:opacity-50"
                            >
                              {deletingUserId === item.id ? "Deleting..." : "Delete"}
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </div>

        {error && <div className="rounded-lg bg-red-100 px-3 py-2 text-sm text-red-700">{error}</div>}
        {message && <div className="rounded-lg bg-green-100 px-3 py-2 text-sm text-green-700">{message}</div>}
      </div>
    </div>
  );
};

export default UsersPage;
