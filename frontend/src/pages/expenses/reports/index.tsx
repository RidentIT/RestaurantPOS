import { useState } from "react";
import { Link } from "react-router-dom";
import { ArrowLeft, FileSpreadsheet, FileText, TrendingDown, TrendingUp, TriangleAlert } from "lucide-react";
import type { PeriodComparison, ProfitSummary } from "@/entities/expense";
import {
  exportDailyReportExcel,
  exportDailyReportPdf,
  exportMonthlyReportExcel,
  exportMonthlyReportPdf,
  useDailyExpenseReport,
  useMonthlyExpenseReport,
} from "@/features/expenses";
import {
  Alert,
  AlertDescription,
  AlertTitle,
  Badge,
  Button,
  Card,
  Input,
  Label,
  LoadingState,
  SegmentedTabs,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/shared/ui";
import { cn } from "@/shared/lib/utils";
import { CategoryDonut, DailyTrendChart, TrendLegend } from "../Charts";

type Tab = "daily" | "monthly";

const money = (value: number) =>
  value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });

const today = () => new Date().toISOString().slice(0, 10);

/** The four headline figures every report leads with (EXP-026, EXP-027). */
function SummaryCards({ summary }: { summary: ProfitSummary }) {
  const cards = [
    { label: "Revenue", value: money(summary.revenue), hint: "Settled bills at the till" },
    { label: "Expenses", value: money(summary.expenses), hint: "Approved expenses only" },
    {
      label: "Profit",
      value: money(summary.profit),
      hint: summary.profit >= 0 ? "Revenue less expenses" : "Spent more than was taken",
      negative: summary.profit < 0,
    },
    {
      label: "Profit margin",
      value: summary.profitMargin === null ? "—" : `${summary.profitMargin.toFixed(1)}%`,
      hint: summary.profitMargin === null ? "No takings to compare against" : "Share of revenue kept",
    },
  ];

  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
      {cards.map((card) => (
        <Card key={card.label} className="p-4">
          <p className="text-sm text-muted-foreground">{card.label}</p>
          <p className={cn("mt-1 text-2xl font-semibold tabular", card.negative && "text-destructive")}>
            {card.value}
          </p>
          <p className="mt-1 text-xs text-muted-foreground">{card.hint}</p>
        </Card>
      ))}
    </div>
  );
}

/** How the period compares with the one before it (EXP-025). */
function ComparisonCard({ comparison }: { comparison: PeriodComparison }) {
  const up = comparison.change > 0;
  const flat = comparison.change === 0;

  return (
    <Card className="space-y-2 p-4">
      <p className="text-sm font-medium">Compared with {comparison.previousLabel}</p>

      <div className="flex flex-wrap items-baseline gap-2">
        <span className="tabular text-muted-foreground">{money(comparison.previousTotal)}</span>
        <span className="text-muted-foreground">→</span>
        <span className="text-xl font-semibold tabular">{money(comparison.currentTotal)}</span>

        {!flat && (
          <Badge variant={up ? "destructive" : "success"} className="gap-1">
            {up ? <TrendingUp className="size-3.5" /> : <TrendingDown className="size-3.5" />}
            {up ? "+" : ""}
            {money(comparison.change)}
            {comparison.changePercentage !== null &&
              ` (${up ? "+" : ""}${comparison.changePercentage.toFixed(1)}%)`}
          </Badge>
        )}
      </div>

      {comparison.largestIncreaseCategory && (
        <p className="text-sm text-muted-foreground">
          Largest increase: <span className="font-medium">{comparison.largestIncreaseCategory}</span>{" "}
          (+{money(comparison.largestIncreaseAmount ?? 0)})
        </p>
      )}
    </Card>
  );
}

/**
 * Expense reporting: a day at a time or a month at a time, each exportable as a real PDF or
 * spreadsheet the owner can keep or email (EXP-028 to EXP-033).
 */
