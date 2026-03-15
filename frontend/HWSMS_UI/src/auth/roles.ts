export const AppRoles = {
  Admin: "Admin",
  Manager: "Manager",
  Cashier: "Cashier",
  User: "User",
} as const;

export type AppRole = (typeof AppRoles)[keyof typeof AppRoles];

export const canAccessInventory = (role?: string) => {
  return role === AppRoles.Admin || role === AppRoles.Manager;
};

export const canAccessSales = (role?: string) => {
  return role === AppRoles.Admin || role === AppRoles.Manager || role === AppRoles.Cashier;
};
