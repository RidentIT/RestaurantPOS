import { useEffect, useRef } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import type {
  Expense,
  ExpenseCategory,
  ExpenseCategoryPayload,
  ExpenseFilters,
  ExpensePayload,
  RecurringExpense,
  RecurringExpensePayload,
} from "@/entities/expense";
import { expensesApi } from "../api/expensesApi";

export const EXPENSES_KEY = "expenses";
export const EXPENSE_KEY = "expense";
export const EXPENSE_CATEGORIES_KEY = "expense-categories";
export const RECURRING_EXPENSES_KEY = "recurring-expenses";
export const EXPENSE_REPORT_KEY = "expense-report";

export function useExpenses(filters: ExpenseFilters = {}) {
  return useQuery({
    queryKey: [EXPENSES_KEY, filters],
    queryFn: () => expensesApi.list(filters),
    placeholderData: (previous) => previous,
  });
}

export function useExpense(id: string | undefined) {
  return useQuery({
    queryKey: [EXPENSE_KEY, id],
    queryFn: () => expensesApi.getById(id!),
    enabled: !!id,
  });
}

export function useExpenseCategories(isActive?: boolean) {
  return useQuery({
    queryKey: [EXPENSE_CATEGORIES_KEY, isActive],
    queryFn: () => expensesApi.categories(isActive),
  });
}

export function useRecurringExpenses() {
  return useQuery({ queryKey: [RECURRING_EXPENSES_KEY], queryFn: expensesApi.recurring });
}

/**
 * Materialises any recurring expense still owed, once per mount.
 *
 * This is what stands in for a scheduler: the restaurant's machine is off overnight, so rent
 * appears the first time somebody opens the expenses screen rather than at midnight on the 1st.
 * The ref guards against React's development double-mount firing it twice — the server is
 * idempotent regardless, but a duplicate request would still show a misleading toast.
 */
export function useGenerateDueRecurringExpenses(onGenerated?: (count: number) => void) {
  const queryClient = useQueryClient();
  const hasRun = useRef(false);

  useEffect(() => {
    if (hasRun.current) return;
    hasRun.current = true;

    expensesApi
      .generateRecurring()
      .then((count) => {
        if (count > 0) {
          queryClient.invalidateQueries({ queryKey: [EXPENSES_KEY] });
          queryClient.invalidateQueries({ queryKey: [RECURRING_EXPENSES_KEY] });
          onGenerated?.(count);
        }
      })
      // A failure here must not break the screen: the expense list itself is unaffected, and the
      // generation will simply be retried the next time the page is opened.
      .catch(() => undefined);
  }, [queryClient, onGenerated]);
}

export function useExpenseMutations() {
  const queryClient = useQueryClient();

  const invalidate = (id?: string) => {
    queryClient.invalidateQueries({ queryKey: [EXPENSES_KEY] });
    queryClient.invalidateQueries({ queryKey: [EXPENSE_REPORT_KEY] });
    if (id) queryClient.invalidateQueries({ queryKey: [EXPENSE_KEY, id] });
  };

  const create = useMutation<Expense, Error, ExpensePayload>({
    mutationFn: expensesApi.create,
    onSuccess: (expense) => invalidate(expense.id),
  });

  const update = useMutation<Expense, Error, { id: string; payload: ExpensePayload }>({
    mutationFn: ({ id, payload }) => expensesApi.update(id, payload),
    onSuccess: (expense) => invalidate(expense.id),
  });

  const remove = useMutation<void, Error, string>({
    mutationFn: expensesApi.remove,
    onSuccess: () => invalidate(),
  });

  const submit = useMutation<Expense, Error, { id: string; comments: string | null }>({
    mutationFn: ({ id, comments }) => expensesApi.submit(id, comments),
    onSuccess: (expense) => invalidate(expense.id),
  });

  const approve = useMutation<Expense[], Error, { ids: string[]; comments: string | null }>({
    mutationFn: ({ ids, comments }) => expensesApi.approve(ids, comments),
    onSuccess: () => invalidate(),
  });

  const reject = useMutation<Expense[], Error, { ids: string[]; comments: string | null }>({
    mutationFn: ({ ids, comments }) => expensesApi.reject(ids, comments),
    onSuccess: () => invalidate(),
  });

  const setPaid = useMutation<Expense, Error, { id: string; isPaid: boolean; paymentDate: string | null }>({
    mutationFn: ({ id, isPaid, paymentDate }) => expensesApi.setPaid(id, isPaid, paymentDate),
    onSuccess: (expense) => invalidate(expense.id),
  });

  const addAttachment = useMutation<Expense, Error, { id: string; file: File }>({
    mutationFn: ({ id, file }) => expensesApi.addAttachment(id, file),
    onSuccess: (expense) => invalidate(expense.id),
  });

  const removeAttachment = useMutation<Expense, Error, { id: string; attachmentId: string }>({
    mutationFn: ({ id, attachmentId }) => expensesApi.removeAttachment(id, attachmentId),
    onSuccess: (expense) => invalidate(expense.id),
  });

  return { create, update, remove, submit, approve, reject, setPaid, addAttachment, removeAttachment };
}

export function useExpenseCategoryMutations() {
  const queryClient = useQueryClient();
  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: [EXPENSE_CATEGORIES_KEY] });
    queryClient.invalidateQueries({ queryKey: [EXPENSE_REPORT_KEY] });
  };

  const create = useMutation<ExpenseCategory, Error, ExpenseCategoryPayload>({
    mutationFn: expensesApi.createCategory,
    onSuccess: invalidate,
  });

  const update = useMutation<ExpenseCategory, Error, { id: string; payload: ExpenseCategoryPayload }>({
    mutationFn: ({ id, payload }) => expensesApi.updateCategory(id, payload),
    onSuccess: invalidate,
  });

  const setActive = useMutation<ExpenseCategory, Error, { id: string; isActive: boolean }>({
    mutationFn: ({ id, isActive }) => expensesApi.setCategoryActive(id, isActive),
    onSuccess: invalidate,
  });

  const remove = useMutation<void, Error, string>({
    mutationFn: expensesApi.deleteCategory,
    onSuccess: invalidate,
  });

  return { create, update, setActive, remove };
}

export function useRecurringExpenseMutations() {
  const queryClient = useQueryClient();
  const invalidate = () => queryClient.invalidateQueries({ queryKey: [RECURRING_EXPENSES_KEY] });

  const save = useMutation<RecurringExpense, Error, { id?: string; payload: RecurringExpensePayload }>({
    mutationFn: ({ id, payload }) =>
      id ? expensesApi.updateRecurring(id, payload) : expensesApi.createRecurring(payload),
    onSuccess: invalidate,
  });

  const setActive = useMutation<RecurringExpense, Error, { id: string; isActive: boolean }>({
    mutationFn: ({ id, isActive }) => expensesApi.setRecurringActive(id, isActive),
    onSuccess: invalidate,
  });

  return { save, setActive };
}

export function useDailyExpenseReport(date: string) {
  return useQuery({
    queryKey: [EXPENSE_REPORT_KEY, "daily", date],
    queryFn: () => expensesApi.dailyReport(date),
  });
}

export function useMonthlyExpenseReport(year: number, month: number) {
  return useQuery({
    queryKey: [EXPENSE_REPORT_KEY, "monthly", year, month],
    queryFn: () => expensesApi.monthlyReport(year, month),
  });
}
