import { NavLink } from "react-router-dom";
import { canAccessInventory, canAccessSales, canAccessTransactions, canManageUsers } from "../auth/roles";
import { getCurrentUser } from "../services/authService";

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
      "rounded-lg px-3 py-2 text-sm font-medium transition-colors",
      isActive ? "bg-blue-600 text-white shadow-sm" : "text-slate-600 hover:bg-slate-100 hover:text-slate-950",
    ].join(" ");

  return (
    <header className="sticky top-0 z-40 border-b border-slate-200 bg-white/95 px-4 py-3 shadow-sm backdrop-blur lg:px-8">
      <div className="mx-auto flex max-w-7xl flex-col gap-3 xl:flex-row xl:items-center xl:justify-between">
        <div className="flex flex-wrap items-center gap-3">
          <div className="grid h-10 w-10 place-items-center rounded-xl bg-blue-600 text-sm font-bold text-white shadow-sm">
            HS
          </div>
          <div className="min-w-[220px]">
            <h1 className="text-base font-semibold text-slate-950">Hardware Store Management</h1>
            <p className="text-xs text-slate-500">Inventory, sales, suppliers, and reports</p>
          </div>
        </div>

        <nav className="flex flex-wrap items-center gap-1">
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

        <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between xl:justify-end">
          {showSearch && (
            <input
              type="text"
              value={search}
              onChange={(e) => onSearchChange(e.target.value)}
              placeholder="Search..."
              className="w-full rounded-lg border border-slate-200 bg-slate-50 px-4 py-2 text-sm focus:border-blue-500 focus:bg-white focus:outline-none sm:w-64"
            />
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
              className="rounded-lg bg-slate-900 px-3 py-2 text-sm font-medium text-white transition-colors hover:bg-slate-700"
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
