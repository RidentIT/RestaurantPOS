import { useMemo, useState } from "react";
import { Bell, Info, Monitor, TriangleAlert } from "lucide-react";
import { toast } from "sonner";
import type { NotificationPreference, NotificationSeverity } from "@/entities/notification";
import { THRESHOLD_UNIT_LABELS } from "@/entities/notification";
import { useAuth } from "@/features/auth";
import {
  useNotificationPreferences,
  useNotificationSettingsMutations,
  useRequestDesktopPermission,
} from "@/features/notifications";
import { toApiError } from "@/shared/api/problem";
import { MODULE_ROUTES } from "@/shared/config/moduleRoutes";
import { Alert, AlertDescription, Badge, Card, Input, LoadingState, Switch } from "@/shared/ui";
import { cn } from "@/shared/lib/utils";

const SEVERITY_BADGE: Record<NotificationSeverity, "outline" | "warning" | "destructive"> = {
  Info: "outline",
  Warning: "warning",
  Urgent: "destructive",
};

/** Turns "StoreStockManagement" into "Store Stock Management" for the group headings. */
const humanise = (module: string) => module.replace(/([a-z])([A-Z])/g, "$1 $2");

function ThresholdField({
  preference,
  onSave,
  disabled,
}: {
  preference: NotificationPreference;
  onSave: (value: number) => void;
  disabled: boolean;
}) {
  const [value, setValue] = useState(String(preference.threshold ?? ""));

  const commit = () => {
    const parsed = Number(value);

    if (!Number.isFinite(parsed) || parsed < 0) {
      toast.error("Enter a number of zero or more.");
      setValue(String(preference.threshold ?? ""));
      return;
    }

    if (parsed !== preference.threshold) {
      onSave(parsed);
    }
  };

  return (
    <div className="flex items-center gap-2">
      <Input
        value={value}
        onChange={(event) => setValue(event.target.value)}
        onBlur={commit}
        onKeyDown={(event) => event.key === "Enter" && event.currentTarget.blur()}
        inputMode="decimal"
        disabled={disabled}
        aria-label={`${preference.name} threshold`}
        className="h-8 w-20 tabular"
      />
      <span className="text-xs text-muted-foreground">
        {THRESHOLD_UNIT_LABELS[preference.thresholdUnit]}
      </span>
    </div>
  );
}

/**
 * Where each person decides what their own bell tells them, and an administrator tunes the
 * numbers the rules run on.
 *
 * Only notifications this user could actually receive are listed — an option you can never be
 * sent is just clutter.
 */
