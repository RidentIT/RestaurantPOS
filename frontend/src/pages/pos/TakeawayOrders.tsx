import type { OrderSummary } from "@/entities/order";
import { orderDisplayStatus } from "@/entities/order";
import { Badge } from "@/shared/ui";
import { cn } from "@/shared/lib/utils";
import { STATUS_STYLES } from "./TableCard";

/**
 * Takeaway orders never sit on the floor plan — there's no table for one to occupy — so this is
 * the only place a cashier can find one again once it's no longer the screen they're looking at.
 * Same tile language as a table card, just keyed by order rather than by seat.
 */
export function TakeawayOrders({
  orders,
  onOpen,
}: {
  orders: OrderSummary[];
  onOpen: (order: OrderSummary) => void;
}) {
  if (orders.length === 0) {
    return null;
  }

  return (
    <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5">
      {orders.map((order) => {
        const style = STATUS_STYLES[orderDisplayStatus(order)];

        return (
          <button
            key={order.id}
            type="button"
            onClick={() => onOpen(order)}
            aria-label={`Takeaway order ${order.orderNumber ?? "not confirmed"} — ${style.label}`}
            className={cn(
              "flex min-h-36 flex-col rounded-xl border-2 p-4 text-left transition-all",
              "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2",
              style.card,
            )}
          >
            <div className="flex items-start justify-between gap-2">
              <span className="text-lg font-semibold">Takeaway</span>
              <Badge variant={style.badge} className="gap-1">
                {style.icon}
                {style.label}
              </Badge>
            </div>

            <div className="mt-auto space-y-0.5 pt-3">
              <p className="text-sm font-medium">
                {order.orderNumber ? `Order #${String(order.orderNumber).padStart(3, "0")}` : "Not confirmed"}
              </p>
              <p className="text-sm text-muted-foreground">
                {order.itemCount} item{order.itemCount === 1 ? "" : "s"} · {order.total.toFixed(2)}
              </p>
            </div>
          </button>
        );
      })}
    </div>
  );
}
