import { useEffect, useState } from "react";
import { Minus, Plus } from "lucide-react";
import type { MenuItem } from "@/entities/menu-item";
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

export interface AddItemDialogProps {
  /** The dish being added, or null when the dialog is closed. */
  item: MenuItem | null;
  onOpenChange: (open: boolean) => void;
  onAdd: (quantity: number, specialInstructions: string | null) => Promise<void>;
  pending: boolean;
}

/**
 * Confirms quantity and any special instructions for one dish (POS-004, POS-005).
 *
 * Opens on every pick rather than only when instructions are wanted, because "no chilli" is the
 * kind of thing a customer says in passing and a cashier has no second chance to capture once
 * the KOT has printed.
 */
export function AddItemDialog({ item, onOpenChange, onAdd, pending }: AddItemDialogProps) {
  const [quantity, setQuantity] = useState(1);
  const [instructions, setInstructions] = useState("");

  useEffect(() => {
    if (item) {
      setQuantity(1);
      setInstructions("");
    }
  }, [item]);

  const submit = async () => {
    await onAdd(quantity, instructions.trim() || null);
  };

  return (
    <Dialog open={!!item} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-sm">
        <DialogHeader>
          <DialogTitle>{item?.name}</DialogTitle>
          <DialogDescription>
            {item ? `${item.price.toFixed(2)} each · ${item.category}` : ""}
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          <FormField htmlFor="quantity" label="Quantity" required>
            <div className="flex items-center gap-2">
              <Button
                type="button"
                variant="outline"
                size="icon"
                aria-label="Decrease quantity"
                disabled={quantity <= 1}
                onClick={() => setQuantity((current) => Math.max(1, current - 1))}
              >
                <Minus className="size-4" />
              </Button>
              <Input
                id="quantity"
                inputMode="numeric"
                value={quantity}
                onChange={(event) => {
                  const parsed = Number(event.target.value.replace(/\D/g, ""));
                  setQuantity(Number.isFinite(parsed) && parsed > 0 ? parsed : 1);
                }}
                className="w-20 text-center text-lg tabular"
              />
              <Button
                type="button"
                variant="outline"
                size="icon"
                aria-label="Increase quantity"
                onClick={() => setQuantity((current) => current + 1)}
              >
                <Plus className="size-4" />
              </Button>
              <span className="ml-auto text-lg font-semibold tabular">
                {item ? (item.price * quantity).toFixed(2) : ""}
              </span>
            </div>
          </FormField>

          <FormField htmlFor="instructions" label="Special instructions" hint="Optional — printed on the KOT">
            <Input
              id="instructions"
              value={instructions}
              onChange={(event) => setInstructions(event.target.value)}
              placeholder="e.g. No chilli"
              onKeyDown={(event) => {
                if (event.key === "Enter") {
                  event.preventDefault();
                  void submit();
                }
              }}
            />
          </FormField>
        </div>

        <DialogFooter>
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={pending}>
            Cancel
          </Button>
          <Button type="button" onClick={submit} loading={pending}>
            Add to bill
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
