import { useId } from "react";

/**
 * Small SVG charts, hand-rolled rather than pulled from a charting library.
 *
 * The shapes needed across the app's reports are a donut, a couple of trend lines and the odd
 * bar chart, none over a few dozen points — a few hundred lines of SVG against roughly half a
 * megabyte of dependency, in an app that ships to a single restaurant PC. They also inherit the
 * app's own colours, so charts match the rest of the interface instead of bringing a second
 * palette with them. Lives in `shared` rather than any one feature because both the expense
 * reports and the sales reports draw from it.
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

/** One slice of a {@link ShareDonut} — a name, its amount, and (for the legend row) its share. */
export interface DonutSlice {
  id: string;
  label: string;
  value: number;
  /** Share of the whole, 0-100. Recomputed from `value` if omitted. */
  percentageOfTotal?: number;
}

/** Any breakdown's shares as a donut, with the period's total in the middle. */
export function ShareDonut({ slices, emptyLabel }: { slices: DonutSlice[]; emptyLabel: string }) {
  const total = slices.reduce((sum, s) => sum + s.value, 0);

  if (total <= 0) {
    return <p className="py-8 text-center text-sm text-muted-foreground">{emptyLabel}</p>;
  }

  let cursor = 0;

  return (
    <div className="flex flex-wrap items-center justify-center gap-6">
      <svg viewBox="0 0 200 200" className="size-48 shrink-0" role="img" aria-label={emptyLabel}>
        {slices.map((slice, index) => {
          const sweep = (slice.value / total) * 360;
          const path = arcPath(100, 100, 88, 54, cursor, cursor + sweep);
          cursor += sweep;
          const share = slice.percentageOfTotal ?? (slice.value / total) * 100;

          return (
            <path key={slice.id} d={path} fill={seriesColour(index)}>
              <title>{`${slice.label}: ${money(slice.value)} (${share.toFixed(1)}%)`}</title>
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
        {slices.map((slice, index) => {
          const share = slice.percentageOfTotal ?? (slice.value / total) * 100;

          return (
            <li key={slice.id} className="flex items-center gap-2 text-sm">
              <span
                aria-hidden="true"
                className="size-3 shrink-0 rounded-sm"
                style={{ backgroundColor: seriesColour(index) }}
              />
              <span className="min-w-0 flex-1 truncate">{slice.label}</span>
              <span className="tabular text-muted-foreground">{share.toFixed(1)}%</span>
              <span className="w-20 text-right tabular font-medium">{money(slice.value)}</span>
            </li>
          );
        })}
      </ul>
    </div>
  );
}

/** One point on a two-series trend line — a labelled x position and each series' value. */
export interface TrendPoint {
  label: string;
  primary: number;
  secondary: number;
}

/** Two series across a period, as filled trend lines (EXP-033) sharing one scale. */
export function TrendChart({ points }: { points: TrendPoint[] }) {
  const gradientId = useId();

  if (points.length === 0) {
    return <p className="py-8 text-center text-sm text-muted-foreground">Nothing to chart yet.</p>;
  }

  const width = 720;
  const height = 220;
  const padding = { top: 12, right: 12, bottom: 24, left: 52 };
  const plotWidth = width - padding.left - padding.right;
  const plotHeight = height - padding.top - padding.bottom;

  // Both series share one scale, or the comparison between them would be meaningless.
  const peak = Math.max(...points.map((p) => Math.max(p.primary, p.secondary)), 1);

  const x = (index: number) =>
    padding.left + (points.length === 1 ? plotWidth / 2 : (index / (points.length - 1)) * plotWidth);

  const y = (value: number) => padding.top + plotHeight - (value / peak) * plotHeight;

  const line = (pick: (point: TrendPoint) => number) =>
    points.map((point, index) => `${index === 0 ? "M" : "L"} ${x(index)} ${y(pick(point))}`).join(" ");

  const area = `${line((p) => p.secondary)} L ${x(points.length - 1)} ${y(0)} L ${x(0)} ${y(0)} Z`;

  const gridValues = [0, 0.25, 0.5, 0.75, 1].map((fraction) => fraction * peak);

  return (
    <svg viewBox={`0 0 ${width} ${height}`} className="w-full" role="img" aria-label="Trend over the period">
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
      <path d={line((p) => p.primary)} fill="none" stroke={seriesColour(0)} strokeWidth="2.5" />
      <path d={line((p) => p.secondary)} fill="none" stroke={seriesColour(1)} strokeWidth="2.5" />

      {/* Only a few labels, or a 31-day month turns the axis into a smear. */}
      {points.map((point, index) =>
        index % Math.ceil(points.length / 8) === 0 || index === points.length - 1 ? (
          <text
            key={point.label}
            x={x(index)}
            y={height - 6}
            textAnchor="middle"
            className="fill-muted-foreground text-[10px]"
          >
            {point.label}
          </text>
        ) : null,
      )}
    </svg>
  );
}

/** The key for {@link TrendChart}, kept separate so it can sit beside the heading. */
export function TrendLegend({ primaryLabel, secondaryLabel }: { primaryLabel: string; secondaryLabel: string }) {
  return (
    <div className="flex items-center gap-4 text-sm">
      <span className="flex items-center gap-1.5">
        <span aria-hidden="true" className="h-0.5 w-4 rounded" style={{ backgroundColor: seriesColour(0) }} />
        {primaryLabel}
      </span>
      <span className="flex items-center gap-1.5">
        <span aria-hidden="true" className="h-0.5 w-4 rounded" style={{ backgroundColor: seriesColour(1) }} />
        {secondaryLabel}
      </span>
    </div>
  );
}

/** One bar in a {@link RankedBarChart}. */
export interface BarDatum {
  id: string;
  label: string;
  value: number;
  /** Small text after the bar, e.g. a quantity — kept separate from the money value. */
  detail?: string;
}

/**
 * A simple horizontal ranked bar chart — best sellers, busiest hours, whatever already comes
 * pre-sorted. Bars are proportional to the largest value shown, not to zero across the whole
 * chart's height, since a "top 10" list is read by relative size, not absolute position.
 */
export function RankedBarChart({ data, emptyLabel }: { data: BarDatum[]; emptyLabel: string }) {
  if (data.length === 0) {
    return <p className="py-8 text-center text-sm text-muted-foreground">{emptyLabel}</p>;
  }

  const peak = Math.max(...data.map((d) => d.value), 1);

  return (
    <ul className="space-y-2.5">
      {data.map((datum, index) => (
        <li key={datum.id} className="space-y-1">
          <div className="flex items-baseline justify-between gap-2 text-sm">
            <span className="min-w-0 flex-1 truncate font-medium">{datum.label}</span>
            <span className="shrink-0 tabular text-muted-foreground">{datum.detail}</span>
            <span className="w-20 shrink-0 text-right tabular font-medium">{money(datum.value)}</span>
          </div>
          <div className="h-1.5 overflow-hidden rounded-full bg-muted">
            <div
              className="h-full rounded-full"
              style={{ width: `${(datum.value / peak) * 100}%`, backgroundColor: seriesColour(index) }}
            />
          </div>
        </li>
      ))}
    </ul>
  );
}
