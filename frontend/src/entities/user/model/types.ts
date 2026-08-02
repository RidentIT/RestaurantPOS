/**
 * Mirrors the backend contracts in `RestaurantPOS.Application`. Enums cross the wire as their
 * names, so these are string unions rather than numeric enums.
 */

export type UserRole = "Admin" | "User";

/**
 * Identifier of an assignable module. Kept as a plain string rather than a closed union: the
 * catalog is served by `GET /modules`, so adding a module on the backend must not require a
 * frontend change.
 */
export type ModuleKey = string;

/** A staff account. */
export interface User {
  id: string;
  username: string;
  fullName: string;
  email: string | null;
  role: UserRole;
  isActive: boolean;
  /** True until the user has chosen their own password. */
  mustChangePassword: boolean;
  /** The built-in administrator, which cannot be deactivated or demoted. */
  isSystemAdmin: boolean;
  hasApprovalPin: boolean;
  lastLoginAtUtc: string | null;
  createdAtUtc: string;
  /** Modules the user can open. For administrators this is the entire catalog. */
  modules: ModuleKey[];
}

/** Display metadata for one module, as served by the backend catalog. */
export interface ModuleDescriptor {
  module: ModuleKey;
  name: string;
  group: string;
  description: string;
  sortOrder: number;
  /** Reserved for administrators; never offered in the permission editor. */
  adminOnly: boolean;
}

/** A signed-in session. */
export interface Session {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
  user: User;
}

/** Identifies the administrator who authorised a PIN-gated action. */
export interface Approval {
  approvedByUserId: string;
  approvedByName: string;
  approvedAtUtc: string;
}

export interface CreateUserPayload {
  username: string;
  fullName: string;
  email: string | null;
  password: string;
  role: UserRole;
  modules: ModuleKey[];
}

export interface UpdateUserPayload {
  fullName: string;
  email: string | null;
  role: UserRole;
  modules: ModuleKey[];
}

export interface UserFilters {
  search?: string;
  role?: UserRole;
  isActive?: boolean;
}
