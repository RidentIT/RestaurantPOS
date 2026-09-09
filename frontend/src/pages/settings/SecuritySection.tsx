import { useEffect, useState } from "react";
import { toast } from "sonner";
import type { RestaurantSettings } from "@/entities/settings";
import { useSettingsMutations } from "@/features/settings";
import { toApiError } from "@/shared/api/problem";
import { Button, Card, CardContent, CardDescription, CardHeader, CardTitle, FormField, Input } from "@/shared/ui";

/** How forgiving a mistyped approval PIN is at the till. */
export function SecuritySection({ settings }: { settings: RestaurantSettings }) {
  const { updateApprovalPinPolicy } = useSettingsMutations();

  const [maxAttempts, setMaxAttempts] = useState(String(settings.approvalPinMaxAttempts));
  const [lockoutMinutes, setLockoutMinutes] = useState(String(settings.approvalPinLockoutMinutes));

  useEffect(() => {
    setMaxAttempts(String(settings.approvalPinMaxAttempts));
    setLockoutMinutes(String(settings.approvalPinLockoutMinutes));
  }, [settings]);

  const save = async () => {
    const attempts = Number(maxAttempts);
    const minutes = Number(lockoutMinutes);

    if (!Number.isInteger(attempts) || attempts < 1 || attempts > 10) {
      toast.error("Attempts allowed must be a whole number between 1 and 10.");
      return;
    }

    if (!Number.isInteger(minutes) || minutes < 1 || minutes > 60) {
      toast.error("The lockout must be a whole number of minutes between 1 and 60.");
      return;
    }

    try {
      await updateApprovalPinPolicy.mutateAsync({ maxAttempts: attempts, lockoutMinutes: minutes });
      toast.success("Approval PIN policy updated.");
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  };

  return (
    <div className="max-w-xl">
      <Card>
        <CardHeader>
          <CardTitle>Approval PIN lockout</CardTitle>
          <CardDescription>
            Controls how many wrong attempts a terminal allows before pausing PIN entry there, and
            for how long. This never locks an administrator's account itself — only that terminal.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid gap-4 sm:grid-cols-2">
            <FormField htmlFor="settings-pin-attempts" label="Attempts allowed (1-10)" required>
              <Input
                id="settings-pin-attempts"
                inputMode="numeric"
                value={maxAttempts}
                onChange={(e) => setMaxAttempts(e.target.value)}
              />
            </FormField>
            <FormField htmlFor="settings-pin-lockout" label="Lockout, in minutes (1-60)" required>
              <Input
                id="settings-pin-lockout"
                inputMode="numeric"
                value={lockoutMinutes}
                onChange={(e) => setLockoutMinutes(e.target.value)}
              />
            </FormField>
          </div>
          <Button onClick={save} loading={updateApprovalPinPolicy.isPending}>
            Save
          </Button>
        </CardContent>
      </Card>
    </div>
  );
}
