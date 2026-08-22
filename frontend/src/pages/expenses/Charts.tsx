import { useId } from "react";
import type { CategoryBreakdown, DailyFigure } from "@/entities/expense";

/**
 * Small SVG charts, hand-rolled rather than pulled from a charting library.
 *
 * The shapes needed here are a donut and two trend lines over at most 31 points — a few dozen
 * lines of SVG against roughly half a megabyte of dependency, in an app that ships to a single
 * restaurant PC. They also inherit the app's own colours, so the charts match the rest of the
 * interface instead of bringing a second palette with them.
 */

/** Chart colours, walked in order. Chosen to stay distinguishable in the light and dark themes. */
const SERIES_COLOURS = [
  "hsl(168 62% 28%)",
  "hsl(28 88% 52%)",
  "hsl(214 72% 50%)",
  "hsl(340 65% 52%)",
  "hsl(268 55% 55%)",
  "hsl(48 90% 45%)",
  "hsl(190 65% 42%)",
  "hsl(0 65% 52%)",
];

export const seriesColour = (index: number) => SERIES_COLOURS[index % SERIES_COLOURS.length];

const money = (value: number) =>
  value.toLocaleString(undefined, { minimumFractionDigits: 0, maximumFractionDigits: 0 });

/** Where a slice's arc lands on the circle, given its share of the whole. */
function arcPath(cx: number, cy: number, radius: number, inner: number, from: number, to: number): string {
  const start = (from - 90) * (Math.PI / 180);
  const end = (to - 90) * (Math.PI / 180);
  const large = to - from > 180 ? 1 : 0;

  const x1 = cx + radius * Math.cos(start);
  const y1 = cy + radius * Math.sin(start);
  const x2 = cx + radius * Math.cos(end);
  const y2 = cy + radius * Math.sin(end);

  const ix1 = cx + inner * Math.cos(end);
  const iy1 = cy + inner * Math.sin(end);
  const ix2 = cx + inner * Math.cos(start);
  const iy2 = cy + inner * Math.sin(start);

  return `M ${x1} ${y1} A ${radius} ${radius} 0 ${large} 1 ${x2} ${y2} L ${ix1} ${iy1} A ${inner} ${inner} 0 ${large} 0 ${ix2} ${iy2} Z`;
}

/** Category shares as a donut, with the period's total in the middle. */
export function CategoryDonut({ categories }: { categories: CategoryBreakdown[] }) {
  const total = categories.reduce((sum, c) => sum + c.total, 0);

  if (total <= 0) {
    return (
      <p className="py-8 text-center text-sm text-muted-foreground">
        No approved expenses in this period yet.
      </p>
    );
  }

  let cursor = 0;

  return (
    <div className="flex flex-wrap items-center justify-center gap-6">
      <svg viewBox="0 0 200 200" className="size-48 shrink-0" role="img" aria-label="Expenses by category">
        {categories.map((category, index) => {
          const sweep = (category.total / total) * 360;
          const path = arcPath(100, 100, 88, 54, cursor, cursor + sweep);
          cursor += sweep;

          return (
            <path key={category.categoryId} d={path} fill={seriesColour(index)}>
              <title>{`${category.categoryName}: ${money(category.total)} (${category.percentageOfTotal.toFixed(1)}%)`}</title>
            </path>
          );
        })}
        <text x="100" y="96" textAnchor="middle" className="fill-foreground text-lg font-semibold">
          {money(total)}
        </text>
        <text x="100" y="114" textAnchor="middle" className="fill-muted-foreground text-[11px]">
          total
        </text>
      </svg>

      <ul className="min-w-48 space-y-1.5">
        {categories.map((category, index) => (
          <li key={category.categoryId} className="flex items-center gap-2 text-sm">
            <span
              aria-hidden="true"
              className="size-3 shrink-0 rounded-sm"
              style={{ backgroundColor: seriesColour(index) }}
            />
            <span className="min-w-0 flex-1 truncate">{category.categoryName}</span>
            <span className="tabular text-muted-foreground">{category.percentageOfTotal.toFixed(1)}%</span>
            <span className="w-20 text-right tabular font-medium">{money(category.total)}</span>
          </li>
        ))}
      </ul>
    </div>
  );
}

