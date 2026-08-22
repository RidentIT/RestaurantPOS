import { useState } from "react";
import { Link } from "react-router-dom";
import { ArrowLeft, MoreHorizontal, Pencil, Plus, Repeat, ShieldCheck, ShieldOff } from "lucide-react";
import { toast } from "sonner";
import type { RecurringExpense } from "@/entities/expense";
import { EXPENSE_PAYMENT_METHOD_LABELS } from "@/entities/expense";
import {
  RecurringExpenseDialog,
  useExpenseCategories,
  useRecurringExpenseMutations,
  useRecurringExpenses,
} from "@/features/expenses";
import { toApiError } from "@/shared/api/problem";
import {
  Alert,
  AlertDescription,
  Badge,
  Button,
  Card,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  EmptyState,
  LoadingState,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/shared/ui";

const money = (value: number) =>
  value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });

const ordinal = (day: number) => {
  const suffix = day % 10 === 1 && day !== 11 ? "st" : day % 10 === 2 && day !== 12 ? "nd" : day % 10 === 3 && day !== 13 ? "rd" : "th";

  return `${day}${suffix}`;
};

/** Standing monthly costs such as rent and salaries (EXP-012, BR-EXP-008). */
export default function RecurringExpensesPage() {
  const [form, setForm] = useState<RecurringExpense | null | undefined>(undefined);
  const { data: recurring, isLoading } = useRecurringExpenses();
  const { data: categories } = useExpenseCategories();
  const { setActive } = useRecurringExpenseMutations();

  const activeCategories = (categories ?? []).filter((c) => c.isActive);

  const toggleActive = async (item: RecurringExpense) => {
    try {
      await setActive.mutateAsync({ id: item.id, isActive: !item.isActive });
      toast.success(item.isActive ? "Stopped." : "Resumed.");
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  };

  return (
    <div className="space-y-6 p-8">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div className="space-y-1">
          <Button variant="ghost" size="sm" asChild className="-ml-2">
            <Link to="/expenses">
              <ArrowLeft /> Back to expenses
            </Link>
          </Button>
          <h1 className="text-2xl font-semibold">Recurring expenses</h1>
          <p className="text-sm text-muted-foreground">
            Costs that repeat every month without being keyed in again.
          </p>
        </div>
        <Button onClick={() => setForm(null)} disabled={activeCategories.length === 0}>
          <Plus /> Add recurring expense
        </Button>
      </div>

      <Alert variant="info">
        <Repeat className="size-4" />
        <AlertDescription>
          Each month's expenses are created as drafts the first time the Expenses screen is opened that
          month — including any month the restaurant was closed for. Nothing counts towards a report until
          it has been approved.
        </AlertDescription>
      </Alert>

      <Card>
        {isLoading ? (
          <LoadingState label="Loading recurring expenses…" />
        ) : !recurring || recurring.length === 0 ? (
          <EmptyState
            icon={<Repeat className="size-6" />}
            title="No recurring expenses"
            description="Set up rent or salaries so they appear each month on their own."
          />
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Category</TableHead>
                <TableHead>Description</TableHead>
                <TableHead>Falls on</TableHead>
                <TableHead>Method</TableHead>
                <TableHead>Last created</TableHead>
                <TableHead>State</TableHead>
                <TableHead className="text-right">Amount</TableHead>
                <TableHead className="w-12" />
              </TableRow>
            </TableHeader>
            <TableBody>
              {recurring.map((item) => (
                <TableRow key={item.id}>
                  <TableCell className="font-medium">{item.categoryName}</TableCell>
                  <TableCell className="max-w-56 truncate text-sm text-muted-foreground">
                    {item.description ?? "—"}
                  </TableCell>
                  <TableCell className="text-sm text-muted-foreground">
                    {ordinal(item.dayOfMonth)} of the month
                  </TableCell>
                  <TableCell className="text-sm text-muted-foreground">
                    {EXPENSE_PAYMENT_METHOD_LABELS[item.paymentMethod]}
                  </TableCell>
                  <TableCell className="text-sm text-muted-foreground tabular">
                    {item.lastGeneratedYear && item.lastGeneratedMonth
                      ? `${item.lastGeneratedYear}-${String(item.lastGeneratedMonth).padStart(2, "0")}`
                      : "Never"}
                  </TableCell>
                  <TableCell>
                    {item.isActive ? (
                      <Badge variant="success">Active</Badge>
                    ) : (
                      <Badge variant="destructive">Stopped</Badge>
                    )}
                  </TableCell>
                  <TableCell className="text-right font-medium tabular">{money(item.amount)}</TableCell>
                  <TableCell>
                    <DropdownMenu>
                      <DropdownMenuTrigger asChild>
                        <Button variant="ghost" size="icon">
                          <MoreHorizontal className="size-4" />
                        </Button>
                      </DropdownMenuTrigger>
                      <DropdownMenuContent align="end">
                        <DropdownMenuItem onSelect={() => setForm(item)}>
                          <Pencil /> Edit
                        </DropdownMenuItem>
                        <DropdownMenuItem destructive={item.isActive} onSelect={() => toggleActive(item)}>
                          {item.isActive ? (
                            <>
                              <ShieldOff /> Stop
                            </>
                          ) : (
                            <>
                              <ShieldCheck /> Resume
                            </>
                          )}
                        </DropdownMenuItem>
                      </DropdownMenuContent>
                    </DropdownMenu>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </Card>

      <RecurringExpenseDialog
        open={form !== undefined}
        onOpenChange={(open) => !open && setForm(undefined)}
        categories={activeCategories}
        recurring={form ?? undefined}
      />
    </div>
  );
}
