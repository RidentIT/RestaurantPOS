import { useForm } from "react-hook-form";
import { toast } from "sonner";
import type { Steward } from "@/entities/steward";
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
import { useStewardMutations } from "../model/useStewards";

export interface StewardFormDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  steward?: Steward;
}

interface FormValues {
  name: string;
}

/** Adds a steward to the roster, or corrects one's name. */
export function StewardFormDialog({ open, onOpenChange, steward }: StewardFormDialogProps) {
  const isEditing = !!steward;
  const { create, rename } = useStewardMutations();
  const pending = create.isPending || rename.isPending;

  const defaults: FormValues = { name: steward?.name ?? "" };

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
    const name = values.name.trim();

    try {
      if (isEditing) {
        await rename.mutateAsync({ id: steward.id, name });
        toast.success("Steward renamed.");
      } else {
        await create.mutateAsync(name);
        toast.success(`${name} was added.`);
      }
      close(false);
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  });

  return (
    <Dialog open={open} onOpenChange={close}>
      <DialogContent className="max-w-sm">
        <DialogHeader>
          <DialogTitle>{isEditing ? "Rename steward" : "Add steward"}</DialogTitle>
          <DialogDescription>
            The name the cashier picks when opening a table, and the one on the sales-by-steward report.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={onSubmit} className="space-y-4">
          <FormField htmlFor="steward-name" label="Name" required error={errors.name?.message}>
            <Input
              id="steward-name"
              {...register("name", { required: "A steward's name is required." })}
              placeholder="Kamal"
              autoFocus
              maxLength={80}
            />
          </FormField>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => close(false)} disabled={pending}>
              Cancel
            </Button>
            <Button type="submit" loading={pending}>
              {isEditing ? "Save" : "Add steward"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
