/** How loudly a notification arrives. Only `Urgent` interrupts with a toast. */
export type NotificationSeverity = "Info" | "Warning" | "Urgent";

/** What a threshold measures, so the settings screen can label its input. */
export type ThresholdUnit = "None" | "Minutes" | "Days" | "Percent" | "Amount";

export const THRESHOLD_UNIT_LABELS: Record<ThresholdUnit, string> = {
  None: "",
  Minutes: "minutes",
  Days: "days",
  Percent: "%",
  Amount: "amount",
};

export interface AppNotification {
  id: string;
  /** The catalog key, e.g. `MainStoreLowStock`. */
  type: string;
  /** Human-readable name of the kind, e.g. "Main Store running low". */
  typeName: string;
  severity: NotificationSeverity;
  title: string;
  body: string | null;
  /** Where clicking it should go. Null when there is nowhere useful to send someone. */
  link: string | null;
  raisedAtUtc: string;
  isRead: boolean;
  /** Whether this user also wants a Windows notification for this kind. */
  desktopEnabled: boolean;
}

export interface NotificationFeed {
  unreadCount: number;
  notifications: AppNotification[];
}

export interface NotificationPreference {
  type: string;
  name: string;
  description: string;
  severity: NotificationSeverity;
  /** Who is eligible for it — used to group the settings screen by module. */
  modules: string[];
  isEnabled: boolean;
  desktopEnabled: boolean;
  /** True while the user has never expressed an opinion and the catalog default applies. */
  isDefault: boolean;
  hasThreshold: boolean;
  thresholdUnit: ThresholdUnit;
  threshold: number | null;
}

/** Relative wording for the feed — "just now" reads better than a timestamp on a bell. */
export function describeAge(raisedAtUtc: string, now: Date = new Date()): string {
  const minutes = Math.max(0, Math.floor((now.getTime() - new Date(raisedAtUtc).getTime()) / 60000));

  if (minutes < 1) return "just now";
  if (minutes < 60) return `${minutes}m ago`;

  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h ago`;

  const days = Math.floor(hours / 24);

  return days === 1 ? "yesterday" : `${days}d ago`;
}
