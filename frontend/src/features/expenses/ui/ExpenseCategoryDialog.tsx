import { useForm, Controller } from "react-hook-form";
import { toast } from "sonner";
import type { ExpenseCategory } from "@/entities/expense";
import { toApiError } from "@/shared/api/problem";
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
import { useExpenseCategoryMutations } from "../model/useExpenses";

export interface ExpenseCategoryDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  categories: ExpenseCategory[];
  category?: ExpenseCategory;
}

interface FormValues {
  name: string;
  description: string;
  monthlyBudget: string;
  parentCategoryId: string;
}

const NO_PARENT = "none";

/** Adds or edits an expense category and its monthly budget (EXP-011, BR-EXP-015). */
export function ExpenseCategoryDialog({
  open,
  onOpenChange,
  categories,
  category,
}: ExpenseCategoryDialogProps) {
  const isEditing = !!category;
  const { create, update } = useExpenseCategoryMutations();
  const pending = create.isPending || update.isPending;

  const defaults: FormValues = {
    name: category?.name ?? "",
    description: category?.description ?? "",
    monthlyBudget: category?.monthlyBudget != null ? String(category.monthlyBudget) : "",
    parentCategoryId: category?.parentCategoryId ?? NO_PARENT,
  };

  const {
    control,
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<FormValues>({ defaultValues: defaults });

  // Only top-level categories can be parents, and nothing can parent itself.
  const parentOptions = categories.filter(
    (c) => c.parentCategoryId === null && c.id !== category?.id && c.isActive,
  );

  const close = (isOpen: boolean) => {
    if (!isOpen) reset(defaults);
    onOpenChange(isOpen);
  };

  const onSubmit = handleSubmit(async (values) => {
    const budget = values.monthlyBudget.trim();

    if (budget !== "" && (!Number.isFinite(Number(budget)) || Number(budget) < 0)) {
      toast.error("Enter a budget of zero or more, or leave it blank.");
      return;
    }

    const payload = {
      name: values.name.trim(),
      description: values.description.trim() || null,
      monthlyBudget: budget === "" ? null : Number(budget),
      parentCategoryId: values.parentCategoryId === NO_PARENT ? null : values.parentCategoryId,
    };

    try {
      if (isEditing) {
        await update.mutateAsync({ id: category.id, payload });
        toast.success(`${payload.name} was updated.`);
      } else {
        await create.mutateAsync(payload);
        toast.success(`${payload.name} was added.`);
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
          <DialogTitle>{isEditing ? "Edit category" : "Add category"}</DialogTitle>
          <DialogDescription>
            A budget is optional. Setting one turns on overspend alerts for this category.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={onSubmit} className="space-y-4">
          <FormField htmlFor="name" label="Name" required error={errors.name?.message}>
            <Input
              {...register("name", { required: "A name is required." })}
              id="name"
              placeholder="Cleaning Supplies"
              autoFocus
            />
          </FormField>

          <FormField htmlFor="description" label="Description" hint="Optional">
            <Input {...register("description")} id="description" placeholder="Mops and detergent" />
          </FormField>

          <div className="grid gap-4 sm:grid-cols-2">
            <FormField htmlFor="monthlyBudget" label="Monthly budget" hint="Optional">
              <Input
                {...register("monthlyBudget")}
                id="monthlyBudget"
                inputMode="decimal"
                placeholder="50000"
                className="tabular"
              />
            </FormField>

            <FormField htmlFor="parentCategoryId" label="Sits under" hint="Optional">
              <Controller
                name="parentCategoryId"
                control={control}
                render={({ field }) => (
                  <Select value={field.value} onValueChange={field.onChange}>
                    <SelectTrigger id="parentCategoryId">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value={NO_PARENT}>Top level</SelectItem>
                      {parentOptions.map((option) => (
                        <SelectItem key={option.id} value={option.id}>
                          {option.name}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
            </FormField>
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => close(false)} disabled={pending}>
              Cancel
            </Button>
            <Button type="submit" loading={pending}>
              {isEditing ? "Save changes" : "Add category"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
