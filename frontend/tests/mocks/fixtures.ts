import type { ModuleDescriptor, Session, User } from "@/entities/user";

/** A signed-in administrator who still owes a password change. */
export const adminPendingChange: User = {
  id: "11111111-1111-1111-1111-111111111111",
  username: "admin",
  fullName: "System Administrator",
  email: null,
  role: "Admin",
  isActive: true,
  mustChangePassword: true,
  isSystemAdmin: true,
  hasApprovalPin: false,
  lastLoginAtUtc: null,
  createdAtUtc: "2026-01-01T00:00:00Z",
  modules: [],
};

/** A fully provisioned administrator. */
export const adminProvisioned: User = {
  ...adminPendingChange,
  mustChangePassword: false,
};

/** A staff account limited to POS & Billing. */
export const cashierUser: User = {
  id: "22222222-2222-2222-2222-222222222222",
  username: "cashier01",
  fullName: "Ravi Kumar",
  email: null,
  role: "User",
  isActive: true,
  mustChangePassword: false,
  isSystemAdmin: false,
  hasApprovalPin: false,
  lastLoginAtUtc: null,
  createdAtUtc: "2026-01-02T00:00:00Z",
  modules: ["PosBilling"],
};

export const moduleCatalog: ModuleDescriptor[] = [
  {
    module: "PosBilling",
    name: "POS & Billing",
    group: "Operations",
    description: "Take orders, split and settle bills, and print receipts.",
    sortOrder: 10,
    adminOnly: false,
  },
  {
    module: "ReportsAnalytics",
    name: "Reports & Analytics",
    group: "Administration",
    description: "Sales, inventory, expense and staff performance reporting.",
    sortOrder: 90,
    adminOnly: false,
  },
  {
    module: "UserManagement",
    name: "User Management & Roles",
    group: "Administration",
    description: "Create staff accounts and control which modules they can open.",
    sortOrder: 110,
    adminOnly: true,
  },
];

export function sessionFor(user: User): Session {
  return {
    accessToken: "mock-access-token",
    accessTokenExpiresAtUtc: "2026-01-01T01:00:00Z",
    refreshToken: "mock-refresh-token",
    refreshTokenExpiresAtUtc: "2026-01-15T00:00:00Z",
    user,
  };
}