/** Revenue against expenses across a month, as two filled trend lines (EXP-033). */
export function DailyTrendChart({ figures }: { figures: DailyFigure[] }) {
  const gradientId = useId();

  if (figures.length === 0) {
    return <p className="py-8 text-center text-sm text-muted-foreground">Nothing to chart yet.</p>;
  }

  const width = 720;
  const height = 220;
  const padding = { top: 12, right: 12, bottom: 24, left: 52 };
  const plotWidth = width - padding.left - padding.right;
  const plotHeight = height - padding.top - padding.bottom;

  // Both series share one scale, or the comparison between them would be meaningless.
  const peak = Math.max(...figures.map((f) => Math.max(f.revenue, f.expenses)), 1);

  const x = (index: number) =>
    padding.left + (figures.length === 1 ? plotWidth / 2 : (index / (figures.length - 1)) * plotWidth);

  const y = (value: number) => padding.top + plotHeight - (value / peak) * plotHeight;

  const line = (pick: (figure: DailyFigure) => number) =>
    figures.map((figure, index) => `${index === 0 ? "M" : "L"} ${x(index)} ${y(pick(figure))}`).join(" ");

  const area = `${line((f) => f.expenses)} L ${x(figures.length - 1)} ${y(0)} L ${x(0)} ${y(0)} Z`;

  const gridValues = [0, 0.25, 0.5, 0.75, 1].map((fraction) => fraction * peak);

  return (
    <svg
      viewBox={`0 0 ${width} ${height}`}
      className="w-full"
      role="img"
      aria-label="Daily revenue and expenses"
    >
      <defs>
        <linearGradient id={gradientId} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor={seriesColour(1)} stopOpacity="0.25" />
          <stop offset="100%" stopColor={seriesColour(1)} stopOpacity="0" />
        </linearGradient>
      </defs>

      {gridValues.map((value) => (
        <g key={value}>
          <line
            x1={padding.left}
            x2={width - padding.right}
            y1={y(value)}
            y2={y(value)}
            className="stroke-border"
            strokeWidth="1"
          />
          <text x={padding.left - 6} y={y(value) + 4} textAnchor="end" className="fill-muted-foreground text-[10px]">
            {money(value)}
          </text>
        </g>
      ))}

      <path d={area} fill={`url(#${gradientId})`} />
      <path d={line((f) => f.revenue)} fill="none" stroke={seriesColour(0)} strokeWidth="2.5" />
      <path d={line((f) => f.expenses)} fill="none" stroke={seriesColour(1)} strokeWidth="2.5" />

      {/* Only a few day labels, or a 31-day month turns the axis into a smear. */}
      {figures.map((figure, index) =>
        index % Math.ceil(figures.length / 8) === 0 || index === figures.length - 1 ? (
          <text
            key={figure.date}
            x={x(index)}
            y={height - 6}
            textAnchor="middle"
            className="fill-muted-foreground text-[10px]"
          >
            {figure.date.slice(8)}
          </text>
        ) : null,
      )}
    </svg>
  );
}

/** The key for {@link DailyTrendChart}, kept separate so it can sit beside the heading. */
export function TrendLegend() {
  return (
    <div className="flex items-center gap-4 text-sm">
      <span className="flex items-center gap-1.5">
        <span aria-hidden="true" className="h-0.5 w-4 rounded" style={{ backgroundColor: seriesColour(0) }} />
        Revenue
      </span>
      <span className="flex items-center gap-1.5">
        <span aria-hidden="true" className="h-0.5 w-4 rounded" style={{ backgroundColor: seriesColour(1) }} />
        Expenses
      </span>
    </div>
  );
}
