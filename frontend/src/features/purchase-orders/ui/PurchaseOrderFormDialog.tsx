import { useState } from "react";
import { Controller, useForm } from "react-hook-form";
import { toast } from "sonner";
import type { RawMaterial } from "@/entities/raw-material";
import type { PurchaseOrder, PurchaseOrderLineInput, Supplier } from "@/entities/supplier";
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
import { toApiError } from "@/shared/api/problem";
import { usePurchaseOrderMutations } from "../model/usePurchaseOrders";
import { PurchaseOrderLinesEditor } from "./PurchaseOrderLinesEditor";

export interface PurchaseOrderFormDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  suppliers: Supplier[];
  rawMaterials: RawMaterial[];
  /** The draft order being edited, or omitted to create a new one. */
  order?: PurchaseOrder;
}

interface FormValues {
  supplierId: string;
  expectedDeliveryDate: string;
  notes: string;
}

/**
 * Creates a new draft purchase order, or edits an existing one that has not yet been submitted —
 * the supplier is fixed once a draft exists, since submitting sends it to that specific supplier.
 */
export function PurchaseOrderFormDialog({
  open,
  onOpenChange,
  suppliers,
  rawMaterials,
  order,
}: PurchaseOrderFormDialogProps) {
  const isEditing = !!order;
  const { create, update } = usePurchaseOrderMutations();
  const pending = create.isPending || update.isPending;

  const [lines, setLines] = useState<PurchaseOrderLineInput[]>(
    order?.lines.map((l) => ({ rawMaterialId: l.rawMaterialId, quantity: l.quantity, unitPrice: l.unitPrice })) ?? [],
  );

  const defaults: FormValues = {
    supplierId: order?.supplierId ?? "",
    expectedDeliveryDate: order?.expectedDeliveryDate?.slice(0, 10) ?? "",
    notes: order?.notes ?? "",
  };

  const {
    control,
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<FormValues>({ defaultValues: defaults });

  const close = (isOpen: boolean) => {
    if (!isOpen) {
      reset(defaults);
      setLines(order?.lines.map((l) => ({ rawMaterialId: l.rawMaterialId, quantity: l.quantity, unitPrice: l.unitPrice })) ?? []);
    }
    onOpenChange(isOpen);
  };

  const onSubmit = handleSubmit(async (values) => {
    if (lines.length === 0) {
      toast.error("Add at least one raw material.");
      return;
    }

    const expectedDeliveryDate = values.expectedDeliveryDate || null;
    const notes = values.notes.trim() || null;

    try {
      if (isEditing) {
        await update.mutateAsync({ id: order.id, payload: { lines, expectedDeliveryDate, notes } });
        toast.success("Purchase order updated.");
      } else {
        await create.mutateAsync({ supplierId: values.supplierId, lines, expectedDeliveryDate, notes });
        toast.success("Purchase order created as a draft.");
      }
      close(false);
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  });

  return (
    <Dialog open={open} onOpenChange={close}>
      <DialogContent className="max-w-2xl">
        <DialogHeader>
          <DialogTitle>{isEditing ? "Edit draft purchase order" : "New purchase order"}</DialogTitle>
          <DialogDescription>
            {isEditing ? "Only draft orders can be edited." : "Created as a draft — submit it to send it to the supplier."}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={onSubmit} className="space-y-4">
          <FormField htmlFor="supplierId" label="Supplier" required error={errors.supplierId?.message}>
            <Controller
              name="supplierId"
              control={control}
              rules={{ required: "Choose a supplier." }}
              render={({ field }) => (
                <Select value={field.value} onValueChange={field.onChange} disabled={isEditing}>
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

          <div>
            <p className="mb-2 text-sm font-medium">Raw materials ordered</p>
            <PurchaseOrderLinesEditor rawMaterials={rawMaterials} lines={lines} onChange={setLines} />
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
            <FormField htmlFor="expectedDeliveryDate" label="Expected delivery" hint="Optional">
              <Input {...register("expectedDeliveryDate")} type="date" />
            </FormField>

            <FormField htmlFor="notes" label="Notes" hint="Optional">
              <Input {...register("notes")} placeholder="e.g. Confirm before Friday" />
            </FormField>
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => close(false)} disabled={pending}>
              Cancel
            </Button>
            <Button type="submit" loading={pending}>
              {isEditing ? "Save changes" : "Create draft"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
