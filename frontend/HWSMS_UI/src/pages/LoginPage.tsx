import axios from "axios";
import { useState } from "react";
import type { FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { login } from "../services/authService";
import { canAccessInventory, canAccessSales } from "../auth/roles";

const LoginPage = () => {
  const navigate = useNavigate();

  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState("");

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError("");

    if (!username.trim() || !password) {
      setError("Please enter both username and password.");
      return;
    }

    setIsSubmitting(true);

    try {
      const auth = await login({ username: username.trim(), password });

      if (canAccessInventory(auth.role)) {
        navigate("/dashboard", { replace: true });
      } else if (canAccessSales(auth.role)) {
        navigate("/sales", { replace: true });
      } else {
        navigate("/access-denied", { replace: true });
      }
    } catch (err) {
      if (axios.isAxiosError(err) && typeof err.response?.data === "string") {
        setError(err.response.data);
      } else {
        setError("Login failed. Please check your credentials and try again.");
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="hw-page flex items-center justify-center px-6 py-10">
      <div className="w-full max-w-md rounded-3xl border border-[#bfccd9] bg-white/92 p-8 shadow-[0_35px_70px_-45px_rgba(16,32,51,0.9)] backdrop-blur">
        <p className="text-sm font-semibold uppercase tracking-[0.22em] text-[#1f6b8c]">HWSMS Portal</p>
        <h1 className="mt-2 text-3xl font-bold text-slate-900">Sign In</h1>
        <p className="mt-2 text-sm text-slate-600">Access your inventory dashboard with your account.</p>

        <form onSubmit={handleSubmit} className="mt-7 space-y-5">
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">Username</label>
            <input
              type="text"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              placeholder="Enter username"
              className="hw-input px-4 py-2.5"
              autoComplete="username"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">Password</label>
            <div className="relative">
              <input
                type={showPassword ? "text" : "password"}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="Enter password"
                className="hw-input px-4 py-2.5 pr-20"
                autoComplete="current-password"
              />
              <button
                type="button"
                onClick={() => setShowPassword((value) => !value)}
                className="absolute right-3 top-1/2 -translate-y-1/2 text-sm font-semibold text-[#1f6b8c] hover:text-[#15516c]"
              >
                {showPassword ? "Hide" : "Show"}
              </button>
            </div>
          </div>

          {error && <div className="rounded-lg bg-red-100 px-3 py-2 text-sm text-red-700">{error}</div>}

          <button
            type="submit"
            disabled={isSubmitting}
            className="hw-btn-primary w-full py-2.5 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {isSubmitting ? "Signing in..." : "Sign In"}
          </button>
        </form>

        <p className="mt-5 text-sm text-slate-600">New users can only be created by an existing admin.</p>
      </div>
    </div>
  );
};

export default LoginPage;
