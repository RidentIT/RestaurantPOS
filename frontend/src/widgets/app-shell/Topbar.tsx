import { Bell, Home, LogOut, PanelLeft, ShieldCheck, User as UserIcon } from "lucide-react";
import { NavLink, useNavigate } from "react-router-dom";
import { useAuth, useLogout } from "@/features/auth";
import { NotificationBell } from "@/features/notifications";
import { cn } from "@/shared/lib/utils";
import {
  Badge,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/shared/ui";

export interface TopbarProps {
  sidebarCollapsed: boolean;
  onToggleSidebar: () => void;
}

/** Top bar: sidebar toggle, notifications, current user, role, and account actions. */
export function Topbar({ sidebarCollapsed, onToggleSidebar }: TopbarProps) {
  const { user } = useAuth();
  const logout = useLogout();
  const navigate = useNavigate();

  if (!user) return null;

  return (
    <header className="flex h-16 shrink-0 items-center justify-between gap-2 border-b bg-card px-6">
      <div className="flex items-center gap-1">
        <button
          type="button"
          onClick={onToggleSidebar}
          title={sidebarCollapsed ? "Show the menu" : "Hide the menu"}
          aria-label={sidebarCollapsed ? "Show the menu" : "Hide the menu"}
          aria-pressed={!sidebarCollapsed}
          className="flex size-9 items-center justify-center rounded-md text-foreground/70 transition-colors hover:bg-accent hover:text-accent-foreground"
        >
          <PanelLeft className="size-5" />
        </button>

        <NavLink
          to="/"
          end
          title="Home"
          aria-label="Home"
          className={({ isActive }) =>
            cn(
              "flex size-9 items-center justify-center rounded-md transition-colors",
              isActive
                ? "bg-primary/10 text-primary"
                : "text-foreground/70 hover:bg-accent hover:text-accent-foreground",
            )
          }
        >
          <Home className="size-5" />
        </NavLink>
      </div>

      <div className="flex items-center gap-1">
        {/* Polling is paused while a forced password change is outstanding, since the API refuses
            everything else until it is done. */}
        <NotificationBell enabled={!user.mustChangePassword} />

        <DropdownMenu>
          <DropdownMenuTrigger className="flex items-center gap-3 rounded-md px-2 py-1.5 text-sm hover:bg-accent">
            <span className="flex size-8 items-center justify-center rounded-full bg-primary/10 text-primary">
              <UserIcon className="size-4" />
            </span>
            <span className="text-left leading-tight">
              <span className="block font-medium">{user.fullName}</span>
              <span className="block text-xs text-muted-foreground">@{user.username}</span>
            </span>
            {user.role === "Admin" && (
              <Badge variant="default" className="ml-1 gap-1">
                <ShieldCheck className="size-3" />
                Admin
              </Badge>
            )}
          </DropdownMenuTrigger>

          <DropdownMenuContent align="end">
            <DropdownMenuLabel>Signed in as {user.username}</DropdownMenuLabel>
            <DropdownMenuSeparator />
            <DropdownMenuItem onSelect={() => navigate("/account")}>
              <UserIcon /> My account
            </DropdownMenuItem>
            <DropdownMenuItem onSelect={() => navigate("/notifications/settings")}>
              <Bell /> Notification settings
            </DropdownMenuItem>
            <DropdownMenuSeparator />
            <DropdownMenuItem destructive onSelect={() => logout.mutate()}>
              <LogOut /> Sign out
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
    </header>
  );
}
