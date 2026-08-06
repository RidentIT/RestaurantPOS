import { useEffect, useRef, useState } from "react";
import { ShieldAlert } from "lucide-react";
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
  Input,
  Label,
} from "@/shared/ui";

export interface ManagerPinDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /** What the manager is being asked to approve, shown so they know what they are authorising. */
  action: string;
  /** Runs the guarded command. Reject with a message to keep the dialog open and show it. */
  onConfirm: (pin: string) => Promise<void>;
  pending?: boolean;
}

const PIN_LENGTH = 4;

/**
 * Collects a manager's 4-digit approval PIN for a guarded action (POS-016, POS-017).
 *
 * The PIN is only ever passed up to the caller, which sends it with the command for the server to
 * check. Nothing here decides whether it was right — a client-side check would be theatre, since
 * anything that can reach the API could simply skip this dialog.
 */
export function ManagerPinDialog({
  open,
  onOpenChange,
  action,
  onConfirm,
  pending = false,
}: ManagerPinDialogProps) {
  const [pin, setPin] = useState("");
  const [error, setError] = useState<string | null>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (open) {
      setPin("");
      setError(null);
      // Focus after the dialog's own opening animation, or the caret lands nowhere.
      const timer = window.setTimeout(() => inputRef.current?.focus(), 60);

      return () => window.clearTimeout(timer);
    }

    return undefined;
  }, [open]);

  const submit = async () => {
    if (pin.length !== PIN_LENGTH) {
      setError(`The PIN is ${PIN_LENGTH} digits.`);
      return;
    }

    try {
      setError(null);
      await onConfirm(pin);
      onOpenChange(false);
    } catch (failure) {
      // The server counts the attempts and says how many are left, so its message is shown as-is
      // rather than replaced with a generic one.
      setError(failure instanceof Error ? failure.message : "That PIN was not accepted.");
      setPin("");
      inputRef.current?.focus();
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-sm">
        <DialogHeader>
          <DialogTitle>Manager approval</DialogTitle>
          <DialogDescription>{action}</DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          <div className="space-y-1.5">
            <Label htmlFor="manager-pin">Enter 4-digit PIN</Label>
            <Input
              id="manager-pin"
              ref={inputRef}
              type="password"
              inputMode="numeric"
              autoComplete="off"
              maxLength={PIN_LENGTH}
              value={pin}
              onChange={(event) => {
                setPin(event.target.value.replace(/\D/g, "").slice(0, PIN_LENGTH));
                setError(null);
              }}
              onKeyDown={(event) => {
                if (event.key === "Enter") {
                  event.preventDefault();
                  void submit();
                }
              }}
              className="text-center text-2xl tracking-[0.6em]"
              placeholder="••••"
            />
          </div>

          {error && (
            <Alert variant="destructive">
              <ShieldAlert className="size-4" />
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          )}
        </div>

        <DialogFooter>
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={pending}>
            Cancel
          </Button>
          <Button type="button" onClick={submit} loading={pending} disabled={pin.length !== PIN_LENGTH}>
            Approve
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
