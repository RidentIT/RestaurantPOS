import { Paperclip, Receipt, Repeat } from "lucide-react";
import type { ExpenseStatus, ExpenseSummary } from "@/entities/expense";
import { EXPENSE_PAYMENT_METHOD_LABELS } from "@/entities/expense";
import {
  Badge,
  Checkbox,
  EmptyState,
  LoadingState,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/shared/ui";

export const STATUS_BADGE: Record<ExpenseStatus, "outline" | "warning" | "success" | "destructive"> = {
  Draft: "outline",
  Pending: "warning",
  Approved: "success",
  Rejected: "destructive",
};

const money = (value: number) =>
  value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });

export function ExpensesTable({
  expenses,
  isLoading,
  selected,
  onToggle,
  onToggleAll,
  onOpen,
}: {
  expenses: ExpenseSummary[] | undefined;
  isLoading: boolean;
  selected: Set<string>;
  onToggle: (id: string) => void;
  onToggleAll: (ids: string[]) => void;
  onOpen: (expense: ExpenseSummary) => void;
}) {
  if (isLoading) {
    return <LoadingState label="Loading expenses…" />;
  }

  if (!expenses || expenses.length === 0) {
    return (
      <EmptyState
        icon={<Receipt className="size-6" />}
        title="No expenses match"
        description="Record one, or widen the filters."
      />
    );
  }

  // Only undecided expenses can be selected — approving something already approved is not an action.
  const selectableIds = expenses.filter((e) => e.status === "Draft" || e.status === "Pending").map((e) => e.id);
  const allSelected = selectableIds.length > 0 && selectableIds.every((id) => selected.has(id));

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead className="w-10">
            <Checkbox
              checked={allSelected}
              disabled={selectableIds.length === 0}
              onCheckedChange={() => onToggleAll(selectableIds)}
              aria-label="Select all expenses awaiting a decision"
            />
          </TableHead>
          <TableHead>Number</TableHead>
          <TableHead>Date</TableHead>
          <TableHead>Category</TableHead>
          <TableHead>Description</TableHead>
          <TableHead>Method</TableHead>
          <TableHead>Status</TableHead>
          <TableHead className="text-right">Amount</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {expenses.map((expense) => {
          const selectable = expense.status === "Draft" || expense.status === "Pending";

          return (
            <TableRow
              key={expense.id}
              className="cursor-pointer"
              onClick={() => onOpen(expense)}
            >
              <TableCell onClick={(event) => event.stopPropagation()}>
                <Checkbox
                  checked={selected.has(expense.id)}
                  disabled={!selectable}
                  onCheckedChange={() => onToggle(expense.id)}
                  aria-label={`Select ${expense.expenseNumber}`}
                />
              </TableCell>
              <TableCell className="whitespace-nowrap font-medium">
                <span className="flex items-center gap-1.5">
                  {expense.expenseNumber}
                  {expense.isRecurring && (
                    <Repeat className="size-3.5 text-muted-foreground" aria-label="Recurring" />
                  )}
                  {expense.attachmentCount > 0 && (
                    <Paperclip className="size-3.5 text-muted-foreground" aria-label="Has a receipt" />
                  )}
                </span>
              </TableCell>
              <TableCell className="whitespace-nowrap text-sm text-muted-foreground">
                {expense.expenseDate}
              </TableCell>
              <TableCell>{expense.categoryName}</TableCell>
              <TableCell className="max-w-56 truncate text-sm text-muted-foreground">
                {expense.description ?? "—"}
              </TableCell>
              <TableCell className="text-sm text-muted-foreground">
                {EXPENSE_PAYMENT_METHOD_LABELS[expense.paymentMethod]}
                {expense.isPaid ? "" : " · unpaid"}
              </TableCell>
              <TableCell>
                <Badge variant={STATUS_BADGE[expense.status]}>{expense.status}</Badge>
              </TableCell>
              <TableCell className="text-right font-medium tabular">{money(expense.amount)}</TableCell>
            </TableRow>
          );
        })}
      </TableBody>
    </Table>
  );
}
