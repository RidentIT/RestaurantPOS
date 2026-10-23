import { PAYMENT_METHOD_LABELS } from "@/entities/order";
import { formatClock, formatDuration, formatMoney, formatShiftTime } from "@/entities/shift";
import {
  Badge,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  LoadingState,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/shared/ui";
import { useShiftDetail } from "../model/useShifts";

export interface ShiftDetailDialogProps {
  shiftId: string | null;
  onOpenChange: (open: boolean) => void;
}

/** One shift in full: its totals, how the cash counted up, and every order rung up during it. */
export function ShiftDetailDialog({ shiftId, onOpenChange }: ShiftDetailDialogProps) {
  const { data, isLoading } = useShiftDetail(shiftId);

  return (
    <Dialog open={!!shiftId} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl">
        {isLoading || !data ? (
          <LoadingState label="Loading the shift…" />
        ) : (
          <>
            <DialogHeader>
              <DialogTitle>
                {data.shift.cashierName} · {formatShiftTime(data.shift.checkedInAtUtc)}
              </DialogTitle>
              <DialogDescription>
                {data.shift.checkedOutAtUtc
                  ? `Closed ${formatShiftTime(data.shift.checkedOutAtUtc)} · worked ${formatDuration(data.shift.checkedInAtUtc, data.shift.checkedOutAtUtc)}`
                  : `Still open · ${formatDuration(data.shift.checkedInAtUtc, null)} so far`}
              </DialogDescription>
            </DialogHeader>

            <div className="grid gap-4 sm:grid-cols-2">
              <div className="space-y-1 rounded-lg border p-3 text-sm">
                <Line label="Orders" value={String(data.shift.summary.ordersCompleted)} />
                <Line label="Items sold" value={String(data.shift.summary.itemsSold)} />
                <Line label="Gross" value={formatMoney(data.shift.summary.grossSales)} />
                <Line label="Discounts" value={formatMoney(data.shift.summary.discountsGiven)} />
                <Line label="Net sales" value={formatMoney(data.shift.summary.netSales)} strong />
                {data.shift.summary.payments.map((p) => (
                  <Line key={p.method} label={`Paid by ${PAYMENT_METHOD_LABELS[p.method]}`} value={formatMoney(p.amount)} />
                ))}
              </div>

              <div className="space-y-1 rounded-lg border p-3 text-sm">
                <Line label="Opening cash" value={formatMoney(data.shift.openingCash)} />
                <Line label="Cash sales" value={formatMoney(data.shift.summary.cashSales)} />
                <Line label="Expected in drawer" value={formatMoney(data.shift.expectedCash)} strong />
                <Line
                  label="Counted"
                  value={data.shift.closingCash === null ? "Not counted" : formatMoney(data.shift.closingCash)}
                />
                <Line
                  label="Difference"
                  value={data.shift.variance === null ? "—" : formatMoney(data.shift.variance)}
                  bad={data.shift.variance !== null && data.shift.variance !== 0}
                />
                {data.shift.note && <p className="pt-2 text-muted-foreground">“{data.shift.note}”</p>}
              </div>
            </div>

            <div className="max-h-72 overflow-auto rounded-lg border">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Order</TableHead>
                    <TableHead>Table</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead>Paid at</TableHead>
                    <TableHead className="text-right">Total</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {data.orders.length === 0 ? (
                    <TableRow>
                      <TableCell colSpan={5} className="text-center text-muted-foreground">
                        No orders in this shift.
                      </TableCell>
                    </TableRow>
                  ) : (
                    data.orders.map((order) => (
                      <TableRow key={order.orderId}>
                        <TableCell>{order.orderNumber ? `#${String(order.orderNumber).padStart(3, "0")}` : "—"}</TableCell>
                        <TableCell>{order.tableNumber ?? "Takeaway"}</TableCell>
                        <TableCell>
                          <Badge variant={order.status === "Completed" ? "success" : "outline"}>{order.status}</Badge>
                        </TableCell>
                        <TableCell>{order.completedAtUtc ? formatClock(order.completedAtUtc) : "—"}</TableCell>
                        <TableCell className="tabular text-right">{formatMoney(order.total)}</TableCell>
                      </TableRow>
                    ))
                  )}
                </TableBody>
              </Table>
            </div>
          </>
        )}
      </DialogContent>
    </Dialog>
  );
}

function Line({ label, value, strong, bad }: { label: string; value: string; strong?: boolean; bad?: boolean }) {
  return (
    <div className="flex justify-between gap-4">
      <span className="text-muted-foreground">{label}</span>
      <span className={`tabular ${strong ? "font-semibold" : ""} ${bad ? "font-medium text-destructive" : ""}`}>
        {value}
      </span>
    </div>
  );
}
