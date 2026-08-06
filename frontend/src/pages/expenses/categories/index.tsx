import { useState } from "react";
import { Link } from "react-router-dom";
import { ArrowLeft, Layers, MoreHorizontal, Pencil, Plus, ShieldCheck, ShieldOff, Trash2 } from "lucide-react";
import { toast } from "sonner";
import type { ExpenseCategory } from "@/entities/expense";
import { ExpenseCategoryDialog, useExpenseCategories, useExpenseCategoryMutations } from "@/features/expenses";
import { toApiError } from "@/shared/api/problem";
import {
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
  value.toLocaleString(undefined, { minimumFractionDigits: 0, maximumFractionDigits: 0 });

/** Which categories exist and what each is budgeted at (EXP-010, EXP-011, BR-EXP-015). */
export default function ExpenseCategoriesPage() {
  const [form, setForm] = useState<ExpenseCategory | null | undefined>(undefined);
  const { data: categories, isLoading } = useExpenseCategories();
  const { setActive, remove } = useExpenseCategoryMutations();

  const toggleActive = async (category: ExpenseCategory) => {
    try {
      await setActive.mutateAsync({ id: category.id, isActive: !category.isActive });
      toast.success(category.isActive ? `${category.name} retired.` : `${category.name} is back in use.`);
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  };

  const deleteCategory = async (category: ExpenseCategory) => {
    try {
      await remove.mutateAsync(category.id);
      toast.success(`${category.name} was deleted.`);
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
          <h1 className="text-2xl font-semibold">Expense categories</h1>
          <p className="text-sm text-muted-foreground">
            Built-in categories can be renamed and re-budgeted but not deleted, so past reports keep their
            labels.
          </p>
        </div>
        <Button onClick={() => setForm(null)}>
          <Plus /> Add category
        </Button>
      </div>

      <Card>
        {isLoading ? (
          <LoadingState label="Loading categories…" />
        ) : !categories || categories.length === 0 ? (
          <EmptyState icon={<Layers className="size-6" />} title="No categories yet" />
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Category</TableHead>
                <TableHead>Description</TableHead>
                <TableHead className="text-right">Monthly budget</TableHead>
                <TableHead className="text-right">Expenses</TableHead>
                <TableHead>State</TableHead>
                <TableHead className="w-12" />
              </TableRow>
            </TableHeader>
            <TableBody>
              {categories.map((category) => (
                <TableRow key={category.id}>
                  <TableCell className="font-medium">
                    {category.parentCategoryName && (
                      <span className="text-muted-foreground">{category.parentCategoryName} › </span>
                    )}
                    {category.name}
                    {category.isSystem && (
                      <Badge variant="secondary" className="ml-2">
                        Built-in
                      </Badge>
                    )}
                  </TableCell>
                  <TableCell className="max-w-56 truncate text-sm text-muted-foreground">
                    {category.description ?? "—"}
                  </TableCell>
                  <TableCell className="text-right tabular">
                    {category.monthlyBudget === null ? (
                      <span className="text-muted-foreground">Not budgeted</span>
                    ) : (
                      money(category.monthlyBudget)
                    )}
                  </TableCell>
                  <TableCell className="text-right tabular text-muted-foreground">
                    {category.expenseCount}
                  </TableCell>
                  <TableCell>
                    {category.isActive ? (
                      <Badge variant="success">Active</Badge>
                    ) : (
                      <Badge variant="destructive">Retired</Badge>
                    )}
                  </TableCell>
                  <TableCell>
                    <DropdownMenu>
                      <DropdownMenuTrigger asChild>
                        <Button variant="ghost" size="icon">
                          <MoreHorizontal className="size-4" />
                        </Button>
                      </DropdownMenuTrigger>
                      <DropdownMenuContent align="end">
                        <DropdownMenuItem onSelect={() => setForm(category)}>
                          <Pencil /> Edit
                        </DropdownMenuItem>
                        <DropdownMenuItem
                          destructive={category.isActive}
                          onSelect={() => toggleActive(category)}
                        >
                          {category.isActive ? (
                            <>
                              <ShieldOff /> Retire
                            </>
                          ) : (
                            <>
                              <ShieldCheck /> Put back in use
                            </>
                          )}
                        </DropdownMenuItem>
                        {!category.isSystem && category.expenseCount === 0 && (
                          <DropdownMenuItem destructive onSelect={() => deleteCategory(category)}>
                            <Trash2 /> Delete
                          </DropdownMenuItem>
                        )}
                      </DropdownMenuContent>
                    </DropdownMenu>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </Card>

      <ExpenseCategoryDialog
        open={form !== undefined}
        onOpenChange={(open) => !open && setForm(undefined)}
        categories={categories ?? []}
        category={form ?? undefined}
      />
    </div>
  );
}
