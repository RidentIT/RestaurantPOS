import { Controller, useForm } from "react-hook-form";
import { toast } from "sonner";
import type { StoreType } from "@/entities/inventory";
import type { RawMaterial } from "@/entities/raw-material";
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
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/shared/ui";
import { useInventoryMutations } from "../model/useInventory";

export interface StockAdjustmentDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  store: StoreType;
  rawMaterials: RawMaterial[];
}

interface FormValues {
  rawMaterialId: string;
  direction: "increase" | "decrease";
  quantity: string;
  reason: string;
}

/**
 * Corrects a raw material's balance to match a physical stock count. The direction and quantity
 * are entered separately (rather than one signed number) so it reads naturally: "decrease Rice
 * by 2 kg", not "adjust Rice by -2".
 */
export function StockAdjustmentDialog({ open, onOpenChange, store, rawMaterials }: StockAdjustmentDialogProps) {
  const { createMainStoreAdjustment, createKitchenAdjustment } = useInventoryMutations();
  const mutation = store === "MainStore" ? createMainStoreAdjustment : createKitchenAdjustment;

  const {
    register,
    handleSubmit,
    control,
    reset,
    formState: { errors },
  } = useForm<FormValues>({
    defaultValues: { rawMaterialId: "", direction: "decrease", quantity: "", reason: "" },
  });

  const close = (isOpen: boolean) => {
    if (!isOpen) reset({ rawMaterialId: "", direction: "decrease", quantity: "", reason: "" });
    onOpenChange(isOpen);
  };

  const onSubmit = handleSubmit(async (values) => {
    const magnitude = Number(values.quantity);

    if (!Number.isFinite(magnitude) || magnitude <= 0) {
      return;
    }

    const quantityDelta = values.direction === "decrease" ? -magnitude : magnitude;

    try {
      await mutation.mutateAsync({ rawMaterialId: values.rawMaterialId, store, quantityDelta, reason: values.reason });
      toast.success("Stock adjustment recorded.");
      close(false);
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Something went wrong.");
    }
  });

  return (
    <Dialog open={open} onOpenChange={close}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Adjust stock</DialogTitle>
          <DialogDescription>Correct a raw material's balance to match a physical count.</DialogDescription>
        </DialogHeader>

        <form onSubmit={onSubmit} className="space-y-4">
          <FormField htmlFor="rawMaterialId" label="Raw material" required error={errors.rawMaterialId?.message}>
            <Controller
              name="rawMaterialId"
              control={control}
              rules={{ required: "Choose a raw material." }}
              render={({ field }) => (
                <Select value={field.value} onValueChange={field.onChange}>
                  <SelectTrigger id="rawMaterialId">
                    <SelectValue placeholder="Choose a raw material" />
                  </SelectTrigger>
                  <SelectContent>
                    {rawMaterials.map((r) => (
                      <SelectItem key={r.id} value={r.id}>
                        {r.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
            />
          </FormField>

          <div className="grid grid-cols-[1fr_auto] gap-3">
            <FormField htmlFor="quantity" label="Quantity" required error={errors.quantity?.message}>
              <Input
                {...register("quantity", { required: "Enter a quantity." })}
                inputMode="decimal"
                placeholder="e.g. 2"
              />
            </FormField>

            <FormField htmlFor="direction" label="Direction" required>
              <Controller
                name="direction"
                control={control}
                render={({ field }) => (
                  <Select value={field.value} onValueChange={field.onChange}>
                    <SelectTrigger id="direction" className="w-32">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="decrease">Decrease</SelectItem>
                      <SelectItem value="increase">Increase</SelectItem>
                    </SelectContent>
                  </Select>
                )}
              />
            </FormField>
          </div>

          <FormField htmlFor="reason" label="Reason" required error={errors.reason?.message}>
            <Input
              {...register("reason", { required: "A reason is required." })}
              placeholder="e.g. Spillage during prep"
            />
          </FormField>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => close(false)} disabled={mutation.isPending}>
              Cancel
            </Button>
            <Button type="submit" loading={mutation.isPending}>
              Save adjustment
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
