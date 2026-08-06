import type {
  DailyExpenseReport,
  Expense,
  ExpenseCategory,
  ExpenseCategoryPayload,
  ExpenseFilters,
  ExpensePayload,
  ExpenseRangeReport,
  ExpenseSummary,
  MonthlyExpenseReport,
  RecurringExpense,
  RecurringExpensePayload,
} from "@/entities/expense";
import { axiosClient } from "@/shared/api/axiosClient";
import { API_ENDPOINTS, apiService } from "@/shared/api/endpoints";

export const expensesApi = {
  list: (filters: ExpenseFilters = {}) =>
    apiService.get<ExpenseSummary[]>(API_ENDPOINTS.EXPENSES.BASE, {
      from: filters.from,
      to: filters.to,
      categoryId: filters.categoryId,
      paymentMethod: filters.paymentMethod,
      status: filters.status,
      isPaid: filters.isPaid,
      search: filters.search || undefined,
    }),

  getById: (id: string) => apiService.get<Expense>(API_ENDPOINTS.EXPENSES.BY_ID(id)),

  create: (payload: ExpensePayload) => apiService.post<Expense>(API_ENDPOINTS.EXPENSES.BASE, payload),

  update: (id: string, payload: ExpensePayload) =>
    apiService.put<Expense>(API_ENDPOINTS.EXPENSES.BY_ID(id), payload),

  remove: (id: string) => apiService.delete<void>(API_ENDPOINTS.EXPENSES.BY_ID(id)),

  submit: (id: string, comments: string | null) =>
    apiService.post<Expense>(API_ENDPOINTS.EXPENSES.SUBMIT(id), { comments }),

  approve: (expenseIds: string[], comments: string | null) =>
    apiService.post<Expense[]>(API_ENDPOINTS.EXPENSES.APPROVE, { expenseIds, comments }),

  reject: (expenseIds: string[], comments: string | null) =>
    apiService.post<Expense[]>(API_ENDPOINTS.EXPENSES.REJECT, { expenseIds, comments }),

  setPaid: (id: string, isPaid: boolean, paymentDate: string | null) =>
    apiService.put<Expense>(API_ENDPOINTS.EXPENSES.PAID(id), { isPaid, paymentDate }),

  addAttachment: (id: string, file: File) => {
    const form = new FormData();
    form.append("file", file);

    return axiosClient
      .post<Expense>(API_ENDPOINTS.EXPENSES.ATTACHMENTS(id), form)
      .then((response) => response.data);
  },

  removeAttachment: (id: string, attachmentId: string) =>
    apiService.delete<Expense>(API_ENDPOINTS.EXPENSES.REMOVE_ATTACHMENT(id, attachmentId)),

  /** The URL a receipt can be opened at. Served inline so an image shows in a viewer. */
  attachmentUrl: (attachmentId: string) =>
    `${axiosClient.defaults.baseURL ?? ""}${API_ENDPOINTS.EXPENSES.ATTACHMENT_BY_ID(attachmentId)}`,

  categories: (isActive?: boolean) =>
    apiService.get<ExpenseCategory[]>(API_ENDPOINTS.EXPENSES.CATEGORIES, { isActive }),

  createCategory: (payload: ExpenseCategoryPayload) =>
    apiService.post<ExpenseCategory>(API_ENDPOINTS.EXPENSES.CATEGORIES, payload),

  updateCategory: (id: string, payload: ExpenseCategoryPayload) =>
    apiService.put<ExpenseCategory>(API_ENDPOINTS.EXPENSES.CATEGORY_BY_ID(id), payload),

  setCategoryActive: (id: string, isActive: boolean) =>
    apiService.put<ExpenseCategory>(API_ENDPOINTS.EXPENSES.CATEGORY_STATUS(id), { isActive }),

  deleteCategory: (id: string) => apiService.delete<void>(API_ENDPOINTS.EXPENSES.CATEGORY_BY_ID(id)),

  recurring: () => apiService.get<RecurringExpense[]>(API_ENDPOINTS.EXPENSES.RECURRING),

  createRecurring: (payload: RecurringExpensePayload) =>
    apiService.post<RecurringExpense>(API_ENDPOINTS.EXPENSES.RECURRING, payload),

  updateRecurring: (id: string, payload: RecurringExpensePayload) =>
    apiService.put<RecurringExpense>(API_ENDPOINTS.EXPENSES.RECURRING_BY_ID(id), payload),

  setRecurringActive: (id: string, isActive: boolean) =>
    apiService.put<RecurringExpense>(API_ENDPOINTS.EXPENSES.RECURRING_STATUS(id), { isActive }),

  generateRecurring: () => apiService.post<number>(API_ENDPOINTS.EXPENSES.RECURRING_GENERATE),

  dailyReport: (date: string) =>
    apiService.get<DailyExpenseReport>(API_ENDPOINTS.EXPENSES.REPORT_DAILY, { date }),

  monthlyReport: (year: number, month: number) =>
    apiService.get<MonthlyExpenseReport>(API_ENDPOINTS.EXPENSES.REPORT_MONTHLY, { year, month }),

  rangeReport: (from: string, to: string) =>
    apiService.get<ExpenseRangeReport>(API_ENDPOINTS.EXPENSES.REPORT_RANGE, { from, to }),
};
