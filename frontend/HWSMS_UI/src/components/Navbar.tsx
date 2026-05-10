import { NavLink } from "react-router-dom";
import {
  canAccessDashboard,
  canAccessInventory,
  canAccessSales,
  canAccessTransactions,
  canManageUsers,
} from "../auth/roles";
import { getCurrentUser } from "../services/authService";
// Use the public logo placed at /logo.png

type NavbarProps = {
  search?: string;
  onSearchChange?: (value: string) => void;
  username?: string;
  onLogout?: () => void;
};

const Navbar = ({ search, onSearchChange, username, onLogout }: NavbarProps) => {
  const showSearch = typeof search === "string" && typeof onSearchChange === "function";
  const user = getCurrentUser();
  const role = user?.role;
  const displayName = username ?? user?.username;
  const linkClassName = ({ isActive }: { isActive: boolean }) =>
    [
      "rounded-xl px-3 py-2 text-sm font-semibold transition-all",
      isActive
        ? "bg-[linear-gradient(140deg,#e46b1f,#c2500f)] text-white shadow-md"
        : "text-slate-600 hover:bg-white/80 hover:text-slate-900",
      "rounded-xl px-3 py-2 text-sm font-semibold transition-all",
      isActive
        ? "bg-[linear-gradient(140deg,#e46b1f,#c2500f)] text-white shadow-md"
        : "text-slate-600 hover:bg-white/80 hover:text-slate-900",
    ].join(" ");

  return (
    <header className="sticky top-0 z-40 mx-3 mt-3 rounded-2xl border border-[#bfccd9] bg-[#f4f8fc]/88 px-4 py-3 shadow-sm backdrop-blur lg:mx-6 lg:px-8">
      <div className="mx-auto flex max-w-7xl flex-col gap-3 xl:flex-row xl:items-center xl:justify-between">
        <div className="flex flex-wrap items-center gap-3">
          <div className="grid h-11 w-11 place-items-center overflow-hidden rounded-xl bg-white shadow-md ring-1 ring-slate-200">
            <img src="/logo.png" alt="Janatha Hardware logo" className="h-full w-full object-cover" />
          </div>
          <div className="min-w-[220px]">
            <h1 className="text-base font-bold text-slate-900">Janatha Hardware</h1>
            <p className="text-xs tracking-wide text-slate-500">INVENTORY | SALES | SUPPLIERS | REPORTS</p>
          </div>
        </div>

        <nav className="flex flex-1 flex-nowrap items-center justify-center gap-1 whitespace-nowrap">
          {canAccessDashboard(role) && (
            <NavLink to="/dashboard" className={linkClassName}>
              Dashboard
            </NavLink>
          )}
          {canAccessInventory(role) && (
            <NavLink to="/inventory" className={linkClassName}>
              Inventory
            </NavLink>
          )}
          {canAccessSales(role) && (
            <NavLink to="/sales" className={linkClassName}>
              Sales
            </NavLink>
          )}
          {canAccessInventory(role) && (
            <NavLink to="/suppliers" className={linkClassName}>
              Suppliers
            </NavLink>
          )}
          {canAccessTransactions(role) && (
            <NavLink to="/transactions" className={linkClassName}>
              Transactions
            </NavLink>
          )}
          {canAccessTransactions(role) && (
            <NavLink to="/reports/daily" className={linkClassName}>
              Reports
            </NavLink>
          )}
          {canManageUsers(role) && (
            <NavLink to="/users" className={linkClassName}>
              Users
            </NavLink>
          )}
        </nav>

        <div className="flex items-center gap-3">
          {showSearch ? (
            <input
              type="text"
              value={search}
              onChange={(e) => onSearchChange(e.target.value)}
              placeholder="Search..."
              className="hw-input sm:w-64"
            />
          ) : (
            <div className="w-56" aria-hidden="true" />
          )}
          <div className="flex items-center gap-3">
            {displayName && (
              <div className="text-right">
                <p className="text-sm font-medium text-slate-800">{displayName}</p>
                {role && <p className="text-xs text-slate-500">{role}</p>}
              </div>
            )}
            <button
              type="button"
              onClick={onLogout}
              className="hw-btn-primary"
              className="hw-btn-primary"
            >
              Logout
            </button>
          </div>
        </div>
      </div>
    </header>
  );
};

export default Navbar;
