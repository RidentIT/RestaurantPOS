import { useMemo, useState } from "react";
import { Controller, useForm } from "react-hook-form";
import { ClipboardCheck } from "lucide-react";
import { toast } from "sonner";
import type { StockLineInput } from "@/entities/inventory";
import type { RawMaterial } from "@/entities/raw-material";
import { UNIT_ABBREVIATIONS } from "@/entities/raw-material";
import type { Supplier } from "@/entities/supplier";
import {
  PURCHASE_ORDER_STATUS_BADGE,
  describeOrderContents,
  purchaseOrderRef,
  usePurchaseOrders,
} from "@/features/purchase-orders";
import {
  Badge,
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
  const purchaseOrderId = watch("purchaseOrderId");

  // Only orders already sent to (or received from) this supplier make sense to link a delivery
  // to — a draft has not been placed yet, and a cancelled order cannot be fulfilled.
  const { data: supplierOrders } = usePurchaseOrders({ supplierId }, !!supplierId);
  const linkableOrders = useMemo(
    () => (supplierOrders ?? []).filter((o) => o.status !== "Draft" && o.status !== "Cancelled"),
    [supplierOrders],
  );

  const selectedOrder = useMemo(
    () => linkableOrders.find((o) => o.id === purchaseOrderId),
    [linkableOrders, purchaseOrderId],
  );

  /**
   * Copies the ordered materials and quantities onto the delivery. A delivery usually matches
   * what was ordered, so keying it in a second time is wasted effort — and every line stays
   * editable afterwards for the times it arrives short.
   */
  const fillFromOrder = () => {
    if (!selectedOrder) return;

    const known = selectedOrder.lines.filter((line) =>
      rawMaterials.some((material) => material.id === line.rawMaterialId),
    );

    if (known.length === 0) {
      toast.error("None of this order's materials are available to receive.");
      return;
    }

    setLines(known.map((line) => ({ rawMaterialId: line.rawMaterialId, quantity: line.quantity })));
    toast.success(`Filled in ${known.length} line${known.length === 1 ? "" : "s"} from the order.`);
  };

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
                    <SelectContent className="max-w-[var(--radix-select-trigger-width)]">
                      <SelectItem value={NO_PURCHASE_ORDER}>Not linked to an order</SelectItem>
                      {linkableOrders.map((order) => (
                        <SelectItem key={order.id} value={order.id}>
                          <span className="font-mono text-xs text-muted-foreground">
                            {purchaseOrderRef(order.id)}
                          </span>
                          {" · "}
                          {describeOrderContents(order.lines)}
                          {" · "}
                          <span className="tabular">{order.totalAmount.toFixed(2)}</span>
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
            </FormField>
          )}

          {/* What was actually ordered, so the storekeeper can check the delivery against it
              without leaving this screen. */}
          {selectedOrder && (
            <div className="space-y-3 rounded-lg border bg-muted/30 p-3">
              <div className="flex flex-wrap items-center gap-2">
                <span className="font-mono text-xs text-muted-foreground">
                  {purchaseOrderRef(selectedOrder.id)}
                </span>
                <Badge variant={PURCHASE_ORDER_STATUS_BADGE[selectedOrder.status]}>
                  {selectedOrder.status}
                </Badge>
                <span className="text-xs text-muted-foreground">
                  Ordered {new Date(selectedOrder.createdAtUtc).toLocaleDateString()}
                  {selectedOrder.expectedDeliveryDate &&
                    ` · Expected ${new Date(selectedOrder.expectedDeliveryDate).toLocaleDateString()}`}
                </span>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  className="ml-auto"
                  onClick={fillFromOrder}
                  disabled={selectedOrder.lines.length === 0}
                >
                  <ClipboardCheck /> Fill in what was ordered
                </Button>
              </div>

              {selectedOrder.lines.length === 0 ? (
                <p className="text-sm text-muted-foreground">This order has no materials on it.</p>
              ) : (
                <ul className="space-y-1 text-sm">
                  {selectedOrder.lines.map((line) => (
                    <li key={line.rawMaterialId} className="flex items-baseline gap-2">
                      <span className="min-w-0 flex-1 truncate">{line.rawMaterialName}</span>
                      <span className="shrink-0 tabular text-muted-foreground">
                        {line.quantity} {UNIT_ABBREVIATIONS[line.unitOfMeasurement]} × {line.unitPrice.toFixed(2)}
                      </span>
                      <span className="w-20 shrink-0 text-right tabular">{line.lineTotal.toFixed(2)}</span>
                    </li>
                  ))}
                  <li className="flex items-baseline gap-2 border-t pt-1 font-medium">
                    <span className="flex-1">Order total</span>
                    <span className="w-20 text-right tabular">{selectedOrder.totalAmount.toFixed(2)}</span>
                  </li>
                </ul>
              )}
            </div>
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
