import { useState } from "react";
import { toast } from "sonner";
import type { DiscountType, Order } from "@/entities/order";
import {
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  FormField,
  Input,
} from "@/shared/ui";
import { cn } from "@/shared/lib/utils";

export interface DiscountDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  order: Order;
  onApply: (type: DiscountType, value: number) => Promise<void>;
  pending: boolean;
}

const TYPES: { value: DiscountType; label: string }[] = [
  { value: "None", label: "No discount" },
  { value: "Percentage", label: "Percentage" },
  { value: "Fixed", label: "Fixed amount" },
];

/** Applies money off the bill (POS-007), never more than the bill itself (BR-POS-010). */
export function DiscountDialog({ open, onOpenChange, order, onApply, pending }: DiscountDialogProps) {
  const [type, setType] = useState<DiscountType>(order.discountType);
  const [value, setValue] = useState(order.discountValue ? String(order.discountValue) : "");

  const parsed = Number(value);
  const preview =
    type === "Percentage"
      ? (order.subtotal * (Number.isFinite(parsed) ? parsed : 0)) / 100
      : Math.min(Number.isFinite(parsed) ? parsed : 0, order.subtotal);

  const submit = async () => {
    if (type === "None") {
      await onApply("None", 0);
      return;
    }

    if (!Number.isFinite(parsed) || parsed < 0) {
      toast.error("Enter a discount of zero or more.");
      return;
    }

    if (type === "Percentage" && parsed > 100) {
      toast.error("A percentage discount cannot exceed 100%.");
      return;
    }

    if (type === "Fixed" && parsed > order.subtotal) {
      toast.error(`A discount cannot exceed the subtotal of ${order.subtotal.toFixed(2)}.`);
      return;
    }

    await onApply(type, parsed);
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-sm">
        <DialogHeader>
          <DialogTitle>Discount</DialogTitle>
          <DialogDescription>Subtotal is {order.subtotal.toFixed(2)}.</DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          <div className="flex gap-1.5">
            {TYPES.map((option) => (
              <button
                key={option.value}
                type="button"
                onClick={() => setType(option.value)}
                className={cn(
                  "flex-1 rounded-md border px-3 py-2 text-sm transition-colors",
                  type === option.value
                    ? "border-primary bg-primary text-primary-foreground"
                    : "border-input hover:bg-muted",
                )}
              >
                {option.label}
              </button>
            ))}
          </div>

          {type !== "None" && (
            <FormField
              htmlFor="discount-value"
              label={type === "Percentage" ? "Percentage off" : "Amount off"}
              required
            >
              <Input
                id="discount-value"
                inputMode="decimal"
                value={value}
                onChange={(event) => setValue(event.target.value)}
                placeholder={type === "Percentage" ? "10" : "100.00"}
                autoFocus
              />
            </FormField>
          )}

          {type !== "None" && (
            <div className="rounded-lg border bg-muted/40 p-3 text-sm">
              <div className="flex justify-between">
                <span className="text-muted-foreground">Discount</span>
                <span className="tabular">−{preview.toFixed(2)}</span>
              </div>
              <div className="mt-1 flex justify-between font-semibold">
                <span>New total</span>
                <span className="tabular">{Math.max(0, order.subtotal - preview).toFixed(2)}</span>
              </div>
            </div>
          )}
        </div>

        <DialogFooter>
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={pending}>
            Cancel
          </Button>
          <Button type="button" onClick={submit} loading={pending}>
            Apply
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
