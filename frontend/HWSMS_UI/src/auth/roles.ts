export const AppRoles = {
  Admin: "Admin",
  Manager: "Manager",
  Cashier: "Cashier",
} as const;

export type AppRole = (typeof AppRoles)[keyof typeof AppRoles];

const normalizeRole = (role?: string): string => {
  return role?.trim().toLowerCase() ?? "";
};

const isRoleEqual = (role?: string, expectedRole?: string): boolean => {
  return normalizeRole(role) === normalizeRole(expectedRole);
};

export const canAccessInventory = (role?: string) => {
  return isRoleEqual(role, AppRoles.Admin) || isRoleEqual(role, AppRoles.Manager);
};

export const canAccessSales = (role?: string) => {
  return isRoleEqual(role, AppRoles.Admin) || isRoleEqual(role, AppRoles.Manager) || isRoleEqual(role, AppRoles.Cashier);
};

export const canAccessTransactions = (role?: string) => {
  return isRoleEqual(role, AppRoles.Admin) || isRoleEqual(role, AppRoles.Manager);
};

export const canManageUsers = (role?: string) => {
  return isRoleEqual(role, AppRoles.Admin);
};

export const isAdmin = (role?: string) => {
  return isRoleEqual(role, AppRoles.Admin);
};
