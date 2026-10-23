import { formatDuration, formatMoney, formatShiftTime } from "@/entities/shift";
import type { Shift } from "@/entities/shift";
import {
  Badge,
  Button,
  EmptyState,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/shared/ui";

export interface ShiftsTableProps {
  shifts: Shift[];
  /** Show who worked each shift — for the owner, who sees everyone's. */
  showCashier?: boolean;
  onOpen: (shift: Shift) => void;
  /** When given, an open shift gets a "Close" action — the owner closing one a cashier forgot. */
  onForceClose?: (shift: Shift) => void;
  emptyTitle?: string;
}

/** Shifts as rows: when, how long, what they sold, and how the cash counted up. */
export function ShiftsTable({
  shifts,
  showCashier = false,
  onOpen,
  onForceClose,
  emptyTitle = "No shifts yet.",
}: ShiftsTableProps) {
  if (shifts.length === 0) {
    return <EmptyState title={emptyTitle} />;
  }

  return (
    <div className="overflow-x-auto">
      <Table>
        <TableHeader>
          <TableRow>
            {showCashier && <TableHead>Cashier</TableHead>}
            <TableHead>Checked in</TableHead>
            <TableHead>Checked out</TableHead>
            <TableHead className="text-right">Orders</TableHead>
            <TableHead className="text-right">Net sales</TableHead>
            <TableHead className="text-right">Expected cash</TableHead>
            <TableHead className="text-right">Counted</TableHead>
            <TableHead className="text-right">Difference</TableHead>
            <TableHead />
          </TableRow>
        </TableHeader>
        <TableBody>
          {shifts.map((shift) => (
            <TableRow key={shift.id} className="cursor-pointer" onClick={() => onOpen(shift)}>
              {showCashier && <TableCell className="font-medium">{shift.cashierName}</TableCell>}
              <TableCell>{formatShiftTime(shift.checkedInAtUtc)}</TableCell>
              <TableCell>
                {shift.checkedOutAtUtc ? (
                  <>
                    {formatShiftTime(shift.checkedOutAtUtc)}
                    <span className="ml-2 text-xs text-muted-foreground">
                      {formatDuration(shift.checkedInAtUtc, shift.checkedOutAtUtc)}
                    </span>
                  </>
                ) : (
                  <Badge variant="success">Open · {formatDuration(shift.checkedInAtUtc, null)}</Badge>
                )}
              </TableCell>
              <TableCell className="tabular text-right">{shift.summary.ordersCompleted}</TableCell>
              <TableCell className="tabular text-right font-medium">{formatMoney(shift.summary.netSales)}</TableCell>
              <TableCell className="tabular text-right">{formatMoney(shift.expectedCash)}</TableCell>
              <TableCell className="tabular text-right">
                {shift.closingCash === null ? "—" : formatMoney(shift.closingCash)}
              </TableCell>
              <TableCell
                className={`tabular text-right ${
                  shift.variance === null || shift.variance === 0 ? "" : "font-medium text-destructive"
                }`}
              >
                {shift.variance === null ? "—" : formatMoney(shift.variance)}
              </TableCell>
              <TableCell className="text-right" onClick={(event) => event.stopPropagation()}>
                {onForceClose && shift.isOpen && (
                  <Button size="sm" variant="outline" onClick={() => onForceClose(shift)}>
                    Close shift
                  </Button>
                )}
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}
