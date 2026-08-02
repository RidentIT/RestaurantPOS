import { describe, expect, it } from "vitest";
import type { ModuleDescriptor, User } from "@/entities/user";
import { assignableModules, canAccessModule, groupModules } from "@/entities/user";

function makeUser(overrides: Partial<User> = {}): User {
  return {
    id: "1",
    username: "cashier01",
    fullName: "Ravi Kumar",
    email: null,
    role: "User",
    isActive: true,
    mustChangePassword: false,
    isSystemAdmin: false,
    hasApprovalPin: false,
    lastLoginAtUtc: null,
    createdAtUtc: "2026-01-01T00:00:00Z",
    modules: [],
    ...overrides,
  };
}

const catalog: ModuleDescriptor[] = [
  { module: "PosBilling", name: "POS & Billing", group: "Operations", description: "", sortOrder: 10, adminOnly: false },
  { module: "ExpensesManagement", name: "Expenses Management", group: "Operations", description: "", sortOrder: 80, adminOnly: false },
  { module: "ReportsAnalytics", name: "Reports & Analytics", group: "Administration", description: "", sortOrder: 90, adminOnly: false },
  { module: "UserManagement", name: "User Management & Roles", group: "Administration", description: "", sortOrder: 110, adminOnly: true },
];

describe("canAccessModule", () => {
  it("is false for a signed-out user regardless of module", () => {
    expect(canAccessModule(null, "PosBilling")).toBe(false);
  });

  it("is limited to granted modules for a staff user", () => {
    const user = makeUser({ modules: ["PosBilling"] });

    expect(canAccessModule(user, "PosBilling")).toBe(true);
    expect(canAccessModule(user, "ReportsAnalytics")).toBe(false);
  });

  it("is unconditional for an administrator, even with no listed modules", () => {
    const admin = makeUser({ role: "Admin", modules: [] });

    expect(canAccessModule(admin, "UserManagement")).toBe(true);
  });
});

describe("assignableModules", () => {
  it("excludes admin-only modules", () => {
    const result = assignableModules(catalog);

    expect(result.map((m) => m.module)).toEqual(["PosBilling", "ExpensesManagement", "ReportsAnalytics"]);
  });
});

describe("groupModules", () => {
  it("groups by the catalog's group label and preserves sort order within and across groups", () => {
    const shuffled = [...catalog].reverse();

    const groups = groupModules(shuffled);

    expect(groups.map((g) => g.group)).toEqual(["Operations", "Administration"]);
    expect(groups[0].modules.map((m) => m.module)).toEqual(["PosBilling", "ExpensesManagement"]);
    expect(groups[1].modules.map((m) => m.module)).toEqual(["ReportsAnalytics", "UserManagement"]);
  });
});
