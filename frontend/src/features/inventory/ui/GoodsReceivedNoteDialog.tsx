import { useMemo, useState } from "react";
import { Controller, useForm } from "react-hook-form";
import { toast } from "sonner";
import type { StockLineInput } from "@/entities/inventory";
import type { RawMaterial } from "@/entities/raw-material";
import type { Supplier } from "@/entities/supplier";
import { usePurchaseOrders } from "@/features/purchase-orders";
import {
  Button,
  Checkbox,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  FormField,
  Input,
  Label,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/shared/ui";
import { useInventoryMutations } from "../model/useInventory";
import { StockLinesEditor } from "./StockLinesEditor";

export interface GoodsReceivedNoteDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  suppliers: Supplier[];
  rawMaterials: RawMaterial[];
}

const NO_PURCHASE_ORDER = "none";
const NOT_RATED = "unrated";
const QUALITY_RATINGS = [1, 2, 3, 4, 5];

interface FormValues {
  supplierId: string;
  purchaseOrderId: string;
  qualityRating: string;
  hasIssue: boolean;
  notes: string;
}

/**
 * Records stock received from a supplier. There is no separate confirmation step — saving this
 * increases Main Store stock immediately, since a GRN is only ever entered once the goods have
 * actually been counted in.
 */
export function GoodsReceivedNoteDialog({ open, onOpenChange, suppliers, rawMaterials }: GoodsReceivedNoteDialogProps) {
  const { createGoodsReceivedNote } = useInventoryMutations();
  const [lines, setLines] = useState<StockLineInput[]>([]);

  const defaults: FormValues = {
    supplierId: "",
    purchaseOrderId: NO_PURCHASE_ORDER,
    qualityRating: NOT_RATED,
    hasIssue: false,
    notes: "",
  };

  const {
    control,
    register,
    handleSubmit,
    reset,
    watch,
    setValue,
    formState: { errors },
  } = useForm<FormValues>({ defaultValues: defaults });

  const supplierId = watch("supplierId");

  // Only orders already sent to (or received from) this supplier make sense to link a delivery
  // to — a draft has not been placed yet, and a cancelled order cannot be fulfilled.
  const { data: supplierOrders } = usePurchaseOrders({ supplierId }, !!supplierId);
  const linkableOrders = useMemo(
    () => (supplierOrders ?? []).filter((o) => o.status !== "Draft" && o.status !== "Cancelled"),
    [supplierOrders],
  );

  const close = (isOpen: boolean) => {
    if (!isOpen) {
      reset(defaults);
      setLines([]);
    }
    onOpenChange(isOpen);
  };

  const onSubmit = handleSubmit(async (values) => {
    if (lines.length === 0) {
      toast.error("Add at least one raw material.");
      return;
    }

    try {
      await createGoodsReceivedNote.mutateAsync({
        supplierId: values.supplierId,
        lines,
        notes: values.notes.trim() || null,
        purchaseOrderId: values.purchaseOrderId === NO_PURCHASE_ORDER ? null : values.purchaseOrderId,
        qualityRating: values.qualityRating === NOT_RATED ? null : Number(values.qualityRating),
        hasIssue: values.hasIssue,
      });
      toast.success("Stock received. Main Store balances have been updated.");
      close(false);
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Something went wrong.");
    }
  });

  return (
    <Dialog open={open} onOpenChange={close}>
      <DialogContent className="max-w-xl">
        <DialogHeader>
          <DialogTitle>Receive goods</DialogTitle>
          <DialogDescription>Main Store stock increases as soon as you save this.</DialogDescription>
        </DialogHeader>

        <form onSubmit={onSubmit} className="space-y-4">
          <FormField htmlFor="supplierId" label="Supplier" required error={errors.supplierId?.message}>
            <Controller
              name="supplierId"
              control={control}
              rules={{ required: "Choose a supplier." }}
              render={({ field }) => (
                <Select
                  value={field.value}
                  onValueChange={(value) => {
                    field.onChange(value);
                    setValue("purchaseOrderId", NO_PURCHASE_ORDER);
                  }}
                >
                  <SelectTrigger id="supplierId">
                    <SelectValue placeholder="Choose a supplier" />
                  </SelectTrigger>
                  <SelectContent>
                    {suppliers.map((s) => (
                      <SelectItem key={s.id} value={s.id}>
                        {s.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
            />
          </FormField>

          {linkableOrders.length > 0 && (
            <FormField htmlFor="purchaseOrderId" label="Purchase order" hint="Optional — links this delivery to an order.">
              <Controller
                name="purchaseOrderId"
                control={control}
                render={({ field }) => (
                  <Select value={field.value} onValueChange={field.onChange}>
                    <SelectTrigger id="purchaseOrderId">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value={NO_PURCHASE_ORDER}>Not linked to an order</SelectItem>
                      {linkableOrders.map((order) => (
                        <SelectItem key={order.id} value={order.id}>
                          {order.status} · {new Date(order.createdAtUtc).toLocaleDateString()} · {order.totalAmount.toFixed(2)}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
            </FormField>
          )}

          <div>
            <p className="mb-2 text-sm font-medium">Raw materials received</p>
            <StockLinesEditor rawMaterials={rawMaterials} lines={lines} onChange={setLines} />
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
            <FormField htmlFor="qualityRating" label="Quality rating" hint="Optional">
              <Controller
                name="qualityRating"
                control={control}
                render={({ field }) => (
                  <Select value={field.value} onValueChange={field.onChange}>
                    <SelectTrigger id="qualityRating">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value={NOT_RATED}>Not rated</SelectItem>
                      {QUALITY_RATINGS.map((rating) => (
                        <SelectItem key={rating} value={String(rating)}>
                          {rating} / 5
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
            </FormField>

            <FormField htmlFor="notes" label="Notes" hint="Optional">
              <Input {...register("notes")} placeholder="e.g. Weekly delivery" />
            </FormField>
          </div>

          <Controller
            name="hasIssue"
            control={control}
            render={({ field }) => (
              <label className="flex cursor-pointer items-center gap-2 text-sm">
                <Checkbox checked={field.value} onCheckedChange={(value) => field.onChange(value === true)} />
                <Label className="cursor-pointer">Flag an issue with this delivery</Label>
              </label>
            )}
          />

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => close(false)} disabled={createGoodsReceivedNote.isPending}>
              Cancel
            </Button>
            <Button type="submit" loading={createGoodsReceivedNote.isPending}>
              Save
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
