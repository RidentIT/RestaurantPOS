import type { ModuleDescriptor, ModuleKey, User } from "./types";

/**
 * True when the user may open the given module.
 *
 * The backend enforces this on every request; this is only used to decide what to render, so
 * the UI never offers a door the API will slam shut.
 */
export function canAccessModule(user: User | null, module: ModuleKey): boolean {
  if (!user) return false;

  return user.role === "Admin" || user.modules.includes(module);
}

/** The modules an administrator may grant to a non-admin user. */
export function assignableModules(catalog: ModuleDescriptor[]): ModuleDescriptor[] {
  return catalog.filter((m) => !m.adminOnly);
}

/**
 * Groups modules by their catalog group, preserving the backend's ordering both between and
 * within groups so navigation and the permission editor always agree.
 */
export function groupModules(catalog: ModuleDescriptor[]): Array<{
  group: string;
  modules: ModuleDescriptor[];
}> {
  const ordered = [...catalog].sort((a, b) => a.sortOrder - b.sortOrder);
  const groups = new Map<string, ModuleDescriptor[]>();

  for (const descriptor of ordered) {
    const existing = groups.get(descriptor.group);
    if (existing) {
      existing.push(descriptor);
    } else {
      groups.set(descriptor.group, [descriptor]);
    }
  }

  return [...groups.entries()].map(([group, modules]) => ({ group, modules }));
}
