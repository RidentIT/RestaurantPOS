import { useEffect, useState } from "react";
import { Info, MonitorSmartphone } from "lucide-react";
import type { RestaurantSettings } from "@/entities/settings";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/shared/ui";

/** What this install is, and what it is running on — useful when reporting a problem. */
export function AboutSection({ settings }: { settings: RestaurantSettings }) {
  const [versions, setVersions] = useState<{ electron: string; chrome: string } | null>(null);

  useEffect(() => {
    let cancelled = false;

    window.electronAPI
      ?.version()
      .then((v) => !cancelled && setVersions(v))
      .catch(() => undefined);

    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <div className="max-w-xl space-y-6">
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Info className="size-5" /> This restaurant
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-1 text-sm">
          <p className="font-medium">{settings.name}</p>
          <p className="text-muted-foreground">
            {[settings.addressLine1, settings.addressLine2, settings.city].filter(Boolean).join(", ")}
          </p>
          {settings.phone && <p className="text-muted-foreground">{settings.phone}</p>}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <MonitorSmartphone className="size-5" /> This installation
          </CardTitle>
          <CardDescription>Runs entirely on this machine — nothing here talks to the cloud.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-1 text-sm text-muted-foreground">
          <p>Restaurant POS — local edition</p>
          {versions && (
            <>
              <p>Electron {versions.electron}</p>
              <p>Chromium {versions.chrome}</p>
            </>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
