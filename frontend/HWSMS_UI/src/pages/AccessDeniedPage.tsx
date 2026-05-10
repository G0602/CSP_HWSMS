import { Link } from "react-router-dom";

const AccessDeniedPage = () => {
  return (
    <div className="hw-page flex items-center justify-center px-6 py-10">
      <div className="w-full max-w-lg rounded-3xl border border-[#f3b9bc] bg-white/92 p-8 text-center shadow-[0_35px_70px_-45px_rgba(182,43,49,0.75)]">
        <p className="text-sm font-semibold uppercase tracking-[0.2em] text-[#b62b31]">Access Control</p>
        <h1 className="mt-2 text-3xl font-bold text-slate-900">Access Denied</h1>
        <p className="mt-3 text-slate-600">
          You do not have permission to access this page with your current role.
        </p>

        <div className="mt-6 flex justify-center gap-3">
          <Link
            to="/inventory"
            className="hw-btn-ghost px-4 py-2.5"
          >
            Go to Inventory
          </Link>
          <Link
            to="/sales"
            className="hw-btn-primary px-4 py-2.5"
          >
            Go to Sales
          </Link>
        </div>
      </div>
    </div>
  );
};

export default AccessDeniedPage;
