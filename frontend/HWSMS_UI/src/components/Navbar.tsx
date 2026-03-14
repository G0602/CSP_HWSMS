type NavbarProps = {
  search: string;
  onSearchChange: (value: string) => void;
  username?: string;
  onLogout?: () => void;
};

const Navbar = ({ search, onSearchChange, username, onLogout }: NavbarProps) => {
  return (
    <div className="bg-white border-b border-gray-200 px-8 py-4 flex justify-between items-center">
      <div className="flex items-center gap-3">
        <div className="bg-blue-600 text-white p-2 rounded-xl">🛠️</div>
        <h1 className="text-lg font-semibold text-gray-800">Hardware Store Product Management</h1>
      </div>

      <div className="flex items-center gap-4">
        <input
          type="text"
          value={search}
          onChange={(e) => onSearchChange(e.target.value)}
          placeholder="Search inventory..."
          className="bg-gray-100 px-4 py-2 rounded-xl text-sm focus:outline-none"
        />
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
