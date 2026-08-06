import { useForm } from "react-hook-form";
import { toast } from "sonner";
import type { RestaurantTable } from "@/entities/table";
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
import { toApiError } from "@/shared/api/problem";
import { useTableMutations } from "../model/useTables";

export interface TableFormDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  table?: RestaurantTable;
}

interface FormValues {
  number: string;
  seats: string;
  notes: string;
}

/** Adds a table to the floor plan, or renames one. */
export function TableFormDialog({ open, onOpenChange, table }: TableFormDialogProps) {
  const isEditing = !!table;
  const { create, update } = useTableMutations();
  const pending = create.isPending || update.isPending;

  const defaults: FormValues = {
    number: table?.number ?? "",
    seats: table?.seats ? String(table.seats) : "",
    notes: table?.notes ?? "",
  };

  const {
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
    const seats = values.seats.trim() === "" ? 0 : Number(values.seats);

    if (!Number.isInteger(seats) || seats < 0) {
      toast.error("Seats must be a whole number of zero or more.");
      return;
    }

    const payload = {
      number: values.number.trim(),
      seats,
      notes: values.notes.trim() || null,
    };

    try {
      if (isEditing) {
        await update.mutateAsync({ id: table.id, payload });
        toast.success(`Table ${payload.number} was updated.`);
      } else {
        await create.mutateAsync(payload);
        toast.success(`Table ${payload.number} was added.`);
      }
      close(false);
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  });

  return (
    <Dialog open={open} onOpenChange={close}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{isEditing ? "Edit table" : "Add table"}</DialogTitle>
          <DialogDescription>
            The number is what staff call the table — it appears on every KOT and receipt.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={onSubmit} className="space-y-4">
          <div className="grid gap-4 sm:grid-cols-2">
            <FormField htmlFor="number" label="Table number" required error={errors.number?.message}>
              <Input
                {...register("number", { required: "A table number is required." })}
                placeholder="4"
                autoFocus
              />
            </FormField>

            <FormField htmlFor="seats" label="Seats" hint="Optional">
              <Input {...register("seats")} inputMode="numeric" placeholder="4" />
            </FormField>
          </div>

          <FormField htmlFor="notes" label="Notes" hint="Optional">
            <Input {...register("notes")} placeholder="e.g. By the window" />
          </FormField>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => close(false)} disabled={pending}>
              Cancel
            </Button>
            <Button type="submit" loading={pending}>
              {isEditing ? "Save changes" : "Add table"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