export default function ExpenseReportsPage() {
  const [tab, setTab] = useState<Tab>("daily");
  const [date, setDate] = useState(today());
  const [month, setMonth] = useState(() => today().slice(0, 7));

  const [year, monthNumber] = month.split("-").map(Number);

  const daily = useDailyExpenseReport(date);
  const monthly = useMonthlyExpenseReport(year, monthNumber);

  return (
    <div className="space-y-6 p-8">
      <div className="space-y-1">
        <Button variant="ghost" size="sm" asChild className="-ml-2">
          <Link to="/expenses">
            <ArrowLeft /> Back to expenses
          </Link>
        </Button>
        <h1 className="text-2xl font-semibold">Expense reports</h1>
        <p className="text-sm text-muted-foreground">
          Revenue comes from settled bills; only approved expenses are counted.
        </p>
      </div>

      <div className="flex flex-wrap items-end justify-between gap-4">
        <SegmentedTabs
          tabs={[
            { value: "daily", label: "Daily" },
            { value: "monthly", label: "Monthly" },
          ]}
          value={tab}
          onValueChange={(value) => setTab(value as Tab)}
        />

        <div className="flex flex-wrap items-end gap-2">
          {tab === "daily" ? (
            <div className="space-y-1.5">
              <Label htmlFor="report-date">Date</Label>
              <Input
                id="report-date"
                type="date"
                value={date}
                max={today()}
                onChange={(event) => setDate(event.target.value)}
                className="w-44"
              />
            </div>
          ) : (
            <div className="space-y-1.5">
              <Label htmlFor="report-month">Month</Label>
              <Input
                id="report-month"
                type="month"
                value={month}
                onChange={(event) => setMonth(event.target.value)}
                className="w-44"
              />
            </div>
          )}

          <Button
            variant="outline"
            disabled={tab === "daily" ? !daily.data : !monthly.data}
            onClick={() =>
              tab === "daily"
                ? daily.data && exportDailyReportPdf(daily.data)
                : monthly.data && exportMonthlyReportPdf(monthly.data)
            }
          >
            <FileText /> PDF
          </Button>

          <Button
            variant="outline"
            disabled={tab === "daily" ? !daily.data : !monthly.data}
            onClick={() =>
              tab === "daily"
                ? daily.data && exportDailyReportExcel(daily.data)
                : monthly.data && exportMonthlyReportExcel(monthly.data)
            }
          >
            <FileSpreadsheet /> Excel
          </Button>
        </div>
      </div>

      {tab === "daily" &&
        (daily.isLoading || !daily.data ? (
          <LoadingState label="Loading the day…" />
        ) : (
          <div className="space-y-4">
            <SummaryCards summary={daily.data.summary} />

            <div className="grid gap-4 lg:grid-cols-2">
              <Card className="p-5">
                <h2 className="mb-4 font-semibold">Where the money went</h2>
                <CategoryDonut categories={daily.data.categories} />
              </Card>

              <div className="space-y-4">
                <ComparisonCard comparison={daily.data.comparison} />

                {daily.data.highestCategory && (
                  <Card className="space-y-1 p-4 text-sm">
                    <p>
                      <span className="text-muted-foreground">Highest:</span>{" "}
                      <span className="font-medium">{daily.data.highestCategory}</span>
                    </p>
                    <p>
                      <span className="text-muted-foreground">Lowest:</span>{" "}
                      <span className="font-medium">{daily.data.lowestCategory}</span>
                    </p>
                  </Card>
                )}
              </div>
            </div>

            <Card>
              <div className="p-4 pb-0">
                <h2 className="font-semibold">Expenses on {daily.data.date}</h2>
                <p className="text-sm text-muted-foreground">
                  Everything recorded, including anything still awaiting a decision.
                </p>
              </div>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Number</TableHead>
                    <TableHead>Category</TableHead>
                    <TableHead>Description</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead className="text-right">Amount</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {daily.data.expenses.map((expense) => (
                    <TableRow key={expense.id}>
                      <TableCell className="whitespace-nowrap font-medium">{expense.expenseNumber}</TableCell>
                      <TableCell>{expense.categoryName}</TableCell>
                      <TableCell className="max-w-64 truncate text-sm text-muted-foreground">
                        {expense.description ?? "—"}
                      </TableCell>
                      <TableCell>
                        <Badge variant={expense.status === "Approved" ? "success" : "outline"}>
                          {expense.status}
                        </Badge>
                      </TableCell>
                      <TableCell className="text-right tabular">{money(expense.amount)}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </Card>
          </div>
        ))}

      {tab === "monthly" &&
        (monthly.isLoading || !monthly.data ? (
          <LoadingState label="Loading the month…" />
        ) : (
          <div className="space-y-4">
            <SummaryCards summary={monthly.data.summary} />

            {monthly.data.budgetAlerts.length > 0 && (
              <div className="space-y-2">
                {monthly.data.budgetAlerts.map((alert) => (
                  <Alert key={alert.categoryId} variant={alert.isOverBudget ? "destructive" : "warning"}>
                    <TriangleAlert className="size-4" />
                    <div>
                      <AlertTitle>
                        {alert.categoryName} · {alert.usedPercentage.toFixed(1)}% of budget
                      </AlertTitle>
                      <AlertDescription>
                        {money(alert.spentThisMonth)} of {money(alert.monthlyBudget)} —{" "}
                        {alert.isOverBudget
                          ? `over by ${money(Math.abs(alert.remaining))}`
                          : `${money(alert.remaining)} left`}
                      </AlertDescription>
                    </div>
                  </Alert>
                ))}
              </div>
            )}

            <Card className="p-5">
              <div className="mb-4 flex flex-wrap items-center justify-between gap-2">
                <h2 className="font-semibold">Daily trend — {monthly.data.monthLabel}</h2>
                <TrendLegend />
              </div>
              <DailyTrendChart figures={monthly.data.dailyFigures} />
            </Card>

            <div className="grid gap-4 lg:grid-cols-2">
              <Card className="p-5">
                <h2 className="mb-4 font-semibold">Category breakdown</h2>
                <CategoryDonut categories={monthly.data.categories} />
              </Card>

              <div className="space-y-4">
                <ComparisonCard comparison={monthly.data.comparison} />

                <Card>
                  <div className="p-4 pb-0">
                    <h2 className="font-semibold">Weekly breakdown</h2>
                  </div>
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>Week</TableHead>
                        <TableHead className="text-right">Revenue</TableHead>
                        <TableHead className="text-right">Expenses</TableHead>
                        <TableHead className="text-right">Profit</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {monthly.data.weeklyFigures.map((week) => (
                        <TableRow key={week.weekNumber}>
                          <TableCell className="whitespace-nowrap">
                            <span className="font-medium">Week {week.weekNumber}</span>
                            <span className="block text-xs text-muted-foreground">
                              {week.startDate.slice(5)} – {week.endDate.slice(5)}
                            </span>
                          </TableCell>
                          <TableCell className="text-right tabular">{money(week.revenue)}</TableCell>
                          <TableCell className="text-right tabular">{money(week.expenses)}</TableCell>
                          <TableCell
                            className={cn(
                              "text-right tabular font-medium",
                              week.profit < 0 && "text-destructive",
                            )}
                          >
                            {money(week.profit)}
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </Card>
              </div>
            </div>
          </div>
        ))}
    </div>
  );
}
