import { TriangleAlert } from "lucide-react";
import { PeriodComparisonCard, ProfitSummaryCards } from "@/features/expenses";
import type { useDailyExpenseReport, useMonthlyExpenseReport } from "@/features/expenses";
import { ShareDonut, TrendChart, TrendLegend } from "@/shared/charts/Charts";
import { Alert, AlertDescription, AlertTitle, Card, LoadingState, Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/shared/ui";
import { cn } from "@/shared/lib/utils";

const money = (value: number) =>
  value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });

/**
 * The revenue-vs-expenses P&L — Reports & Analytics' own catalog entry promises this, so it reads
 * from the exact same endpoints the Expenses module's reports do rather than a copy of the maths.
 */
export function OverviewDaily({ report }: { report: NonNullable<ReturnType<typeof useDailyExpenseReport>["data"]> }) {
  return (
    <div className="space-y-4">
      <ProfitSummaryCards summary={report.summary} />

      <div className="grid gap-4 lg:grid-cols-2">
        <Card className="p-5">
          <h2 className="mb-4 font-semibold">Where the money went</h2>
          <ShareDonut
            emptyLabel="No approved expenses in this period yet."
            slices={report.categories.map((c) => ({
              id: c.categoryId,
              label: c.categoryName,
              value: c.total,
              percentageOfTotal: c.percentageOfTotal,
            }))}
          />
        </Card>

        <PeriodComparisonCard comparison={report.comparison} />
      </div>
    </div>
  );
}

export function OverviewMonthly({
  report,
}: {
  report: NonNullable<ReturnType<typeof useMonthlyExpenseReport>["data"]>;
}) {
  return (
    <div className="space-y-4">
      <ProfitSummaryCards summary={report.summary} />

      {report.budgetAlerts.length > 0 && (
        <div className="space-y-2">
          {report.budgetAlerts.map((alert) => (
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
          <h2 className="font-semibold">Daily trend — {report.monthLabel}</h2>
          <TrendLegend primaryLabel="Sales" secondaryLabel="Expenses" />
        </div>
        <TrendChart
          points={report.dailyFigures.map((f) => ({ label: f.date.slice(8), primary: f.revenue, secondary: f.expenses }))}
        />
      </Card>

      <div className="grid gap-4 lg:grid-cols-2">
        <Card className="p-5">
          <h2 className="mb-4 font-semibold">Category breakdown</h2>
          <ShareDonut
            emptyLabel="No approved expenses in this period yet."
            slices={report.categories.map((c) => ({
              id: c.categoryId,
              label: c.categoryName,
              value: c.total,
              percentageOfTotal: c.percentageOfTotal,
            }))}
          />
        </Card>

        <div className="space-y-4">
          <PeriodComparisonCard comparison={report.comparison} />

          <Card>
            <div className="p-4 pb-0">
              <h2 className="font-semibold">Weekly breakdown</h2>
            </div>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Week</TableHead>
                  <TableHead className="text-right">Sales</TableHead>
                  <TableHead className="text-right">Expenses</TableHead>
                  <TableHead className="text-right">Profit</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {report.weeklyFigures.map((week) => (
                  <TableRow key={week.weekNumber}>
                    <TableCell className="whitespace-nowrap">
                      <span className="font-medium">Week {week.weekNumber}</span>
                      <span className="block text-xs text-muted-foreground">
                        {week.startDate.slice(5)} – {week.endDate.slice(5)}
                      </span>
                    </TableCell>
                    <TableCell className="text-right tabular">{money(week.revenue)}</TableCell>
                    <TableCell className="text-right tabular">{money(week.expenses)}</TableCell>
                    <TableCell className={cn("text-right tabular font-medium", week.profit < 0 && "text-destructive")}>
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
  );
}

export function OverviewLoading() {
  return <LoadingState label="Loading the numbers…" />;
}
