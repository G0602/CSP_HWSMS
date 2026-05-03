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
  resetUserPassword,
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
  const [confirmPassword, setConfirmPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [users, setUsers] = useState<ManagedUser[]>([]);
  const [loadingUsers, setLoadingUsers] = useState(false);
  const [roleDraftByUserId, setRoleDraftByUserId] = useState<Record<number, UserRole>>({});
  const [editingUserId, setEditingUserId] = useState<number | null>(null);
  const [savingUserId, setSavingUserId] = useState<number | null>(null);
  const [deletingUserId, setDeletingUserId] = useState<number | null>(null);
  const [resetPasswordUserId, setResetPasswordUserId] = useState<number | null>(null);
  const [resetPasswordForm, setResetPasswordForm] = useState({
    newPassword: "",
    confirmPassword: "",
  });
  const [showResetPassword, setShowResetPassword] = useState(false);
  const [showResetConfirmPassword, setShowResetConfirmPassword] = useState(false);
  const [resettingPasswordUserId, setResettingPasswordUserId] = useState<number | null>(null);

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

    if (form.password !== confirmPassword) {
      setError("Passwords do not match.");
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
      setConfirmPassword("");
      setShowPassword(false);
      setShowConfirmPassword(false);
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

  const handleResetPassword = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError("");
    setMessage("");

    if (resetPasswordForm.newPassword.length < 8) {
      setError("Password must be at least 8 characters.");
      return;
    }

    if (resetPasswordForm.newPassword !== resetPasswordForm.confirmPassword) {
      setError("Passwords do not match.");
      return;
    }

    if (!resetPasswordUserId) {
      setError("No user selected.");
      return;
    }

    setResettingPasswordUserId(resetPasswordUserId);

    try {
      await resetUserPassword(resetPasswordUserId, {
        newPassword: resetPasswordForm.newPassword,
        confirmPassword: resetPasswordForm.confirmPassword,
      });
      const targetUser = users.find((u) => u.id === resetPasswordUserId);
      setMessage(`Password reset successfully for ${targetUser?.username}.`);
      setResetPasswordUserId(null);
      setResetPasswordForm({
        newPassword: "",
        confirmPassword: "",
      });
      setShowResetPassword(false);
      setShowResetConfirmPassword(false);
    } catch (err) {
      if (axios.isAxiosError(err) && typeof err.response?.data === "string") {
        setError(err.response.data);
      } else {
        setError("Password reset failed. Please try again.");
      }
    } finally {
      setResettingPasswordUserId(null);
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

      <div className="mx-auto max-w-7xl px-6 py-10">
        {/* Header Section */}
        <div className="mb-8">
          <h1 className="text-3xl font-bold text-slate-900">User Management</h1>
          <p className="mt-2 text-slate-600">Create, manage roles, and reset passwords for users.</p>
        </div>

        {/* Alerts */}
        <div className="space-y-3 mb-6">
          {error && (
            <div className="rounded-lg bg-red-50 border border-red-200 px-4 py-3 text-sm text-red-700">
              <p className="font-medium">Error</p>
              <p>{error}</p>
            </div>
          )}
          {message && (
            <div className="rounded-lg bg-green-50 border border-green-200 px-4 py-3 text-sm text-green-700">
              <p className="font-medium">Success</p>
              <p>{message}</p>
            </div>
          )}
        </div>

        {/* Main Content Grid */}
        <div className="space-y-8">
          {/* Top Section: Create User & Users List */}
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
            {/* Create User Form */}
            <div className="lg:col-span-3">
              <div className="hw-card h-full flex flex-col">
                <div className="mb-6">
                  <h2 className="text-xl font-bold text-slate-900">Create New User</h2>
                  <p className="mt-1 text-sm text-slate-600">Add a new team member account.</p>
                </div>

                <form onSubmit={handleSubmit} className="flex-1 flex flex-col space-y-4">
                  {/* Username Field */}
                  <div className="flex-1">
                    <label className="block text-sm font-semibold text-slate-700 mb-2">Username</label>
                    <input
                      type="text"
                      value={form.username}
                      onChange={(e) => setForm((prev) => ({ ...prev, username: e.target.value }))}
                      placeholder="Enter username"
                      className="hw-input w-full px-3 py-2.5"
                      autoComplete="username"
                    />
                  </div>

                  {/* Password Field */}
                  <div className="flex-1">
                    <label className="block text-sm font-semibold text-slate-700 mb-2">Password</label>
                    <div className="relative">
                      <input
                        type={showPassword ? "text" : "password"}
                        value={form.password}
                        onChange={(e) => setForm((prev) => ({ ...prev, password: e.target.value }))}
                        placeholder="Minimum 8 characters"
                        className="hw-input w-full px-3 py-2.5 pr-16"
                        autoComplete="new-password"
                      />
                      <button
                        type="button"
                        onClick={() => setShowPassword((value) => !value)}
                        className="absolute right-3 top-1/2 -translate-y-1/2 text-xs font-semibold text-[#1f6b8c] hover:text-[#15516c] whitespace-nowrap"
                      >
                        {showPassword ? "Hide" : "Show"}
                      </button>
                    </div>
                  </div>

                  {/* Confirm Password Field */}
                  <div className="flex-1">
                    <label className="block text-sm font-semibold text-slate-700 mb-2">Confirm Password</label>
                    <div className="relative">
                      <input
                        type={showConfirmPassword ? "text" : "password"}
                        value={confirmPassword}
                        onChange={(e) => setConfirmPassword(e.target.value)}
                        placeholder="Minimum 8 characters"
                        className="hw-input w-full px-3 py-2.5 pr-16"
                        autoComplete="new-password"
                      />
                      <button
                        type="button"
                        onClick={() => setShowConfirmPassword((value) => !value)}
                        className="absolute right-3 top-1/2 -translate-y-1/2 text-xs font-semibold text-[#1f6b8c] hover:text-[#15516c] whitespace-nowrap"
                      >
                        {showConfirmPassword ? "Hide" : "Show"}
                      </button>
                    </div>
                  </div>

                  {/* Role Field */}
                  <div className="flex-1">
                    <label className="block text-sm font-semibold text-slate-700 mb-2">Role</label>
                    <select
                      value={form.role}
                      onChange={(e) => setForm((prev) => ({ ...prev, role: e.target.value as CreateUserPayload["role"] }))}
                      className="hw-input w-full px-3 py-2.5"
                    >
                      <option value={AppRoles.Admin}>Admin</option>
                      <option value={AppRoles.Manager}>Manager</option>
                      <option value={AppRoles.Cashier}>Cashier</option>
                    </select>
                  </div>

                  {/* Submit Button */}
                  <div className="pt-4">
                    <button
                      type="submit"
                      disabled={isSubmitting}
                      className="hw-btn-primary w-full py-2.5 font-semibold disabled:cursor-not-allowed disabled:opacity-60"
                    >
                      {isSubmitting ? "Creating..." : "Create User"}
                    </button>
                  </div>
                </form>
              </div>
            </div>

            {/* Users List */}
            <div className="lg:col-span-3">
              <div className="hw-card">
                <div className="mb-6">
                  <h2 className="text-xl font-bold text-slate-900">Users</h2>
                  <p className="mt-1 text-sm text-slate-600">Manage user roles and permissions.</p>
                </div>

                {loadingUsers ? (
                  <div className="flex items-center justify-center py-12">
                    <p className="text-slate-600 text-sm">Loading users...</p>
                  </div>
                ) : sortedUsers.length === 0 ? (
                  <div className="flex items-center justify-center py-12">
                    <p className="text-slate-500 text-sm">No users found.</p>
                  </div>
                ) : (
                  <div className="overflow-x-auto">
                    <table className="w-full text-sm">
                      <thead>
                        <tr className="border-b border-slate-200">
                          <th className="px-4 py-3 text-left font-semibold text-slate-700">Username</th>
                          <th className="px-4 py-3 text-left font-semibold text-slate-700">Role</th>
                          <th className="px-4 py-3 text-right font-semibold text-slate-700">Actions</th>
                        </tr>
                      </thead>
                      <tbody>
                        {sortedUsers.map((item) => (
                          <tr key={item.id} className="border-b border-slate-100 hover:bg-slate-50">
                            <td className="px-4 py-4 font-medium text-slate-900">{item.username}</td>
                            <td className="px-4 py-4">
                              {editingUserId === item.id ? (
                                <select
                                  value={roleDraftByUserId[item.id] ?? item.role}
                                  onChange={(e) =>
                                    setRoleDraftByUserId((prev) => ({
                                      ...prev,
                                      [item.id]: e.target.value as UserRole,
                                    }))
                                  }
                                  className="hw-input px-2.5 py-1.5 text-sm"
                                >
                                  <option value={AppRoles.Admin}>Admin</option>
                                  <option value={AppRoles.Manager}>Manager</option>
                                  <option value={AppRoles.Cashier}>Cashier</option>
                                </select>
                              ) : (
                                <span className="inline-block px-3 py-1 rounded-full text-sm font-medium bg-blue-100 text-blue-700">
                                  {item.role}
                                </span>
                              )}
                            </td>
                            <td className="px-4 py-4 text-right">
                              <div className="flex items-center justify-end gap-2 flex-wrap">
                                {editingUserId === item.id ? (
                                  <>
                                    <button
                                      type="button"
                                      disabled={savingUserId === item.id}
                                      onClick={() => void handleSaveRole(item)}
                                      className="hw-btn-secondary px-3 py-1.5 text-xs disabled:cursor-not-allowed disabled:opacity-50"
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
                                      className="hw-btn-ghost px-3 py-1.5 text-xs"
                                    >
                                      Cancel
                                    </button>
                                  </>
                                ) : (
                                  <>
                                    <button
                                      type="button"
                                      onClick={() => setEditingUserId(item.id)}
                                      className="hw-btn-primary px-3 py-1.5 text-xs"
                                      title="Edit user role"
                                    >
                                      Edit
                                    </button>
                                    <button
                                      type="button"
                                      onClick={() => {
                                        setResetPasswordUserId(item.id);
                                        setResetPasswordForm({ newPassword: "", confirmPassword: "" });
                                        setShowResetPassword(false);
                                        setShowResetConfirmPassword(false);
                                      }}
                                      className="hw-btn-secondary px-3 py-1.5 text-xs"
                                      title="Reset password"
                                    >
                                      Reset
                                    </button>
                                    <button
                                      type="button"
                                      disabled={deletingUserId === item.id}
                                      onClick={() => void handleDeleteUser(item)}
                                      className="hw-btn-danger px-3 py-1.5 text-xs disabled:cursor-not-allowed disabled:opacity-50"
                                      title="Delete user"
                                    >
                                      {deletingUserId === item.id ? "..." : "Delete"}
                                    </button>
                                  </>
                                )}
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
          </div>

          {/* Reset Password Modal */}
          {resetPasswordUserId && (
            <>
              {/* Backdrop Overlay */}
              <div
                className="fixed inset-0 bg-black bg-opacity-50 z-40"
                onClick={() => {
                  setResetPasswordUserId(null);
                  setResetPasswordForm({ newPassword: "", confirmPassword: "" });
                }}
              />

              {/* Modal Dialog */}
              <div className="fixed inset-0 flex items-center justify-center z-50 p-4">
                <div className="bg-white rounded-lg shadow-2xl w-full max-w-md">
                  {/* Modal Header */}
                  <div className="flex items-start justify-between p-6 border-b border-slate-200">
                    <div>
                      <h2 className="text-xl font-bold text-slate-900">Reset Password</h2>
                      <p className="mt-1 text-sm text-slate-600">
                        User: <span className="font-semibold text-slate-900">{users.find((u) => u.id === resetPasswordUserId)?.username}</span>
                      </p>
                    </div>
                    <button
                      type="button"
                      onClick={() => {
                        setResetPasswordUserId(null);
                        setResetPasswordForm({ newPassword: "", confirmPassword: "" });
                      }}
                      className="text-slate-500 hover:text-slate-700 font-bold text-lg"
                      title="Close"
                    >
                      ✕
                    </button>
                  </div>

                  {/* Modal Body */}
                  <form onSubmit={handleResetPassword} className="p-6 space-y-4">
                    {/* New Password Field */}
                    <div>
                      <label className="block text-sm font-semibold text-slate-700 mb-2">New Password</label>
                      <div className="relative">
                        <input
                          type={showResetPassword ? "text" : "password"}
                          value={resetPasswordForm.newPassword}
                          onChange={(e) =>
                            setResetPasswordForm((prev) => ({
                              ...prev,
                              newPassword: e.target.value,
                            }))
                          }
                          placeholder="Minimum 8 characters"
                          className="hw-input w-full px-3 py-2.5 pr-16"
                          autoComplete="new-password"
                        />
                        <button
                          type="button"
                          onClick={() => setShowResetPassword((value) => !value)}
                          className="absolute right-3 top-1/2 -translate-y-1/2 text-xs font-semibold text-[#1f6b8c] hover:text-[#15516c] whitespace-nowrap"
                        >
                          {showResetPassword ? "Hide" : "Show"}
                        </button>
                      </div>
                    </div>

                    {/* Confirm Password Field */}
                    <div>
                      <label className="block text-sm font-semibold text-slate-700 mb-2">Confirm Password</label>
                      <div className="relative">
                        <input
                          type={showResetConfirmPassword ? "text" : "password"}
                          value={resetPasswordForm.confirmPassword}
                          onChange={(e) =>
                            setResetPasswordForm((prev) => ({
                              ...prev,
                              confirmPassword: e.target.value,
                            }))
                          }
                          placeholder="Minimum 8 characters"
                          className="hw-input w-full px-3 py-2.5 pr-16"
                          autoComplete="new-password"
                        />
                        <button
                          type="button"
                          onClick={() => setShowResetConfirmPassword((value) => !value)}
                          className="absolute right-3 top-1/2 -translate-y-1/2 text-xs font-semibold text-[#1f6b8c] hover:text-[#15516c] whitespace-nowrap"
                        >
                          {showResetConfirmPassword ? "Hide" : "Show"}
                        </button>
                      </div>
                    </div>

                    {/* Action Buttons */}
                    <div className="flex gap-3 pt-4">
                      <button
                        type="submit"
                        disabled={resettingPasswordUserId !== null}
                        className="hw-btn-primary flex-1 px-6 py-2.5 font-semibold disabled:cursor-not-allowed disabled:opacity-60"
                      >
                        {resettingPasswordUserId !== null ? "Resetting..." : "Reset Password"}
                      </button>
                      <button
                        type="button"
                        onClick={() => {
                          setResetPasswordUserId(null);
                          setResetPasswordForm({ newPassword: "", confirmPassword: "" });
                        }}
                        className="hw-btn-ghost flex-1 px-6 py-2.5 font-semibold"
                      >
                        Cancel
                      </button>
                    </div>
                  </form>
                </div>
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  );
};

export default UsersPage;
