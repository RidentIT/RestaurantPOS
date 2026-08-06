import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import {
  BarChart3,
  CheckCircle2,
  FileSpreadsheet,
  Plus,
  Repeat,
  Search,
  Settings2,
  XCircle,
} from "lucide-react";
import { toast } from "sonner";
import type { Expense, ExpenseFilters, ExpensePaymentMethod, ExpenseStatus } from "@/entities/expense";
import {
  EXPENSE_PAYMENT_METHODS,
  EXPENSE_PAYMENT_METHOD_LABELS,
  EXPENSE_STATUSES,
} from "@/entities/expense";
import {
  ExpenseFormDialog,
  exportExpenseListExcel,
  expensesApi,
  useExpenseCategories,
  useExpenseMutations,
  useExpenses,
  useGenerateDueRecurringExpenses,
} from "@/features/expenses";
import { toApiError } from "@/shared/api/problem";
import {
  Button,
  Card,
  Input,
  Label,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/shared/ui";
import { ExpenseDetailDialog } from "./ExpenseDetailDialog";
import { ExpensesTable } from "./ExpensesTable";

const ALL = "all";
const money = (value: number) =>
  value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });

const startOfMonth = () => {
  const now = new Date();

  return new Date(now.getFullYear(), now.getMonth(), 1).toISOString().slice(0, 10);
};

const today = () => new Date().toISOString().slice(0, 10);

/**
 * The expense register: everything spent, with the filters to find any of it (EXP-020 to EXP-024)
 * and bulk approval for signing off a day's bills in one go.
 */
