export const AppRoles = {
  Admin: "Admin",
  Manager: "Manager",
  Cashier: "Cashier",
} as const;

export type AppRole = (typeof AppRoles)[keyof typeof AppRoles];

export const canAccessInventory = (role?: string) => {
  return role === AppRoles.Admin || role === AppRoles.Manager;
};

export const canAccessDashboard = (role?: string) => {
  return role === AppRoles.Admin || role === AppRoles.Manager;
};

export const canAccessSales = (role?: string) => {
  return role === AppRoles.Admin || role === AppRoles.Manager || role === AppRoles.Cashier;
};

export const canAccessTransactions = (role?: string) => {
  return role === AppRoles.Admin || role === AppRoles.Manager;
};

export const canManageUsers = (role?: string) => {
  return role === AppRoles.Admin;
};
