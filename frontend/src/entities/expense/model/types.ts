/** Where an expense sits in its approval life (EXP-015). */
export type ExpenseStatus = "Draft" | "Pending" | "Approved" | "Rejected";

export const EXPENSE_STATUSES: ExpenseStatus[] = ["Draft", "Pending", "Approved", "Rejected"];

/** How an expense was settled (BR-EXP-011). */
export type ExpensePaymentMethod = "Cash" | "Cheque" | "Card" | "BankTransfer";

export const EXPENSE_PAYMENT_METHODS: ExpensePaymentMethod[] = ["Cash", "Cheque", "Card", "BankTransfer"];

export const EXPENSE_PAYMENT_METHOD_LABELS: Record<ExpensePaymentMethod, string> = {
  Cash: "Cash",
  Cheque: "Cheque",
  Card: "Card",
  BankTransfer: "Bank Transfer",
};

/** Cash needs no paper trail of its own; everything else must carry one. */
export const requiresReference = (method: ExpensePaymentMethod): boolean => method !== "Cash";

export interface ExpenseCategory {
  id: string;
  name: string;
  description: string | null;
  /** What the restaurant expects to spend here monthly. Null means unbudgeted — no alerts. */
  monthlyBudget: number | null;
  parentCategoryId: string | null;
  parentCategoryName: string | null;
  /** Built-in categories can be renamed and re-budgeted but never deleted. */
  isSystem: boolean;
  isActive: boolean;
  expenseCount: number;
}

export interface ExpenseCategoryPayload {
  name: string;
  description: string | null;
  monthlyBudget: number | null;
  parentCategoryId: string | null;
}

export interface ExpenseAttachment {
  id: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  uploadedAtUtc: string;
  uploadedByName: string;
}

export interface ExpenseApprovalEntry {
  fromStatus: ExpenseStatus;
  toStatus: ExpenseStatus;
  actedByUserId: string;
  actedByName: string;
  actedAtUtc: string;
  comments: string | null;
}

export interface Expense {
  id: string;
  /** Customer-facing reference, e.g. "EXP-001-2026". */
  expenseNumber: string;
  expenseDate: string;
  categoryId: string;
  categoryName: string;
  amount: number;
  description: string | null;
  status: ExpenseStatus;
  paymentMethod: ExpensePaymentMethod;
  paymentReference: string | null;
  paymentDate: string | null;
  isPaid: boolean;
  recordedByUserId: string;
  recordedByName: string;
  createdAtUtc: string;
  approvedByUserId: string | null;
  approvedByName: string | null;
  approvedAtUtc: string | null;
  approvalComments: string | null;
  isRecurring: boolean;
  /** False once approved or rejected — an approved expense is frozen (EXP-036). */
  isEditable: boolean;
  attachments: ExpenseAttachment[];
  approvalTrail: ExpenseApprovalEntry[];
}

export interface ExpenseSummary {
  id: string;
  expenseNumber: string;
  expenseDate: string;
  categoryId: string;
  categoryName: string;
  amount: number;
  description: string | null;
  status: ExpenseStatus;
  paymentMethod: ExpensePaymentMethod;
  paymentReference: string | null;
  isPaid: boolean;
  isRecurring: boolean;
  attachmentCount: number;
  recordedByName: string;
}

export interface ExpensePayload {
  expenseDate: string;
  categoryId: string;
  amount: number;
  description: string | null;
  paymentMethod: ExpensePaymentMethod;
  paymentReference: string | null;
  paymentDate: string | null;
  isPaid: boolean;
  submitForApproval?: boolean;
}

export interface ExpenseFilters {
  from?: string;
  to?: string;
  categoryId?: string;
  paymentMethod?: ExpensePaymentMethod;
  status?: ExpenseStatus;
  isPaid?: boolean;
  search?: string;
}

export interface RecurringExpense {
  id: string;
  categoryId: string;
  categoryName: string;
  amount: number;
  description: string | null;
  paymentMethod: ExpensePaymentMethod;
  dayOfMonth: number;
  isActive: boolean;
  lastGeneratedYear: number | null;
  lastGeneratedMonth: number | null;
}

export interface RecurringExpensePayload {
  categoryId: string;
  amount: number;
  description: string | null;
  paymentMethod: ExpensePaymentMethod;
  dayOfMonth: number;
}

export interface CategoryBreakdown {
  categoryId: string;
  categoryName: string;
  total: number;
  /** Share of the period's expenses, 0-100. */
  percentageOfTotal: number;
  expenseCount: number;
  monthlyBudget: number | null;
  budgetUsedPercentage: number | null;
}

export interface DailyFigure {
  date: string;
  revenue: number;
  expenses: number;
  profit: number;
}

export interface WeeklyFigure {
  weekNumber: number;
  startDate: string;
  endDate: string;
  revenue: number;
  expenses: number;
  profit: number;
}

/** Revenue against expenses. Percentages are null on a day with no takings to divide by. */
export interface ProfitSummary {
  revenue: number;
  expenses: number;
  profit: number;
  expenseRatio: number | null;
  profitMargin: number | null;
}

export interface PeriodComparison {
  previousLabel: string;
  previousTotal: number;
  currentTotal: number;
  change: number;
  changePercentage: number | null;
  largestIncreaseCategory: string | null;
  largestIncreaseAmount: number | null;
}

export interface BudgetAlert {
  categoryId: string;
  categoryName: string;
  monthlyBudget: number;
  spentThisMonth: number;
  remaining: number;
  usedPercentage: number;
  isOverBudget: boolean;
}

export interface DailyExpenseReport {
  date: string;
  summary: ProfitSummary;
  categories: CategoryBreakdown[];
  comparison: PeriodComparison;
  expenses: ExpenseSummary[];
  highestCategory: string | null;
  lowestCategory: string | null;
}

export interface MonthlyExpenseReport {
  year: number;
  month: number;
  monthLabel: string;
  summary: ProfitSummary;
  categories: CategoryBreakdown[];
  dailyFigures: DailyFigure[];
  weeklyFigures: WeeklyFigure[];
  comparison: PeriodComparison;
  budgetAlerts: BudgetAlert[];
}

export interface ExpenseRangeReport {
  from: string;
  to: string;
  summary: ProfitSummary;
  categories: CategoryBreakdown[];
  dailyFigures: DailyFigure[];
}
