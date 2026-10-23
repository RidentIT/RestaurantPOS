import { useMemo, useState } from "react";
import { toast } from "sonner";
import { formatMoney } from "@/entities/shift";
import type { Shift } from "@/entities/shift";
import { ShiftDetailDialog, ShiftsTable, useAllShifts, useShiftMutations } from "@/features/shifts";
import { toApiError } from "@/shared/api/problem";
import {
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  FormField,
  Input,
  Label,
  LoadingState,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/shared/ui";

const ALL = "__all__";
const isoDay = (date: Date) => date.toISOString().slice(0, 10);

/**
 * Every cashier's shifts — who worked when, what each one sold, and whether their drawer counted
 * up. Sits beside the day and month reports because a shift, unlike a calendar day, is what a
 * cashier is actually accountable for.
 */
export function ShiftsTab() {
  const [from, setFrom] = useState(() => isoDay(new Date(Date.now() - 6 * 86_400_000)));
  const [to, setTo] = useState(() => isoDay(new Date()));
  const [cashier, setCashier] = useState(ALL);
  const [openId, setOpenId] = useState<string | null>(null);
  const [closing, setClosing] = useState<Shift | null>(null);
  const [closeNote, setCloseNote] = useState("");

  const { data: shifts, isLoading } = useAllShifts(from, to);
  const { forceClose } = useShiftMutations();

  const cashiers = useMemo(() => {
    const seen = new Map<string, string>();
    (shifts ?? []).forEach((s) => seen.set(s.cashierUserId, s.cashierName));
    return [...seen.entries()];
  }, [shifts]);

  const visible = useMemo(
    () => (shifts ?? []).filter((s) => cashier === ALL || s.cashierUserId === cashier),
    [shifts, cashier],
  );

  const totals = useMemo(
    () => ({
      shifts: visible.length,
      orders: visible.reduce((sum, s) => sum + s.summary.ordersCompleted, 0),
      net: visible.reduce((sum, s) => sum + s.summary.netSales, 0),
      variance: visible.reduce((sum, s) => sum + (s.variance ?? 0), 0),
    }),
    [visible],
  );

  const confirmClose = async () => {
    if (!closing) return;

    try {
      await forceClose.mutateAsync({ id: closing.id, note: closeNote.trim() || null });
      toast.success(`${closing.cashierName}'s shift was closed.`);
      setClosing(null);
      setCloseNote("");
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  };

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-end gap-3">
        <div className="space-y-1.5">
          <Label htmlFor="shifts-from">From</Label>
          <Input id="shifts-from" type="date" value={from} max={to} onChange={(e) => setFrom(e.target.value)} className="w-44" />
        </div>
        <div className="space-y-1.5">
          <Label htmlFor="shifts-to">To</Label>
          <Input id="shifts-to" type="date" value={to} min={from} onChange={(e) => setTo(e.target.value)} className="w-44" />
        </div>
        <div className="space-y-1.5">
          <Label htmlFor="shifts-cashier">Cashier</Label>
          <Select value={cashier} onValueChange={setCashier}>
            <SelectTrigger id="shifts-cashier" className="w-48">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={ALL}>All cashiers</SelectItem>
              {cashiers.map(([id, name]) => (
                <SelectItem key={id} value={id}>
                  {name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>

      <div className="grid gap-3 sm:grid-cols-4">
        <Stat label="Shifts" value={String(totals.shifts)} />
        <Stat label="Orders" value={String(totals.orders)} />
        <Stat label="Net sales" value={formatMoney(totals.net)} />
        <Stat label="Cash difference" value={formatMoney(totals.variance)} bad={totals.variance !== 0} />
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Shifts</CardTitle>
        </CardHeader>
        <CardContent>
          {isLoading || !shifts ? (
            <LoadingState label="Loading shifts…" />
          ) : (
            <ShiftsTable
              shifts={visible}
              showCashier
              onOpen={(shift) => setOpenId(shift.id)}
              onForceClose={(shift) => setClosing(shift)}
              emptyTitle="No shifts in this period."
            />
          )}
        </CardContent>
      </Card>

      <ShiftDetailDialog shiftId={openId} onOpenChange={(open) => !open && setOpenId(null)} />

      <Dialog open={!!closing} onOpenChange={(open) => !open && setClosing(null)}>
        <DialogContent className="max-w-sm">
          <DialogHeader>
            <DialogTitle>Close {closing?.cashierName}'s shift?</DialogTitle>
            <DialogDescription>
              Closes it now without a cash count, even if orders are still unpaid. Use this when a cashier forgot
              to check out.
            </DialogDescription>
          </DialogHeader>
          <FormField htmlFor="close-note" label="Note (optional)">
            <Input id="close-note" value={closeNote} onChange={(e) => setCloseNote(e.target.value)} maxLength={300} />
          </FormField>
          <DialogFooter>
            <Button variant="outline" onClick={() => setClosing(null)} disabled={forceClose.isPending}>
              Cancel
            </Button>
            <Button onClick={confirmClose} loading={forceClose.isPending}>
              Close shift
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

function Stat({ label, value, bad }: { label: string; value: string; bad?: boolean }) {
  return (
    <Card className="p-4">
      <p className="text-sm text-muted-foreground">{label}</p>
      <p className={`tabular mt-1 text-2xl font-semibold ${bad ? "text-destructive" : ""}`}>{value}</p>
    </Card>
  );
}
