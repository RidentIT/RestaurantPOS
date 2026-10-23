import { useEffect, useState } from "react";
import { toast } from "sonner";
import { PAYMENT_METHOD_LABELS } from "@/entities/order";
import { formatDuration, formatMoney } from "@/entities/shift";
import type { Shift } from "@/entities/shift";
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
import { toApiError } from "@/shared/api/problem";
import { useShiftMutations } from "../model/useShifts";

export interface CheckOutDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  shift: Shift;
}

/** Closes the shift: shows what it took, asks for the counted cash, and shows any difference. */
export function CheckOutDialog({ open, onOpenChange, shift }: CheckOutDialogProps) {
  const { checkOut } = useShiftMutations();
  const [counted, setCounted] = useState("");
  const [note, setNote] = useState("");
  const [result, setResult] = useState<Shift | null>(null);

  useEffect(() => {
    if (open) {
      setCounted("");
      setNote("");
      setResult(null);
    }
  }, [open]);

  const { summary } = shift;
  const amount = Number(counted);
  const invalid = counted.trim() === "" || Number.isNaN(amount) || amount < 0;
  const blocked = summary.unsettledOrders > 0;
  const difference = invalid ? null : amount - shift.expectedCash;

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    if (invalid || blocked) return;

    try {
      setResult(await checkOut.mutateAsync({ closingCash: amount, note: note.trim() || null }));
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  };

  if (result) {
    return (
      <Dialog open={open} onOpenChange={onOpenChange}>
        <DialogContent className="max-w-sm">
          <DialogHeader>
            <DialogTitle>Shift closed</DialogTitle>
            <DialogDescription>
              Worked {formatDuration(result.checkedInAtUtc, result.checkedOutAtUtc)} ·{" "}
              {result.summary.ordersCompleted} orders · net {formatMoney(result.summary.netSales)}
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-1 rounded-lg border p-3 text-sm">
            <Row label="Expected in drawer" value={formatMoney(result.expectedCash)} />
            <Row label="You counted" value={formatMoney(result.closingCash ?? 0)} />
            <Row
              label="Difference"
              value={formatMoney(result.variance ?? 0)}
              tone={(result.variance ?? 0) === 0 ? "ok" : "bad"}
            />
          </div>

          <DialogFooter>
            <Button onClick={() => onOpenChange(false)}>Done</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    );
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Check out</DialogTitle>
          <DialogDescription>
            End your shift. Count the drawer and enter what's in it — the shift closes with these
            totals and the next check-in starts fresh.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={submit} className="space-y-4">
          <div className="space-y-1 rounded-lg border p-3 text-sm">
            <Row label="On shift for" value={formatDuration(shift.checkedInAtUtc, null)} />
            <Row label="Orders" value={String(summary.ordersCompleted)} />
            <Row label="Net sales" value={formatMoney(summary.netSales)} />
            {summary.payments.map((p) => (
              <Row key={p.method} label={`  ${PAYMENT_METHOD_LABELS[p.method]}`} value={formatMoney(p.amount)} />
            ))}
            <Row label="Opening cash" value={formatMoney(shift.openingCash)} />
            <Row label="Expected in drawer" value={formatMoney(shift.expectedCash)} strong />
          </div>

          {blocked && (
            <Alert variant="warning">
              <AlertDescription>
                You still have {summary.unsettledOrders} unsettled order{summary.unsettledOrders === 1 ? "" : "s"}.
                Pay or cancel {summary.unsettledOrders === 1 ? "it" : "them"} before checking out.
              </AlertDescription>
            </Alert>
          )}

          <FormField htmlFor="counted-cash" label="Cash you counted in the drawer" required>
            <Input
              id="counted-cash"
              type="number"
              inputMode="decimal"
              min={0}
              step="0.01"
              value={counted}
              onChange={(event) => setCounted(event.target.value)}
              autoFocus
            />
          </FormField>

          {difference !== null && (
            <p className={difference === 0 ? "text-sm text-success" : "text-sm text-destructive"}>
              {difference === 0
                ? "Drawer matches."
                : difference < 0
                  ? `Short by ${formatMoney(Math.abs(difference))}.`
                  : `Over by ${formatMoney(difference)}.`}
            </p>
          )}

          <FormField htmlFor="shift-note" label="Note (optional)">
            <Input id="shift-note" value={note} onChange={(event) => setNote(event.target.value)} maxLength={300} />
          </FormField>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={checkOut.isPending}>
              Cancel
            </Button>
            <Button type="submit" loading={checkOut.isPending} disabled={invalid || blocked}>
              Check out
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function Row({
  label,
  value,
  strong,
  tone,
}: {
  label: string;
  value: string;
  strong?: boolean;
  tone?: "ok" | "bad";
}) {
  return (
    <div className="flex justify-between gap-4">
      <span className="whitespace-pre text-muted-foreground">{label}</span>
      <span
        className={`tabular ${strong ? "font-semibold" : ""} ${tone === "ok" ? "text-success" : tone === "bad" ? "text-destructive" : ""}`}
      >
        {value}
      </span>
    </div>
  );
}
