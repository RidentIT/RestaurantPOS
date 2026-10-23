import { Link } from "react-router-dom";
import { Clock, History, LogIn, LogOut } from "lucide-react";
import { formatClock, formatDuration, formatMoney } from "@/entities/shift";
import type { Shift } from "@/entities/shift";
import { Alert, AlertDescription, Button, Card } from "@/shared/ui";

/**
 * Where the cashier starts and ends their shift. Until they check in the till is locked — every
 * order is counted on a shift — so the not-checked-in state is a prominent call to action rather
 * than a small badge.
 */
export function ShiftBar({
  shift,
  onCheckIn,
  onCheckOut,
}: {
  shift: Shift | null;
  onCheckIn: () => void;
  onCheckOut: () => void;
}) {
  if (!shift) {
    return (
      <Alert variant="warning" className="flex flex-wrap items-center justify-between gap-3">
        <AlertDescription className="text-sm">
          You're not checked in. Check in to start your shift before taking orders.
        </AlertDescription>
        <div className="flex gap-2">
          <Button variant="outline" asChild>
            <Link to="/pos/shifts">
              <History /> My shifts
            </Link>
          </Button>
          <Button onClick={onCheckIn}>
            <LogIn /> Check in
          </Button>
        </div>
      </Alert>
    );
  }

  return (
    <Card className="flex flex-wrap items-center justify-between gap-3 p-3">
      <div className="flex flex-wrap items-center gap-x-6 gap-y-1 text-sm">
        <span className="flex items-center gap-2 font-medium">
          <Clock className="size-4 text-success" />
          On shift since {formatClock(shift.checkedInAtUtc)}
          <span className="font-normal text-muted-foreground">
            ({formatDuration(shift.checkedInAtUtc, null)})
          </span>
        </span>
        <span className="text-muted-foreground">
          {shift.summary.ordersCompleted} order{shift.summary.ordersCompleted === 1 ? "" : "s"} · net{" "}
          <span className="tabular font-medium text-foreground">{formatMoney(shift.summary.netSales)}</span>
        </span>
        <span className="text-muted-foreground">
          Drawer should hold{" "}
          <span className="tabular font-medium text-foreground">{formatMoney(shift.expectedCash)}</span>
        </span>
      </div>

      <div className="flex gap-2">
        <Button variant="outline" asChild>
          <Link to="/pos/shifts">
            <History /> My shifts
          </Link>
        </Button>
        <Button variant="outline" onClick={onCheckOut}>
          <LogOut /> Check out
        </Button>
      </div>
    </Card>
  );
}
