import axios from "axios";
import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { AppRoles } from "../auth/roles";
import Navbar from "../components/Navbar";
import { createUser, getCurrentUser, logout, type CreateUserPayload } from "../services/authService";

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

  const user = getCurrentUser();

  const handleLogout = () => {
    logout();
    navigate("/login", { replace: true });
  };

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

  return (
    <div className="bg-gray-50 min-h-screen">
      <Navbar username={user?.username} onLogout={handleLogout} />

      <div className="max-w-xl mx-auto px-6 py-10">
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

            {error && <div className="rounded-lg bg-red-100 px-3 py-2 text-sm text-red-700">{error}</div>}
            {message && <div className="rounded-lg bg-green-100 px-3 py-2 text-sm text-green-700">{message}</div>}

            <button
              type="submit"
              disabled={isSubmitting}
              className="w-full rounded-xl bg-blue-600 text-white font-semibold py-2.5 hover:bg-blue-700 transition-colors disabled:opacity-60 disabled:cursor-not-allowed"
            >
              {isSubmitting ? "Creating user..." : "Create User"}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
};

export default UsersPage;
