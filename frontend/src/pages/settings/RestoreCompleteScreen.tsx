import { useEffect } from "react";
import { CheckCircle2, Power } from "lucide-react";
import { Button, Card } from "@/shared/ui";

/**
 * Shown the moment a restore succeeds, filling the whole window.
 *
 * The API process that was serving this session shut itself down as part of the restore, so
 * nothing on screen behind this can work anymore anyway — every query would just fail. The only
 * way forward is closing and reopening the application against the database that now exists.
 */
export function RestoreCompleteScreen() {
  const canQuit = typeof window !== "undefined" && !!window.electronAPI?.quit;

  // A deliberate few seconds to read the message before the window closes on its own; the button
  // is there for anyone who would rather not wait.
  useEffect(() => {
    if (!canQuit) return undefined;

    const timer = window.setTimeout(() => void window.electronAPI!.quit(), 4000);
    return () => window.clearTimeout(timer);
  }, [canQuit]);

  return (
    <div className="fixed inset-0 z-[100] flex items-center justify-center bg-background p-8">
      <Card className="max-w-md space-y-4 p-8 text-center">
        <CheckCircle2 className="mx-auto size-14 text-success" />
        <div>
          <h1 className="text-2xl font-semibold">Restore complete</h1>
          <p className="mt-2 text-sm text-muted-foreground">
            The database has been replaced. Please close and reopen the application to continue.
          </p>
        </div>
        {canQuit ? (
          <Button size="lg" className="w-full" onClick={() => window.electronAPI!.quit()}>
            <Power /> Close the application
          </Button>
        ) : (
          <p className="text-sm text-muted-foreground">You can close this window now.</p>
        )}
      </Card>
    </div>
  );
}
