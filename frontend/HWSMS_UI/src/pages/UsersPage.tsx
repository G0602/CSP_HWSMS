import axios from "axios";
import { useEffect, useMemo, useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { AppRoles } from "../auth/roles";
import Navbar from "../components/Navbar";
import {
  createUser,
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
  const [savingUserId, setSavingUserId] = useState<number | null>(null);

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

  const hasRoleChanged = (item: ManagedUser) =>
    roleDraftByUserId[item.id] !== undefined && roleDraftByUserId[item.id] !== item.role;

  const sortedUsers = useMemo(() => [...users].sort((a, b) => a.username.localeCompare(b.username)), [users]);

  const handleSaveRole = async (item: ManagedUser) => {
    const nextRole = roleDraftByUserId[item.id];
    if (!nextRole || nextRole === item.role) {
      return;
    }

    setError("");
    setMessage("");
    setSavingUserId(item.id);

    try {
      await updateUserRole(item.id, nextRole);
      setMessage(`Role updated for ${item.username}.`);
      await loadUsers();
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

  return (
    <div className="bg-gray-50 min-h-screen">
      <Navbar username={user?.username} onLogout={handleLogout} />

      <div className="max-w-6xl mx-auto px-6 py-10 space-y-6">
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-6">
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
                  className="w-full rounded-xl border border-slate-300 px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
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
                  className="w-full rounded-xl border border-slate-300 px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                  autoComplete="new-password"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-slate-700 mb-1">Role</label>
                <select
                  value={form.role}
                  onChange={(e) => setForm((prev) => ({ ...prev, role: e.target.value as CreateUserPayload["role"] }))}
                  className="w-full rounded-xl border border-slate-300 px-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                >
                  <option value={AppRoles.Admin}>Admin</option>
                  <option value={AppRoles.Manager}>Manager</option>
                  <option value={AppRoles.Cashier}>Cashier</option>
                </select>
              </div>

              <button
                type="submit"
                disabled={isSubmitting}
                className="w-full rounded-xl bg-blue-600 text-white font-semibold py-2.5 hover:bg-blue-700 transition-colors disabled:opacity-60 disabled:cursor-not-allowed"
              >
                {isSubmitting ? "Creating user..." : "Create User"}
              </button>
            </form>
          </div>

          <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-6">
            <h2 className="text-2xl font-bold text-slate-900 mb-1">User Roles</h2>
            <p className="text-sm text-slate-600 mb-6">Change role from dropdown and save.</p>

            {loadingUsers ? (
              <p className="text-slate-600 text-sm">Loading users...</p>
            ) : (
              <div className="max-h-[420px] overflow-auto">
                <table className="w-full text-left">
                  <thead className="text-sm text-gray-500 uppercase sticky top-0 bg-white">
                    <tr>
                      <th className="py-2">Username</th>
                      <th className="py-2">Role</th>
                      <th className="py-2"></th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-100">
                    {sortedUsers.map((item) => (
                      <tr key={item.id}>
                        <td className="py-3 font-medium">{item.username}</td>
                        <td className="py-3">
                          <select
                            value={roleDraftByUserId[item.id] ?? item.role}
                            onChange={(e) =>
                              setRoleDraftByUserId((prev) => ({
                                ...prev,
                                [item.id]: e.target.value as UserRole,
                              }))
                            }
                            className="rounded-lg border border-slate-300 px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                          >
                            <option value={AppRoles.Admin}>Admin</option>
                            <option value={AppRoles.Manager}>Manager</option>
                            <option value={AppRoles.Cashier}>Cashier</option>
                          </select>
                        </td>
                        <td className="py-3 text-right">
                          <button
                            type="button"
                            disabled={!hasRoleChanged(item) || savingUserId === item.id}
                            onClick={() => void handleSaveRole(item)}
                            className="rounded-lg bg-slate-800 text-white px-3 py-1.5 text-sm disabled:opacity-50 disabled:cursor-not-allowed hover:bg-slate-900"
                          >
                            {savingUserId === item.id ? "Saving..." : "Save"}
                          </button>
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
