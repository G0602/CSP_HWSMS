import { Link } from "react-router-dom";

const AccessDeniedPage = () => {
  return (
    <div className="min-h-screen bg-gradient-to-br from-red-50 via-orange-50 to-amber-100 flex items-center justify-center px-6 py-10">
      <div className="w-full max-w-lg rounded-2xl border border-white/80 bg-white/90 shadow-xl p-8 text-center">
        <p className="text-sm font-semibold tracking-[0.2em] text-red-700 uppercase">Access Control</p>
        <h1 className="mt-2 text-3xl font-bold text-slate-900">Access Denied</h1>
        <p className="mt-3 text-slate-600">
          You do not have permission to access this page with your current role.
        </p>

        <div className="mt-6 flex justify-center gap-3">
          <Link
            to="/inventory"
            className="rounded-xl bg-slate-800 text-white px-4 py-2.5 font-medium hover:bg-slate-900 transition-colors"
          >
            Go to Inventory
          </Link>
          <Link
            to="/sales"
            className="rounded-xl bg-blue-600 text-white px-4 py-2.5 font-medium hover:bg-blue-700 transition-colors"
          >
            Go to Sales
          </Link>
        </div>
      </div>
    </div>
  );
};

export default AccessDeniedPage;
