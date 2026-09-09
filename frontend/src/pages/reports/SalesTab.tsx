import type { SalesReport } from "@/entities/report";
import { PAYMENT_METHOD_LABELS } from "@/entities/order";
import { ProfitSummaryCards } from "@/features/expenses";
import { RankedBarChart, ShareDonut } from "@/shared/charts/Charts";
import { Card, CardContent, CardHeader, CardTitle } from "@/shared/ui";

const money = (value: number) =>
  value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });

/** "0" → "12 AM", "13" → "1 PM" — how the till's own hours read on a chart, not a 24-hour clock. */
function hourLabel(hour: number): string {
  const period = hour < 12 ? "AM" : "PM";
  const twelve = hour % 12 === 0 ? 12 : hour % 12;
  return `${twelve} ${period}`;
}

/**
 * What sold, how it was paid for, and when — the depth Reports & Analytics adds on top of the
 * revenue-vs-expenses figures the Overview tab already covers.
 */
export function SalesTab({ report }: { report: SalesReport }) {
  const busiestHours = [...report.hourlyPattern]
    .filter((h) => h.orderCount > 0)
    .sort((a, b) => b.revenue - a.revenue)
    .slice(0, 8);

  const busiestDays = [...report.dayOfWeekPattern].sort((a, b) => b.revenue - a.revenue);

  return (
    <div className="space-y-4">
      <ProfitSummaryCards summary={report.summary} />

      <div className="grid gap-4 lg:grid-cols-2">
        <Card className="p-5">
          <h2 className="mb-4 font-semibold">Best sellers</h2>
          <RankedBarChart
            emptyLabel="Nothing sold in this period yet."
            data={report.topItems.slice(0, 10).map((item) => ({
              id: item.menuItemVariantId,
              label: item.name,
              value: item.revenue,
              detail: `${item.quantitySold} sold`,
            }))}
          />
        </Card>

        <Card className="p-5">
          <h2 className="mb-4 font-semibold">Sales by category</h2>
          <ShareDonut
            emptyLabel="Nothing sold in this period yet."
            slices={report.categories.map((c) => ({
              id: c.category,
              label: c.category,
              value: c.revenue,
              percentageOfTotal: c.percentageOfTotal,
            }))}
          />
        </Card>
      </div>

      <div className="grid gap-4 lg:grid-cols-2">
        <Card className="p-5">
          <h2 className="mb-4 font-semibold">How customers paid</h2>
          <ShareDonut
            emptyLabel="No payments recorded in this period yet."
            slices={report.paymentMethods.map((p) => ({
              id: p.method,
              label: PAYMENT_METHOD_LABELS[p.method],
              value: p.amount,
              percentageOfTotal: p.percentageOfTotal,
            }))}
          />
        </Card>

        <Card className="p-5">
          <h2 className="mb-4 font-semibold">Busiest hours</h2>
          <RankedBarChart
            emptyLabel="No completed orders in this period yet."
            data={busiestHours.map((h) => ({
              id: String(h.hour),
              label: hourLabel(h.hour),
              value: h.revenue,
              detail: `${h.orderCount} order${h.orderCount === 1 ? "" : "s"}`,
            }))}
          />
        </Card>
      </div>

      <div className="grid gap-4 lg:grid-cols-2">
        <Card className="p-5">
          <h2 className="mb-4 font-semibold">Busiest days of the week</h2>
          <RankedBarChart
            emptyLabel="No completed orders in this period yet."
            data={busiestDays.map((d) => ({
              id: d.day,
              label: d.day,
              value: d.revenue,
              detail: `${d.orderCount} order${d.orderCount === 1 ? "" : "s"}`,
            }))}
          />
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Discounts</CardTitle>
          </CardHeader>
          <CardContent className="grid grid-cols-2 gap-4">
            <div>
              <p className="text-sm text-muted-foreground">Given away</p>
              <p className="mt-1 text-2xl font-semibold tabular">{money(report.discounts.totalDiscountGiven)}</p>
            </div>
            <div>
              <p className="text-sm text-muted-foreground">Orders discounted</p>
              <p className="mt-1 text-2xl font-semibold tabular">
                {report.discounts.ordersWithDiscount}
                <span className="text-base font-normal text-muted-foreground">
                  {" "}
                  / {report.discounts.totalOrders}
                </span>
              </p>
              <p className="mt-1 text-xs text-muted-foreground">
                {report.discounts.percentageOfOrdersDiscounted.toFixed(1)}% of orders
              </p>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
