import { Controller, useForm } from "react-hook-form";
import { toast } from "sonner";
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
import { toApiError } from "@/shared/api/problem";
import { useSetSupplierPrice } from "../model/useSuppliers";

export interface SetSupplierPriceDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  supplierId: string;
  supplierName: string;
  rawMaterials: RawMaterial[];
  /** Preselects a raw material, e.g. when opened from that row's "Update price" action. */
  rawMaterialId?: string;
}

interface FormValues {
  rawMaterialId: string;
  price: string;
}

/** Records what a supplier currently charges for a raw material, keeping the prior price in history. */
export function SetSupplierPriceDialog({
  open,
  onOpenChange,
  supplierId,
  supplierName,
  rawMaterials,
  rawMaterialId,
}: SetSupplierPriceDialogProps) {
  const setPrice = useSetSupplierPrice();

  const defaults: FormValues = { rawMaterialId: rawMaterialId ?? "", price: "" };

  const {
    control,
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<FormValues>({ defaultValues: defaults });

  const close = (isOpen: boolean) => {
    if (!isOpen) reset(defaults);
    onOpenChange(isOpen);
  };

  const onSubmit = handleSubmit(async (values) => {
    const price = Number(values.price);

    if (!values.rawMaterialId) {
      toast.error("Choose a raw material.");
      return;
    }

    if (!Number.isFinite(price) || price < 0) {
      toast.error("Enter a price of zero or more.");
      return;
    }

    try {
      await setPrice.mutateAsync({ supplierId, rawMaterialId: values.rawMaterialId, price });
      toast.success("Price recorded.");
      close(false);
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  });

  return (
    <Dialog open={open} onOpenChange={close}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Set price</DialogTitle>
          <DialogDescription>What {supplierName} currently charges for a raw material.</DialogDescription>
        </DialogHeader>

        <form onSubmit={onSubmit} className="space-y-4">
          <FormField htmlFor="rawMaterialId" label="Raw material" required>
            <Controller
              name="rawMaterialId"
              control={control}
              rules={{ required: true }}
              render={({ field }) => (
                <Select value={field.value} onValueChange={field.onChange} disabled={!!rawMaterialId}>
                  <SelectTrigger id="rawMaterialId">
                    <SelectValue placeholder="Choose a raw material" />
                  </SelectTrigger>
                  <SelectContent>
                    {rawMaterials.map((material) => (
                      <SelectItem key={material.id} value={material.id}>
                        {material.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
            />
          </FormField>

          <FormField htmlFor="price" label="Price" required error={errors.price?.message}>
            <Input {...register("price")} inputMode="decimal" placeholder="e.g. 250.00" autoFocus />
          </FormField>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => close(false)} disabled={setPrice.isPending}>
              Cancel
            </Button>
            <Button type="submit" loading={setPrice.isPending}>
              Save price
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