export default function ExpensesPage() {
  const [from, setFrom] = useState(startOfMonth());
  const [to, setTo] = useState(today());
  const [categoryId, setCategoryId] = useState<string>(ALL);
  const [paymentMethod, setPaymentMethod] = useState<string>(ALL);
  const [status, setStatus] = useState<string>(ALL);
  const [search, setSearch] = useState("");

  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<Expense | undefined>(undefined);
  const [detailId, setDetailId] = useState<string | null>(null);

  // Standing costs materialise the first time this screen is opened in a month.
  useGenerateDueRecurringExpenses((count) =>
    toast.info(`${count} recurring expense${count === 1 ? "" : "s"} created as ${count === 1 ? "a draft" : "drafts"}.`),
  );

  const filters: ExpenseFilters = useMemo(
    () => ({
      from,
      to,
      categoryId: categoryId === ALL ? undefined : categoryId,
      paymentMethod: paymentMethod === ALL ? undefined : (paymentMethod as ExpensePaymentMethod),
      status: status === ALL ? undefined : (status as ExpenseStatus),
      search: search.trim() || undefined,
    }),
    [from, to, categoryId, paymentMethod, status, search],
  );

  const { data: expenses, isLoading } = useExpenses(filters);
  const { data: categories } = useExpenseCategories();
  const { approve, reject } = useExpenseMutations();

  const activeCategories = (categories ?? []).filter((c) => c.isActive);

  const total = (expenses ?? []).reduce((sum, e) => sum + e.amount, 0);
  const approvedTotal = (expenses ?? [])
    .filter((e) => e.status === "Approved")
    .reduce((sum, e) => sum + e.amount, 0);
  const awaiting = (expenses ?? []).filter((e) => e.status === "Draft" || e.status === "Pending").length;

  const toggle = (id: string) =>
    setSelected((current) => {
      const next = new Set(current);
      next.has(id) ? next.delete(id) : next.add(id);

      return next;
    });

  const toggleAll = (ids: string[]) =>
    setSelected((current) => (ids.every((id) => current.has(id)) ? new Set() : new Set(ids)));

  const decide = async (isApproval: boolean) => {
    const ids = [...selected];

    try {
      const mutation = isApproval ? approve : reject;
      await mutation.mutateAsync({ ids, comments: isApproval ? "Bulk approved" : null });
      toast.success(`${ids.length} expense${ids.length === 1 ? "" : "s"} ${isApproval ? "approved" : "rejected"}.`);
      setSelected(new Set());
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  };

  const openForEdit = async (expenseId: string) => {
    try {
      const expense = await expensesApi.getById(expenseId);
      setDetailId(null);
      setEditing(expense);
      setFormOpen(true);
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  };

  return (
    <div className="space-y-6 p-8">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold">Expenses</h1>
          <p className="text-sm text-muted-foreground">
            {money(total)} recorded in this range · {money(approvedTotal)} approved
            {awaiting > 0 && ` · ${awaiting} awaiting a decision`}
          </p>
        </div>

        <div className="flex flex-wrap gap-2">
          <Button variant="outline" asChild>
            <Link to="/expenses/reports">
              <BarChart3 /> Reports
            </Link>
          </Button>
          <Button variant="outline" asChild>
            <Link to="/expenses/categories">
              <Settings2 /> Categories
            </Link>
          </Button>
          <Button variant="outline" asChild>
            <Link to="/expenses/recurring">
              <Repeat /> Recurring
            </Link>
          </Button>
          <Button
            variant="outline"
            disabled={!expenses || expenses.length === 0}
            onClick={() => exportExpenseListExcel(expenses ?? [], `${from}_to_${to}`)}
          >
            <FileSpreadsheet /> Excel
          </Button>
          <Button
            onClick={() => {
              setEditing(undefined);
              setFormOpen(true);
            }}
          >
            <Plus /> Record expense
          </Button>
        </div>
      </div>

      <Card className="p-4">
        <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-6">
          <div className="space-y-1.5">
            <Label htmlFor="from">From</Label>
            <Input id="from" type="date" value={from} onChange={(e) => setFrom(e.target.value)} />
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="to">To</Label>
            <Input id="to" type="date" value={to} onChange={(e) => setTo(e.target.value)} />
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="filter-category">Category</Label>
            <Select value={categoryId} onValueChange={setCategoryId}>
              <SelectTrigger id="filter-category">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value={ALL}>All categories</SelectItem>
                {activeCategories.map((category) => (
                  <SelectItem key={category.id} value={category.id}>
                    {category.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="filter-method">Method</Label>
            <Select value={paymentMethod} onValueChange={setPaymentMethod}>
              <SelectTrigger id="filter-method">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value={ALL}>All methods</SelectItem>
                {EXPENSE_PAYMENT_METHODS.map((method) => (
                  <SelectItem key={method} value={method}>
                    {EXPENSE_PAYMENT_METHOD_LABELS[method]}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="filter-status">Status</Label>
            <Select value={status} onValueChange={setStatus}>
              <SelectTrigger id="filter-status">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value={ALL}>All statuses</SelectItem>
                {EXPENSE_STATUSES.map((option) => (
                  <SelectItem key={option} value={option}>
                    {option}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="filter-search">Search</Label>
            <div className="relative">
              <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                id="filter-search"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="Description or reference"
                className="pl-9"
              />
            </div>
          </div>
        </div>
      </Card>

      {selected.size > 0 && (
        <div className="flex flex-wrap items-center gap-3 rounded-lg border border-primary/30 bg-primary/5 px-4 py-3">
          <span className="text-sm font-medium">
            {selected.size} selected ·{" "}
            {money(
              (expenses ?? [])
                .filter((e) => selected.has(e.id))
                .reduce((sum, e) => sum + e.amount, 0),
            )}
          </span>
          <div className="ml-auto flex gap-2">
            <Button variant="outline" size="sm" onClick={() => setSelected(new Set())}>
              Clear
            </Button>
            <Button variant="outline" size="sm" onClick={() => decide(false)} loading={reject.isPending}>
              <XCircle /> Reject
            </Button>
            <Button size="sm" onClick={() => decide(true)} loading={approve.isPending}>
              <CheckCircle2 /> Approve all
            </Button>
          </div>
        </div>
      )}

      <Card>
        <ExpensesTable
          expenses={expenses}
          isLoading={isLoading}
          selected={selected}
          onToggle={toggle}
          onToggleAll={toggleAll}
          onOpen={(expense) => setDetailId(expense.id)}
        />
      </Card>

      <ExpenseFormDialog
        open={formOpen}
        onOpenChange={(open) => {
          setFormOpen(open);
          if (!open) setEditing(undefined);
        }}
        categories={activeCategories}
        expense={editing}
      />

      <ExpenseDetailDialog
        expenseId={detailId}
        onOpenChange={(open) => !open && setDetailId(null)}
        onEdit={openForEdit}
      />
    </div>
  );
}
