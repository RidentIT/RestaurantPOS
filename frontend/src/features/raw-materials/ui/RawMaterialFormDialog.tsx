import { zodResolver } from "@hookform/resolvers/zod";
import { Controller, useForm } from "react-hook-form";
import { toast } from "sonner";
import type { RawMaterial, UnitOfMeasurement } from "@/entities/raw-material";
import { UNITS_OF_MEASUREMENT } from "@/entities/raw-material";
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
import { useRawMaterialMutations } from "../model/useRawMaterials";
import { RawMaterialForm, rawMaterialSchema, toNullableThreshold } from "../model/rawMaterialSchema";

export interface RawMaterialFormDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /** The raw material being edited, or omitted to create a new one. */
  rawMaterial?: RawMaterial;
}

/** Creates or edits a raw material: its name, unit of measurement, and stock thresholds. */
export function RawMaterialFormDialog({ open, onOpenChange, rawMaterial }: RawMaterialFormDialogProps) {
  const isEditing = !!rawMaterial;
  const { create, update } = useRawMaterialMutations();
  const pending = create.isPending || update.isPending;

  const defaults = {
    name: rawMaterial?.name ?? "",
    unitOfMeasurement: rawMaterial?.unitOfMeasurement ?? "Kilogram",
    mainStoreReorderLevel: rawMaterial?.mainStoreReorderLevel != null ? String(rawMaterial.mainStoreReorderLevel) : "",
    kitchenParLevel: rawMaterial?.kitchenParLevel != null ? String(rawMaterial.kitchenParLevel) : "",
  };

  const {
    register,
    handleSubmit,
    control,
    reset,
    formState: { errors },
  } = useForm<RawMaterialForm>({ resolver: zodResolver(rawMaterialSchema), defaultValues: defaults });

  const close = (isOpen: boolean) => {
    if (!isOpen) reset(defaults);
    onOpenChange(isOpen);
  };

  const onSubmit = handleSubmit(async (values) => {
    const payload = {
      name: values.name,
      unitOfMeasurement: values.unitOfMeasurement as UnitOfMeasurement,
      mainStoreReorderLevel: toNullableThreshold(values.mainStoreReorderLevel),
      kitchenParLevel: toNullableThreshold(values.kitchenParLevel),
    };

    try {
      if (isEditing) {
        await update.mutateAsync({ id: rawMaterial.id, payload });
        toast.success(`${values.name} was updated.`);
      } else {
        await create.mutateAsync(payload);
        toast.success(`${values.name} was added.`);
      }
      close(false);
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Something went wrong.");
    }
  });

  return (
    <Dialog open={open} onOpenChange={close}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{isEditing ? "Edit raw material" : "Add raw material"}</DialogTitle>
          <DialogDescription>
            The unit chosen here is used everywhere this ingredient appears — recipes, stock counts and history.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={onSubmit} className="space-y-4">
          <FormField htmlFor="name" label="Name" required error={errors.name?.message}>
            <Input {...register("name")} placeholder="Rice" autoFocus />
          </FormField>

          <FormField htmlFor="unitOfMeasurement" label="Unit of measurement" required>
            <Controller
              name="unitOfMeasurement"
              control={control}
              render={({ field }) => (
                <Select value={field.value} onValueChange={field.onChange}>
                  <SelectTrigger id="unitOfMeasurement">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {UNITS_OF_MEASUREMENT.map((unit) => (
                      <SelectItem key={unit} value={unit}>
                        {unit}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
            />
          </FormField>

          <div className="grid gap-4 sm:grid-cols-2">
            <FormField
              htmlFor="mainStoreReorderLevel"
              label="Main Store reorder level"
              hint="Flags low stock. Optional."
              error={errors.mainStoreReorderLevel?.message}
            >
              <Input {...register("mainStoreReorderLevel")} inputMode="decimal" placeholder="e.g. 10" />
            </FormField>

            <FormField
              htmlFor="kitchenParLevel"
              label="Kitchen par level"
              hint="Flags low stock. Optional."
              error={errors.kitchenParLevel?.message}
            >
              <Input {...register("kitchenParLevel")} inputMode="decimal" placeholder="e.g. 2" />
            </FormField>
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => close(false)} disabled={pending}>
              Cancel
            </Button>
            <Button type="submit" loading={pending}>
              {isEditing ? "Save changes" : "Add raw material"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
