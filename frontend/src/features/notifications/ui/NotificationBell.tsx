import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { Bell, CheckCheck, Settings2, TriangleAlert } from "lucide-react";
import type { AppNotification, NotificationSeverity } from "@/entities/notification";
import { describeAge } from "@/entities/notification";
import { Button } from "@/shared/ui";
import { cn } from "@/shared/lib/utils";
import { useNotificationFeed } from "../model/useNotifications";

/** Left border colour per severity, which is what makes the feed scannable at a glance. */
const SEVERITY_ACCENT: Record<NotificationSeverity, string> = {
  Info: "border-l-muted-foreground/40",
  Warning: "border-l-warning",
  Urgent: "border-l-destructive",
};

/**
 * The bell in the top bar: an unread count, and a feed of what needs attention.
 *
 * Rendered for everybody rather than gated on a module — a cashier still needs telling that their
 * food is ready. What actually appears is decided server-side by the modules each person holds.
 */
export function NotificationBell({ enabled = true }: { enabled?: boolean }) {
  const [open, setOpen] = useState(false);
  const navigate = useNavigate();
  const { feed, unreadCount, isLoading, markRead } = useNotificationFeed(enabled);

  const notifications = feed?.notifications ?? [];

  const openNotification = (notification: AppNotification) => {
    if (!notification.isRead) {
      markRead.mutate([notification.id]);
    }

    setOpen(false);

    if (notification.link) {
      navigate(notification.link);
    }
  };

  return (
    <div className="relative">
      <Button
        variant="ghost"
        size="icon"
        aria-label={unreadCount > 0 ? `Notifications, ${unreadCount} unread` : "Notifications"}
        onClick={() => setOpen((current) => !current)}
      >
        <Bell className="size-5" />
        {unreadCount > 0 && (
          <span
            aria-hidden="true"
            className={cn(
              "absolute right-1 top-1 flex min-w-4 items-center justify-center rounded-full px-1",
              "bg-destructive text-[10px] font-semibold leading-4 text-destructive-foreground",
            )}
          >
            {unreadCount > 99 ? "99+" : unreadCount}
          </span>
        )}
      </Button>

      {open && (
        <>
          {/* Click-away layer, so the panel closes without trapping focus anywhere odd. */}
          <div className="fixed inset-0 z-40" aria-hidden="true" onClick={() => setOpen(false)} />

          <div
            role="dialog"
            aria-label="Notifications"
            className="absolute right-0 z-50 mt-2 flex max-h-[32rem] w-96 flex-col rounded-lg border bg-card shadow-lg"
          >
            <div className="flex items-center justify-between gap-2 border-b p-3">
              <p className="font-semibold">
                Notifications
                {unreadCount > 0 && <span className="text-muted-foreground"> · {unreadCount} unread</span>}
              </p>
              <div className="flex items-center gap-1">
                {unreadCount > 0 && (
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => markRead.mutate(undefined)}
                    loading={markRead.isPending}
                  >
                    <CheckCheck /> Mark all read
                  </Button>
                )}
                <Button variant="ghost" size="icon" aria-label="Notification settings" asChild>
                  <Link to="/notifications/settings" onClick={() => setOpen(false)}>
                    <Settings2 className="size-4" />
                  </Link>
                </Button>
              </div>
            </div>

            <div className="min-h-0 flex-1 overflow-y-auto">
              {isLoading ? (
                <p className="p-6 text-center text-sm text-muted-foreground">Loading…</p>
              ) : notifications.length === 0 ? (
                <div className="p-8 text-center">
                  <Bell className="mx-auto mb-2 size-8 text-muted-foreground/40" />
                  <p className="text-sm font-medium">Nothing needs your attention</p>
                  <p className="text-sm text-muted-foreground">You are all caught up.</p>
                </div>
              ) : (
                <ul className="divide-y">
                  {notifications.map((notification) => (
                    <li key={notification.id}>
                      <button
                        type="button"
                        onClick={() => openNotification(notification)}
                        className={cn(
                          "w-full border-l-4 p-3 text-left transition-colors hover:bg-muted/60",
                          SEVERITY_ACCENT[notification.severity],
                          !notification.isRead && "bg-primary/5",
                        )}
                      >
                        <div className="flex items-start gap-2">
                          {notification.severity === "Urgent" && (
                            <TriangleAlert className="mt-0.5 size-4 shrink-0 text-destructive" />
                          )}
                          <div className="min-w-0 flex-1">
                            <p className={cn("text-sm", !notification.isRead && "font-semibold")}>
                              {notification.title}
                            </p>
                            {notification.body && (
                              <p className="mt-0.5 text-sm text-muted-foreground">{notification.body}</p>
                            )}
                            <p className="mt-1 text-xs text-muted-foreground">
                              {notification.typeName} · {describeAge(notification.raisedAtUtc)}
                            </p>
                          </div>
                          {!notification.isRead && (
                            <span
                              aria-hidden="true"
                              className="mt-1.5 size-2 shrink-0 rounded-full bg-primary"
                            />
                          )}
                        </div>
                      </button>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          </div>
        </>
      )}
    </div>
  );
}
