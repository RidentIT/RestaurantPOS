import { TrendingDown, TrendingUp } from "lucide-react";
import type { PeriodComparison, ProfitSummary } from "@/entities/expense";
import { Badge, Card } from "@/shared/ui";
import { cn } from "@/shared/lib/utils";

const money = (value: number) =>
  value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });

/**
 * The headline figures every revenue-vs-expenses report leads with (EXP-026, EXP-027) — shared by
 * the Expenses module's own reports and by Reports & Analytics' Overview tab, which read the exact
 * same numbers from the same endpoint.
 */
export function ProfitSummaryCards({ summary }: { summary: ProfitSummary }) {
  const cards = [
    { label: "Sales", value: money(summary.revenue), hint: "Settled bills at the till" },
    { label: "Expenses", value: money(summary.expenses), hint: "Approved expenses only" },
    {
      label: "Profit",
      value: money(summary.profit),
      hint: summary.profit >= 0 ? "Revenue less expenses" : "Spent more than was taken",
      negative: summary.profit < 0,
    },
    {
      label: "Avg. order",
      value: summary.averageOrderValue === null ? "—" : money(summary.averageOrderValue),
      hint: `${summary.orderCount} order${summary.orderCount === 1 ? "" : "s"} settled`,
    },
    {
      label: "Profit margin",
      value: summary.profitMargin === null ? "—" : `${summary.profitMargin.toFixed(1)}%`,
      hint: summary.profitMargin === null ? "No takings to compare against" : "Share of revenue kept",
    },
  ];

  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-5">
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
export function PeriodComparisonCard({ comparison }: { comparison: PeriodComparison }) {
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
