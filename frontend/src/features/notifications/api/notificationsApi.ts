import type { NotificationFeed, NotificationPreference } from "@/entities/notification";
import { API_ENDPOINTS, apiService } from "@/shared/api/endpoints";

export const notificationsApi = {
  feed: (unreadOnly = false, limit = 50) =>
    apiService.get<NotificationFeed>(API_ENDPOINTS.NOTIFICATIONS.BASE, { unreadOnly, limit }),

  /** Works out what is worth raising right now. Safe to call repeatedly. */
  evaluate: () => apiService.post<number>(API_ENDPOINTS.NOTIFICATIONS.EVALUATE),

  /** Omitting the ids marks everything of the user's as read. */
  markRead: (notificationIds?: string[]) =>
    apiService.post<number>(API_ENDPOINTS.NOTIFICATIONS.READ, { notificationIds: notificationIds ?? null }),

  preferences: () =>
    apiService.get<NotificationPreference[]>(API_ENDPOINTS.NOTIFICATIONS.PREFERENCES),

  updatePreference: (type: string, isEnabled: boolean, desktopEnabled: boolean) =>
    apiService.put<void>(API_ENDPOINTS.NOTIFICATIONS.PREFERENCES, { type, isEnabled, desktopEnabled }),

  updateThreshold: (type: string, threshold: number) =>
    apiService.put<void>(API_ENDPOINTS.NOTIFICATIONS.THRESHOLDS, { type, threshold }),
};
