import { CheckCircle2, Paperclip, XCircle } from "lucide-react";
import { toast } from "sonner";
import { EXPENSE_PAYMENT_METHOD_LABELS } from "@/entities/expense";
import { expensesApi, useExpense, useExpenseMutations } from "@/features/expenses";
import { toApiError } from "@/shared/api/problem";
import {
  Badge,
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  LoadingState,
  Separator,
} from "@/shared/ui";
import { STATUS_BADGE } from "./ExpensesTable";

const money = (value: number) =>
  value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });

/** One expense in full, with its receipts, approval history and the decisions still open on it. */
export function ExpenseDetailDialog({
  expenseId,
  onOpenChange,
  onEdit,
}: {
  expenseId: string | null;
  onOpenChange: (open: boolean) => void;
  onEdit: (expenseId: string) => void;
}) {
  const { data: expense, isLoading } = useExpense(expenseId ?? undefined);
  const { approve, reject, setPaid } = useExpenseMutations();

  const busy = approve.isPending || reject.isPending || setPaid.isPending;

  const decide = async (isApproval: boolean) => {
    if (!expense) return;

    try {
      const mutation = isApproval ? approve : reject;
      await mutation.mutateAsync({ ids: [expense.id], comments: null });
      toast.success(isApproval ? "Expense approved." : "Expense rejected.");
      onOpenChange(false);
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  };

  const togglePaid = async () => {
    if (!expense) return;

    try {
      await setPaid.mutateAsync({
        id: expense.id,
        isPaid: !expense.isPaid,
        paymentDate: expense.isPaid ? null : new Date().toISOString().slice(0, 10),
      });
      toast.success(expense.isPaid ? "Marked as unpaid." : "Marked as paid.");
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  };

  return (
    <Dialog open={!!expenseId} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[90vh] max-w-lg overflow-y-auto">
        {isLoading || !expense ? (
          <LoadingState label="Loading expense…" />
        ) : (
          <>
            <DialogHeader>
              <DialogTitle className="flex flex-wrap items-center gap-2">
                {expense.expenseNumber}
                <Badge variant={STATUS_BADGE[expense.status]}>{expense.status}</Badge>
                {expense.isRecurring && <Badge variant="secondary">Recurring</Badge>}
              </DialogTitle>
              <DialogDescription>
                {expense.categoryName} · {expense.expenseDate}
              </DialogDescription>
            </DialogHeader>

            <div className="space-y-4">
              <p className="text-3xl font-semibold tabular">{money(expense.amount)}</p>

              {expense.description && <p className="text-sm">{expense.description}</p>}

              <dl className="grid grid-cols-2 gap-x-4 gap-y-2 text-sm">
                <dt className="text-muted-foreground">Payment method</dt>
                <dd>{EXPENSE_PAYMENT_METHOD_LABELS[expense.paymentMethod]}</dd>

                <dt className="text-muted-foreground">Reference</dt>
                <dd>{expense.paymentReference ?? "—"}</dd>

                <dt className="text-muted-foreground">Payment date</dt>
                <dd>{expense.paymentDate ?? "—"}</dd>

                <dt className="text-muted-foreground">Paid</dt>
                <dd>{expense.isPaid ? "Yes" : "Not yet"}</dd>

                <dt className="text-muted-foreground">Recorded by</dt>
                <dd>{expense.recordedByName}</dd>

                {expense.approvedByName && (
                  <>
                    <dt className="text-muted-foreground">
                      {expense.status === "Rejected" ? "Rejected by" : "Approved by"}
                    </dt>
                    <dd>{expense.approvedByName}</dd>
                  </>
                )}
              </dl>

              {expense.approvalComments && (
                <p className="rounded-lg border bg-muted/40 p-3 text-sm">“{expense.approvalComments}”</p>
              )}

              {expense.attachments.length > 0 && (
                <div>
                  <p className="mb-1.5 text-sm font-medium">Receipts</p>
                  <ul className="space-y-1">
                    {expense.attachments.map((attachment) => (
                      <li key={attachment.id} className="flex items-center gap-2 text-sm">
                        <Paperclip className="size-3.5 shrink-0 text-muted-foreground" />
                        <a
                          href={expensesApi.attachmentUrl(attachment.id)}
                          target="_blank"
                          rel="noreferrer"
                          className="truncate underline-offset-2 hover:underline"
                        >
                          {attachment.fileName}
                        </a>
                      </li>
                    ))}
                  </ul>
                </div>
              )}

              {expense.approvalTrail.length > 0 && (
                <>
                  <Separator />
                  <div>
                    <p className="mb-2 text-sm font-medium">History</p>
                    <ol className="space-y-2">
                      {expense.approvalTrail.map((entry, index) => (
                        <li key={`${entry.actedAtUtc}-${index}`} className="text-sm">
                          <span className="font-medium">{entry.toStatus}</span>
                          <span className="text-muted-foreground">
                            {" "}
                            by {entry.actedByName} · {new Date(entry.actedAtUtc).toLocaleString()}
                          </span>
                          {entry.comments && (
                            <p className="text-muted-foreground italic">“{entry.comments}”</p>
                          )}
                        </li>
                      ))}
                    </ol>
                  </div>
                </>
              )}
            </div>

            <DialogFooter className="flex-wrap gap-2">
              <Button type="button" variant="outline" onClick={togglePaid} disabled={busy}>
                {expense.isPaid ? "Mark unpaid" : "Mark paid"}
              </Button>

              {expense.isEditable && (
                <>
                  <Button type="button" variant="outline" onClick={() => onEdit(expense.id)} disabled={busy}>
                    Edit
                  </Button>
                  <Button
                    type="button"
                    variant="outline"
                    onClick={() => decide(false)}
                    loading={reject.isPending}
                  >
                    <XCircle /> Reject
                  </Button>
                  <Button type="button" onClick={() => decide(true)} loading={approve.isPending}>
                    <CheckCircle2 /> Approve
                  </Button>
                </>
              )}
            </DialogFooter>
          </>
        )}
      </DialogContent>
    </Dialog>
  );
}