export default function NotificationSettingsPage() {
  const { user } = useAuth();
  const isAdmin = user?.role === "Admin";

  const { data: preferences, isLoading } = useNotificationPreferences();
  const { setPreference, setThreshold } = useNotificationSettingsMutations();
  const requestDesktopPermission = useRequestDesktopPermission();

  const grouped = useMemo(() => {
    const groups = new Map<string, NotificationPreference[]>();

    for (const preference of preferences ?? []) {
      // Filed under its first module, which is the one it most belongs to.
      const key = preference.modules[0] ?? "General";
      groups.set(key, [...(groups.get(key) ?? []), preference]);
    }

    return [...groups.entries()].sort((a, b) => a[0].localeCompare(b[0]));
  }, [preferences]);

  const save = async (
    preference: NotificationPreference,
    changes: { isEnabled?: boolean; desktopEnabled?: boolean },
  ) => {
    const desktopEnabled = changes.desktopEnabled ?? preference.desktopEnabled;

    // Windows will not show anything until the user has agreed to it once.
    if (desktopEnabled && !preference.desktopEnabled) {
      const granted = await requestDesktopPermission();

      if (!granted) {
        toast.error("Windows notifications are blocked for this app. Enable them in system settings.");
        return;
      }
    }

    try {
      await setPreference.mutateAsync({
        type: preference.type,
        isEnabled: changes.isEnabled ?? preference.isEnabled,
        desktopEnabled,
      });
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  };

  const saveThreshold = async (preference: NotificationPreference, threshold: number) => {
    try {
      await setThreshold.mutateAsync({ type: preference.type, threshold });
      toast.success(`${preference.name} updated.`);
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  };

  if (isLoading) {
    return <LoadingState label="Loading your notification settings…" className="h-96" />;
  }

  const enabledCount = (preferences ?? []).filter((p) => p.isEnabled).length;

  return (
    <div className="space-y-6 p-8">
      <div>
        <h1 className="text-2xl font-semibold">Notification settings</h1>
        <p className="text-sm text-muted-foreground">
          {enabledCount} of {preferences?.length ?? 0} turned on. These choices are yours alone
          {isAdmin && " — thresholds apply to the whole restaurant"}.
        </p>
      </div>

      <Alert variant="info">
        <Info className="size-4" />
        <AlertDescription>
          Urgent notifications also raise an on-screen toast. Turn on <strong>Windows pop-up</strong> for
          anything that needs to reach a screen nobody is watching, such as the kitchen display.
        </AlertDescription>
      </Alert>

      {grouped.length === 0 ? (
        <Card className="p-8 text-center">
          <Bell className="mx-auto mb-2 size-8 text-muted-foreground/40" />
          <p className="font-medium">No notifications available</p>
          <p className="text-sm text-muted-foreground">
            You do not currently hold any modules that raise alerts.
          </p>
        </Card>
      ) : (
        grouped.map(([module, items]) => (
          <Card key={module} className="overflow-hidden">
            <div className="flex items-center gap-2 border-b bg-muted/40 px-4 py-3">
              {(() => {
                const Icon = MODULE_ROUTES[module]?.icon;
                return Icon ? <Icon className="size-4 text-muted-foreground" /> : null;
              })()}
              <h2 className="font-semibold">{humanise(module)}</h2>
            </div>

            <ul className="divide-y">
              {items.map((preference) => (
                <li
                  key={preference.type}
                  className="flex flex-wrap items-center gap-4 p-4 sm:flex-nowrap"
                >
                  <div className="min-w-0 flex-1">
                    <div className="flex flex-wrap items-center gap-2">
                      <p className={cn("font-medium", !preference.isEnabled && "text-muted-foreground")}>
                        {preference.name}
                      </p>
                      <Badge variant={SEVERITY_BADGE[preference.severity]}>{preference.severity}</Badge>
                      {preference.severity === "Urgent" && (
                        <TriangleAlert className="size-3.5 text-destructive" aria-hidden="true" />
                      )}
                    </div>
                    <p className="mt-0.5 text-sm text-muted-foreground">{preference.description}</p>
                  </div>

                  {preference.hasThreshold && (
                    <div className="shrink-0">
                      {isAdmin ? (
                        <ThresholdField
                          preference={preference}
                          onSave={(value) => saveThreshold(preference, value)}
                          disabled={setThreshold.isPending}
                        />
                      ) : (
                        <span className="text-sm text-muted-foreground tabular">
                          {preference.threshold} {THRESHOLD_UNIT_LABELS[preference.thresholdUnit]}
                        </span>
                      )}
                    </div>
                  )}

                  <label className="flex shrink-0 cursor-pointer items-center gap-2 text-sm">
                    <Monitor className="size-4 text-muted-foreground" aria-hidden="true" />
                    <span className="sr-only sm:not-sr-only">Windows pop-up</span>
                    <Switch
                      checked={preference.desktopEnabled}
                      disabled={!preference.isEnabled || setPreference.isPending}
                      onCheckedChange={(checked) => save(preference, { desktopEnabled: checked })}
                      aria-label={`Windows pop-up for ${preference.name}`}
                    />
                  </label>

                  <Switch
                    checked={preference.isEnabled}
                    disabled={setPreference.isPending}
                    onCheckedChange={(checked) => save(preference, { isEnabled: checked })}
                    aria-label={preference.name}
                  />
                </li>
              ))}
            </ul>
          </Card>
        ))
      )}
    </div>
  );
}
