import { describe, expect, it } from "vitest";
import {
  AppRoles,
  canAccessInventory,
  canAccessSales,
  canAccessTransactions,
  canManageUsers,
} from "./roles";

describe("role helpers", () => {
  it("allows only admin and manager for inventory", () => {
    expect(canAccessInventory(AppRoles.Admin)).toBe(true);
    expect(canAccessInventory(AppRoles.Manager)).toBe(true);
    expect(canAccessInventory(AppRoles.Cashier)).toBe(false);
    expect(canAccessInventory(undefined)).toBe(false);
  });

  it("allows all roles for sales", () => {
    expect(canAccessSales(AppRoles.Admin)).toBe(true);
    expect(canAccessSales(AppRoles.Manager)).toBe(true);
    expect(canAccessSales(AppRoles.Cashier)).toBe(true);
    expect(canAccessSales("Unknown")).toBe(false);
  });

  it("allows only admin and manager for transactions", () => {
    expect(canAccessTransactions(AppRoles.Admin)).toBe(true);
    expect(canAccessTransactions(AppRoles.Manager)).toBe(true);
    expect(canAccessTransactions(AppRoles.Cashier)).toBe(false);
  });

  it("allows only admin for user management", () => {
    expect(canManageUsers(AppRoles.Admin)).toBe(true);
    expect(canManageUsers(AppRoles.Manager)).toBe(false);
    expect(canManageUsers(AppRoles.Cashier)).toBe(false);
  });
});
