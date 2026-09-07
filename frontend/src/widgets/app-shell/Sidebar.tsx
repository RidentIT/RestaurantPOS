import { UserCircle } from "lucide-react";
import { NavLink } from "react-router-dom";
import { groupModules, type ModuleDescriptor } from "@/entities/user";
import { useAuth } from "@/features/auth";
import { MODULE_ROUTES, DEFAULT_MODULE_ICON } from "@/shared/config/moduleRoutes";
import { cn } from "@/shared/lib/utils";

export interface SidebarProps {
  catalog: ModuleDescriptor[];
}

/**
 * Primary navigation, built from the module catalog rather than a hard-coded list — a module a
 * user cannot open is left out entirely rather than shown and blocked, so the menu only ever
 * promises what it can deliver.
 */
export function Sidebar({ catalog }: SidebarProps) {
  const { can } = useAuth();
  const groups = groupModules(catalog).map((group) => ({
    ...group,
    modules: group.modules.filter((m) => can(m.module)),
  }));

  return (
    <aside className="flex h-full w-64 shrink-0 flex-col border-r bg-card">
      <div className="flex h-16 items-center gap-2 border-b px-5">
        <div className="flex size-8 items-center justify-center rounded-md bg-primary text-sm font-bold text-primary-foreground">
          SL
        </div>
        <div className="leading-tight">
          <p className="text-sm font-semibold">Sri Lakshmi</p>
          <p className="text-xs text-muted-foreground">Family Restaurant</p>
        </div>
      </div>

      <nav className="flex-1 space-y-5 overflow-y-auto px-3 py-4">
        {groups
          .filter((group) => group.modules.length > 0)
          .map(({ group, modules }) => (
            <div key={group}>
              <p className="mb-1.5 px-3 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                {group}
              </p>
              <div className="space-y-0.5">
                {modules.map((descriptor) => (
                  <ModuleNavItem key={descriptor.module} descriptor={descriptor} />
                ))}
              </div>
            </div>
          ))}
      </nav>

      <div className="border-t px-3 py-3">
        <NavLink
          to="/account"
          className={({ isActive }) =>
            cn(
              "flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors",
              isActive
                ? "bg-primary/10 text-primary"
                : "text-foreground/80 hover:bg-accent hover:text-accent-foreground",
            )
          }
        >
          <UserCircle className="size-4 shrink-0" />
          <span className="truncate">My account</span>
        </NavLink>
      </div>
    </aside>
  );
}

function ModuleNavItem({ descriptor }: { descriptor: ModuleDescriptor }) {
  const route = MODULE_ROUTES[descriptor.module];
  const Icon = route?.icon ?? DEFAULT_MODULE_ICON;

  if (!route?.path) {
    return (
      <div
        className="flex items-center gap-3 rounded-md px-3 py-2 text-sm text-muted-foreground/60"
        title="Coming soon"
      >
        <Icon className="size-4 shrink-0" />
        <span className="truncate">{descriptor.name}</span>
        <span className="ml-auto text-[10px] font-medium uppercase tracking-wide">Soon</span>
      </div>
    );
  }

  return (
    <NavLink
      to={route.path}
      className={({ isActive }) =>
        cn(
          "flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors",
          isActive
            ? "bg-primary/10 text-primary"
            : "text-foreground/80 hover:bg-accent hover:text-accent-foreground",
        )
      }
    >
      <Icon className="size-4 shrink-0" />
      <span className="truncate">{descriptor.name}</span>
    </NavLink>
  );
}
