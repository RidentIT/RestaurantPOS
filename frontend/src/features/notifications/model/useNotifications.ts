import { useCallback, useEffect, useRef } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import type { AppNotification, NotificationPreference } from "@/entities/notification";
import { notificationsApi } from "../api/notificationsApi";

export const NOTIFICATIONS_KEY = "notifications";
export const NOTIFICATION_PREFERENCES_KEY = "notification-preferences";

/** How often the bell checks. Each check is a single local call of a millisecond or two. */
const POLL_MS = 60_000;

/**
 * The bell's data, and the noise it makes.
 *
 * Polls rather than holding a socket open: the API runs on this same machine, so a check is a
 * loopback call costing microseconds, and there is no connection to drop or reconnect. Each poll
 * asks the server to re-evaluate first, which is what stands in for a scheduler on a machine that
 * gets switched off every night.
 */
export function useNotificationFeed(enabled: boolean) {
  const queryClient = useQueryClient();

  // Notifications already surfaced as a toast or a desktop pop-up. Seeded from the first load so
  // signing in after a busy morning does not fire twenty toasts at once.
  const surfaced = useRef<Set<string> | null>(null);

  const query = useQuery({
    queryKey: [NOTIFICATIONS_KEY],
    queryFn: async () => {
      // Evaluating is best-effort: a failure there must not stop the feed being read.
      await notificationsApi.evaluate().catch(() => undefined);

      return notificationsApi.feed();
    },
    enabled,
    refetchInterval: enabled ? POLL_MS : false,
    refetchOnWindowFocus: true,
  });

  const notifications = query.data?.notifications;

  useEffect(() => {
    if (!notifications) {
      return;
    }

    if (surfaced.current === null) {
      surfaced.current = new Set(notifications.map((n) => n.id));
      return;
    }

    const seen = surfaced.current;

    for (const notification of notifications) {
      if (notification.isRead || seen.has(notification.id)) {
        continue;
      }

      seen.add(notification.id);
      announce(notification);
    }
  }, [notifications]);

  const markRead = useMutation({
    mutationFn: (ids?: string[]) => notificationsApi.markRead(ids),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [NOTIFICATIONS_KEY] }),
  });

  return {
    feed: query.data,
    unreadCount: query.data?.unreadCount ?? 0,
    isLoading: query.isLoading,
    markRead,
  };
}

/** Raises a toast for anything urgent, and a Windows notification where the user asked for one. */
function announce(notification: AppNotification): void {
  if (notification.severity === "Urgent") {
    toast.warning(notification.title, { description: notification.body ?? undefined });
  }

  if (notification.desktopEnabled) {
    showDesktopNotification(notification);
  }
}

/**
 * Shows a native Windows notification.
 *
 * Uses the standard web Notification API, which the desktop shell maps onto a real OS toast — so
 * it reaches a kitchen screen sitting behind another window. Silently does nothing where
 * notifications are unavailable or refused; a missing pop-up must never break the feed.
 */
function showDesktopNotification(notification: AppNotification): void {
  if (typeof window === "undefined" || !("Notification" in window)) {
    return;
  }

  try {
    if (window.Notification.permission === "granted") {
      // eslint-disable-next-line no-new
      new window.Notification(notification.title, { body: notification.body ?? undefined });
    } else if (window.Notification.permission !== "denied") {
      void window.Notification.requestPermission();
    }
  } catch {
    // Some environments expose the constructor but refuse to construct it.
  }
}

export function useNotificationPreferences(enabled = true) {
  return useQuery({
    queryKey: [NOTIFICATION_PREFERENCES_KEY],
    queryFn: notificationsApi.preferences,
    enabled,
  });
}

export function useNotificationSettingsMutations() {
  const queryClient = useQueryClient();

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: [NOTIFICATION_PREFERENCES_KEY] });
    queryClient.invalidateQueries({ queryKey: [NOTIFICATIONS_KEY] });
  };

  const setPreference = useMutation<
    void,
    Error,
    { type: string; isEnabled: boolean; desktopEnabled: boolean }
  >({
    mutationFn: ({ type, isEnabled, desktopEnabled }) =>
      notificationsApi.updatePreference(type, isEnabled, desktopEnabled),
    onSuccess: invalidate,
  });

  const setThreshold = useMutation<void, Error, { type: string; threshold: number }>({
    mutationFn: ({ type, threshold }) => notificationsApi.updateThreshold(type, threshold),
    onSuccess: invalidate,
  });

  return { setPreference, setThreshold };
}

/**
 * Asks for permission to show Windows notifications, once, when a user first turns one on.
 * Returns whether it ended up granted.
 */
export function useRequestDesktopPermission() {
  return useCallback(async (): Promise<boolean> => {
    if (typeof window === "undefined" || !("Notification" in window)) {
      return false;
    }

    if (window.Notification.permission === "granted") {
      return true;
    }

    if (window.Notification.permission === "denied") {
      return false;
    }

    try {
      return (await window.Notification.requestPermission()) === "granted";
    } catch {
      return false;
    }
  }, []);
}

export type { NotificationPreference };
