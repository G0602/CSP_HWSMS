import { NavLink } from "react-router-dom";
import { canAccessInventory, canAccessSales, canAccessTransactions } from "../auth/roles";
import { getCurrentUser } from "../services/authService";

type NavbarProps = {
  search?: string;
  onSearchChange?: (value: string) => void;
  username?: string;
  onLogout?: () => void;
};

const Navbar = ({ search, onSearchChange, username, onLogout }: NavbarProps) => {
  const showSearch = typeof search === "string" && typeof onSearchChange === "function";
  const role = getCurrentUser()?.role;

  return (
    <div className="bg-white border-b border-gray-200 px-8 py-4 flex justify-between items-center">
      <div className="flex items-center gap-3">
        <div className="bg-blue-600 text-white p-2 rounded-xl">🛠️</div>
        <h1 className="text-lg font-semibold text-gray-800">Hardware Store Product Management</h1>
        <div className="ml-5 flex gap-2">
          {canAccessInventory(role) && (
            <NavLink
              to="/dashboard"
              className={({ isActive }) =>
                `text-sm px-3 py-1.5 rounded-lg ${isActive ? "bg-blue-600 text-white" : "bg-slate-100 text-slate-700"}`
              }
            >
              Inventory
            </NavLink>
          )}
          {canAccessSales(role) && (
            <NavLink
              to="/sales"
              className={({ isActive }) =>
                `text-sm px-3 py-1.5 rounded-lg ${isActive ? "bg-blue-600 text-white" : "bg-slate-100 text-slate-700"}`
              }
            >
              Sales
            </NavLink>
          )}
          {canAccessTransactions(role) && (
            <NavLink
              to="/transactions"
              className={({ isActive }) =>
                `text-sm px-3 py-1.5 rounded-lg ${isActive ? "bg-blue-600 text-white" : "bg-slate-100 text-slate-700"}`
              }
            >
              Transactions
            </NavLink>
          )}
        </div>
      </div>

      <div className="flex items-center gap-4">
        {showSearch && (
          <input
            type="text"
            value={search}
            onChange={(e) => onSearchChange(e.target.value)}
            placeholder="Search inventory..."
            className="bg-gray-100 px-4 py-2 rounded-xl text-sm focus:outline-none"
          />
        )}
        {username && <span className="text-sm text-gray-600">{username}</span>}
        <button
          type="button"
          onClick={onLogout}
          className="text-sm bg-slate-800 text-white px-3 py-2 rounded-lg hover:bg-slate-900 transition-colors"
        >
          Logout
        </button>
      </div>
    </div>
  );
};

export default Navbar;
