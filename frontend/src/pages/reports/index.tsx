import { useState } from "react";
import { BarChart3, FileSpreadsheet, FileText, LineChart } from "lucide-react";
import { useDailyExpenseReport, useMonthlyExpenseReport } from "@/features/expenses";
import { exportSalesReportExcel, exportSalesReportPdf, useDailySalesReport, useMonthlySalesReport } from "@/features/reports";
import { Button, Input, Label, LoadingState, SegmentedTabs } from "@/shared/ui";
import { OverviewDaily, OverviewLoading, OverviewMonthly } from "./OverviewTab";
import { SalesTab } from "./SalesTab";

type Section = "overview" | "sales";
type Period = "daily" | "monthly";

const today = () => new Date().toISOString().slice(0, 10);

/**
 * Sales, inventory, expense and staff performance reporting — the module's own catalog entry.
 * Inventory and staff performance follow in a later pass; this covers the P&L (shared with the
 * Expenses module's own reports) and the sales analytics that are genuinely this module's own.
 */
export default function ReportsPage() {
  const [section, setSection] = useState<Section>("overview");
  const [period, setPeriod] = useState<Period>("daily");
  const [date, setDate] = useState(today());
  const [month, setMonth] = useState(() => today().slice(0, 7));
  const [year, monthNumber] = month.split("-").map(Number);

  const dailyOverview = useDailyExpenseReport(date);
  const monthlyOverview = useMonthlyExpenseReport(year, monthNumber);
  const dailySales = useDailySalesReport(date);
  const monthlySales = useMonthlySalesReport(year, monthNumber);

  const salesReport = period === "daily" ? dailySales.data : monthlySales.data;
  const salesLoading = period === "daily" ? dailySales.isLoading : monthlySales.isLoading;

  return (
    <div className="space-y-6 p-8">
      <div>
        <h1 className="text-2xl font-semibold">Reports &amp; Analytics</h1>
        <p className="text-sm text-muted-foreground">
          Sales against expenses, and what's actually behind the sales figure.
        </p>
      </div>

      <div className="flex flex-wrap items-end justify-between gap-4">
        <div className="space-y-3">
          <SegmentedTabs
            tabs={[
              { value: "overview", label: "Overview", icon: <LineChart className="size-4" /> },
              { value: "sales", label: "Sales", icon: <BarChart3 className="size-4" /> },
            ]}
            value={section}
            onValueChange={(value) => setSection(value as Section)}
          />
          <SegmentedTabs
            tabs={[
              { value: "daily", label: "Daily" },
              { value: "monthly", label: "Monthly" },
            ]}
            value={period}
            onValueChange={(value) => setPeriod(value as Period)}
          />
        </div>

        <div className="flex flex-wrap items-end gap-2">
          {period === "daily" ? (
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

          {section === "sales" && (
            <>
              <Button variant="outline" disabled={!salesReport} onClick={() => salesReport && exportSalesReportPdf(salesReport)}>
                <FileText /> PDF
              </Button>
              <Button
                variant="outline"
                disabled={!salesReport}
                onClick={() => salesReport && exportSalesReportExcel(salesReport)}
              >
                <FileSpreadsheet /> Excel
              </Button>
            </>
          )}
        </div>
      </div>

      {section === "overview" &&
        (period === "daily" ? (
          dailyOverview.isLoading || !dailyOverview.data ? (
            <OverviewLoading />
          ) : (
            <OverviewDaily report={dailyOverview.data} />
          )
        ) : monthlyOverview.isLoading || !monthlyOverview.data ? (
          <OverviewLoading />
        ) : (
          <OverviewMonthly report={monthlyOverview.data} />
        ))}

      {section === "sales" &&
        (salesLoading || !salesReport ? (
          <LoadingState label="Loading the numbers…" />
        ) : (
          <SalesTab report={salesReport} />
        ))}
    </div>
  );
}
