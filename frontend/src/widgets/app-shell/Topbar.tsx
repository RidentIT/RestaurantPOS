import { Bell, LogOut, ShieldCheck, User as UserIcon } from "lucide-react";
import { useNavigate } from "react-router-dom";
import { useAuth, useLogout } from "@/features/auth";
import { NotificationBell } from "@/features/notifications";
import {
  Badge,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/shared/ui";

/** Top bar: notifications, current user, role, and account actions. */
export function Topbar() {
  const { user } = useAuth();
  const logout = useLogout();
  const navigate = useNavigate();

  if (!user) return null;

  return (
    <header className="flex h-16 shrink-0 items-center justify-between gap-2 border-b bg-card px-6">
      <div />

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
