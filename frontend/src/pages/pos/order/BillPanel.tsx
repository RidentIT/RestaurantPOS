import { Minus, Plus, Trash2 } from "lucide-react";
import type { Order, OrderItem } from "@/entities/order";
import { Badge, Button, EmptyState, Separator } from "@/shared/ui";
import { cn } from "@/shared/lib/utils";

/**
 * The bill as the customer would read it (POS-037).
 *
 * Voided lines stay visible, struck through, rather than disappearing: the cashier needs to see
 * that the sandwich was taken off, not wonder whether they ever keyed it in.
 */
export function BillPanel({
  order,
  onChangeQuantity,
  onVoid,
  busy,
}: {
  order: Order;
  onChangeQuantity: (item: OrderItem, quantity: number) => void;
  onVoid: (item: OrderItem) => void;
  busy: boolean;
}) {
  const editable = order.status === "Draft" || order.status === "Open";

  if (order.items.length === 0) {
    return (
      <EmptyState
        title="Nothing on this bill yet"
        description="Pick items from the menu to add them."
      />
    );
  }

  return (
    <div className="space-y-3">
      <ul className="space-y-2">
        {order.items.map((item) => (
          <li
            key={item.id}
            className={cn(
              "rounded-lg border p-3",
              item.isCancelled && "border-dashed bg-muted/40 opacity-70",
            )}
          >
            <div className="flex items-start justify-between gap-3">
              <div className="min-w-0 flex-1">
                <p className={cn("font-medium", item.isCancelled && "line-through")}>
                  {item.menuItemName}
                </p>
                <p className="text-sm text-muted-foreground tabular">
                  {item.quantity} × {item.unitPrice.toFixed(2)}
                </p>
                {item.specialInstructions && (
                  <p className="mt-1 text-sm italic text-muted-foreground">
                    {item.specialInstructions}
                  </p>
                )}
                {item.isCancelled && (
                  <Badge variant="destructive" className="mt-1">
                    Voided
                  </Badge>
                )}
              </div>

              <div className="flex shrink-0 items-center gap-2">
                <span className={cn("w-20 text-right font-medium tabular", item.isCancelled && "line-through")}>
                  {(item.unitPrice * item.quantity).toFixed(2)}
                </span>
              </div>
            </div>

            {editable && !item.isCancelled && (
              <div className="mt-2 flex items-center gap-1">
                <Button
                  variant="outline"
                  size="icon"
                  className="size-8"
                  disabled={busy || item.quantity <= 1}
                  aria-label={`Decrease ${item.menuItemName}`}
                  onClick={() => onChangeQuantity(item, item.quantity - 1)}
                >
                  <Minus className="size-3.5" />
                </Button>
                <span className="w-8 text-center text-sm tabular">{item.quantity}</span>
                <Button
                  variant="outline"
                  size="icon"
                  className="size-8"
                  disabled={busy}
                  aria-label={`Increase ${item.menuItemName}`}
                  onClick={() => onChangeQuantity(item, item.quantity + 1)}
                >
                  <Plus className="size-3.5" />
                </Button>
                <Button
                  variant="ghost"
                  size="icon"
                  className="ml-auto size-8"
                  disabled={busy}
                  aria-label={`Remove ${item.menuItemName}`}
                  onClick={() => onVoid(item)}
                >
                  <Trash2 className="size-3.5 text-destructive" />
                </Button>
              </div>
            )}
          </li>
        ))}
      </ul>

      <Separator />

      <dl className="space-y-1 text-sm">
        <div className="flex justify-between">
          <dt className="text-muted-foreground">Subtotal</dt>
          <dd className="tabular">{order.subtotal.toFixed(2)}</dd>
        </div>
        <div className="flex justify-between">
          <dt className="text-muted-foreground">
            Discount
            {order.discountType === "Percentage" && order.discountValue > 0 && ` (${order.discountValue}%)`}
          </dt>
          <dd className="tabular">−{order.discountAmount.toFixed(2)}</dd>
        </div>
        {order.serviceChargeAmount > 0 && (
          <div className="flex justify-between">
            <dt className="text-muted-foreground">Service charge ({order.serviceChargeRatePercent}%)</dt>
            <dd className="tabular">{order.serviceChargeAmount.toFixed(2)}</dd>
          </div>
        )}
        {order.taxAmount > 0 && (
          <div className="flex justify-between">
            <dt className="text-muted-foreground">Tax / VAT ({order.taxRatePercent}%)</dt>
            <dd className="tabular">{order.taxAmount.toFixed(2)}</dd>
          </div>
        )}
        <div className="flex justify-between pt-1 text-lg font-semibold">
          <dt>Total</dt>
          <dd className="tabular">{order.total.toFixed(2)}</dd>
        </div>
      </dl>
    </div>
  );
}
