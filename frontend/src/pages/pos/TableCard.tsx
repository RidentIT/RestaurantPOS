import { ChefHat, CircleCheck, Clock, CreditCard, Pencil, UserRound, UtensilsCrossed } from "lucide-react";
import type { RestaurantTable, TableDisplayStatus } from "@/entities/table";
import { tableDisplayStatus } from "@/entities/table";
import { Badge } from "@/shared/ui";
import { cn } from "@/shared/lib/utils";

/**
 * How each state paints the tile. Colour is doing real work on this screen: the cashier reads
 * the room at a glance from across the till, so "food is ready to carry out" has to be findable
 * without reading any words.
 */
const STATUS_STYLES: Record<
  TableDisplayStatus,
  { label: string; badge: "default" | "secondary" | "success" | "warning" | "destructive" | "outline"; card: string; icon: React.ReactNode }
> = {
  Available: {
    label: "Available",
    badge: "outline",
    card: "border-dashed hover:border-primary/50 hover:bg-primary/5",
    icon: null,
  },
  Draft: {
    label: "Taking order",
    badge: "secondary",
    card: "border-muted-foreground/40 bg-muted/40",
    icon: <Pencil className="size-3.5" />,
  },
  Ordered: {
    label: "Sent to kitchen",
    badge: "default",
    card: "border-primary/40 bg-primary/5",
    icon: <Clock className="size-3.5" />,
  },
  Preparing: {
    label: "Preparing",
    badge: "warning",
    card: "border-warning/50 bg-warning/5",
    icon: <ChefHat className="size-3.5" />,
  },
  Ready: {
    label: "Ready to serve",
    badge: "success",
    card: "border-success/60 bg-success/10",
    icon: <CircleCheck className="size-3.5" />,
  },
  Served: {
    label: "Served",
    badge: "secondary",
    card: "border-success/30 bg-success/5",
    icon: <UtensilsCrossed className="size-3.5" />,
  },
  Checkout: {
    label: "Paying",
    badge: "warning",
    card: "border-warning/60 bg-warning/10",
    icon: <CreditCard className="size-3.5" />,
  },
};

export function TableCard({
  table,
  onOpen,
  busy,
}: {
  table: RestaurantTable;
  onOpen: (table: RestaurantTable) => void;
  busy: boolean;
}) {
  const status = tableDisplayStatus(table);
  const style = STATUS_STYLES[status];
  const order = table.currentOrder;

  return (
    <button
      type="button"
      onClick={() => onOpen(table)}
      disabled={busy || !table.isActive}
      aria-label={`Table ${table.number} — ${style.label}`}
      className={cn(
        "flex min-h-36 flex-col rounded-xl border-2 p-4 text-left transition-all",
        "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2",
        "disabled:cursor-not-allowed disabled:opacity-50",
        style.card,
        !table.isActive && "opacity-50",
      )}
    >
      <div className="flex items-start justify-between gap-2">
        <span className="text-2xl font-semibold tabular">{table.number}</span>
        <Badge variant={style.badge} className="gap-1">
          {style.icon}
          {style.label}
        </Badge>
      </div>

      {order ? (
        <div className="mt-auto space-y-0.5 pt-3">
          <p className="text-sm font-medium">
            {order.orderNumber ? `Order #${String(order.orderNumber).padStart(3, "0")}` : "Not confirmed"}
          </p>
          <p className="text-sm text-muted-foreground">
            {order.itemCount} item{order.itemCount === 1 ? "" : "s"} · {order.total.toFixed(2)}
          </p>
          {order.stewardName && (
            <p className="flex items-center gap-1 truncate text-xs text-muted-foreground">
              <UserRound className="size-3 shrink-0" />
              {order.stewardName}
            </p>
          )}
        </div>
      ) : (
        <div className="mt-auto pt-3 text-sm text-muted-foreground">
          {table.isActive ? (
            <>
              {table.seats > 0 && <span>{table.seats} seats · </span>}
              Tap to start an order
            </>
          ) : (
            "Out of service"
          )}
        </div>
      )}
    </button>
  );
}

/** A compact legend, so a new cashier can learn the colours without being told. */
export function TableStatusLegend() {
  const shown: TableDisplayStatus[] = ["Available", "Ordered", "Preparing", "Ready", "Checkout"];

  return (
    <div className="flex flex-wrap items-center gap-2">
      {shown.map((status) => (
        <Badge key={status} variant={STATUS_STYLES[status].badge} className="gap-1">
          {STATUS_STYLES[status].icon}
          {STATUS_STYLES[status].label}
        </Badge>
      ))}
    </div>
  );
}

export { STATUS_STYLES };
