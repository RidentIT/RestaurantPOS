import { useState } from "react";
import { AlertTriangle } from "lucide-react";
import type { Backup } from "@/entities/settings";
import { useBackupMutations } from "@/features/settings";
import { toApiError } from "@/shared/api/problem";
import {
  Alert,
  AlertDescription,
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  FormField,
  Input,
} from "@/shared/ui";

const CONFIRMATION_WORD = "RESTORE";

export interface RestoreBackupDialogProps {
  backup: Backup | null;
  onOpenChange: (open: boolean) => void;
  /** Fires once the restore has actually succeeded — the caller shows the relaunch screen. */
  onRestored: () => void;
}

/**
 * Confirms a restore with an administrator's PIN and the typed word "RESTORE" — two separate
 * confirmations for an action that erases every record made since the backup, on top of what a
 * routine PIN-gated action like voiding an order already asks for.
 */
export function RestoreBackupDialog({ backup, onOpenChange, onRestored }: RestoreBackupDialogProps) {
  const { restoreBackup } = useBackupMutations();
  const [pin, setPin] = useState("");
  const [confirmationText, setConfirmationText] = useState("");
  const [error, setError] = useState<string | null>(null);

  const open = !!backup;

  const close = (nextOpen: boolean) => {
    if (!nextOpen) {
      setPin("");
      setConfirmationText("");
      setError(null);
    }
    onOpenChange(nextOpen);
  };

  const submit = async () => {
    if (!backup) return;

    if (confirmationText !== CONFIRMATION_WORD) {
      setError(`Type ${CONFIRMATION_WORD} exactly to confirm.`);
      return;
    }

    if (pin.length !== 4) {
      setError("Enter the 4-digit approval PIN.");
      return;
    }

    try {
      setError(null);
      await restoreBackup.mutateAsync({ fileName: backup.fileName, pin, confirmationText });
      onRestored();
    } catch (failure) {
      setError(toApiError(failure).message);
      setPin("");
    }
  };

  return (
    <Dialog open={open} onOpenChange={close}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Restore from backup</DialogTitle>
          <DialogDescription>
            {backup && `Replaces every record with what "${backup.fileName}" holds.`}
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          <Alert variant="destructive">
            <AlertTriangle className="size-4" />
            <AlertDescription>
              Everything recorded since this backup was taken — every order, payment and change —
              is permanently lost. This cannot be undone. The application closes immediately
              afterwards; reopen it once you have restored.
            </AlertDescription>
          </Alert>

          <FormField htmlFor="restore-confirmation" label={`Type ${CONFIRMATION_WORD} to confirm`} required>
            <Input
              id="restore-confirmation"
              value={confirmationText}
              onChange={(e) => {
                setConfirmationText(e.target.value);
                setError(null);
              }}
              placeholder={CONFIRMATION_WORD}
              autoComplete="off"
            />
          </FormField>

          <FormField htmlFor="restore-pin" label="Administrator approval PIN" required>
            <Input
              id="restore-pin"
              type="password"
              inputMode="numeric"
              autoComplete="off"
              maxLength={4}
              value={pin}
              onChange={(e) => {
                setPin(e.target.value.replace(/\D/g, "").slice(0, 4));
                setError(null);
              }}
              className="text-center text-2xl tracking-[0.6em]"
              placeholder="••••"
            />
          </FormField>

          {error && (
            <Alert variant="destructive">
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          )}
        </div>

        <DialogFooter>
          <Button type="button" variant="outline" onClick={() => close(false)} disabled={restoreBackup.isPending}>
            Cancel
          </Button>
          <Button
            type="button"
            variant="destructive"
            onClick={submit}
            loading={restoreBackup.isPending}
            disabled={confirmationText !== CONFIRMATION_WORD || pin.length !== 4}
          >
            Restore and close the app
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
