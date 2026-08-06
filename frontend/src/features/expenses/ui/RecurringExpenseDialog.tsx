import { Controller, useForm } from "react-hook-form";
import { toast } from "sonner";
import type { ExpenseCategory, ExpensePaymentMethod, RecurringExpense } from "@/entities/expense";
import { EXPENSE_PAYMENT_METHODS, EXPENSE_PAYMENT_METHOD_LABELS } from "@/entities/expense";
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
import { useRecurringExpenseMutations } from "../model/useExpenses";

export interface RecurringExpenseDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  categories: ExpenseCategory[];
  recurring?: RecurringExpense;
}

interface FormValues {
  categoryId: string;
  amount: string;
  description: string;
  paymentMethod: ExpensePaymentMethod;
  dayOfMonth: string;
}

/** Sets up a standing monthly cost such as rent or salaries (EXP-012). */
export function RecurringExpenseDialog({
  open,
  onOpenChange,
  categories,
  recurring,
}: RecurringExpenseDialogProps) {
  const isEditing = !!recurring;
  const { save } = useRecurringExpenseMutations();

  const defaults: FormValues = {
    categoryId: recurring?.categoryId ?? "",
    amount: recurring ? String(recurring.amount) : "",
    description: recurring?.description ?? "",
    paymentMethod: recurring?.paymentMethod ?? "BankTransfer",
    dayOfMonth: String(recurring?.dayOfMonth ?? 1),
  };

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
    const amount = Number(values.amount);
    const dayOfMonth = Number(values.dayOfMonth);

    if (!Number.isFinite(amount) || amount <= 0) {
      toast.error("Enter an amount greater than zero.");
      return;
    }

    if (!Number.isInteger(dayOfMonth) || dayOfMonth < 1 || dayOfMonth > 31) {
      toast.error("The day of the month must be between 1 and 31.");
      return;
    }

    try {
      await save.mutateAsync({
        id: recurring?.id,
        payload: {
          categoryId: values.categoryId,
          amount,
          description: values.description.trim() || null,
          paymentMethod: values.paymentMethod,
          dayOfMonth,
        },
      });

      toast.success(isEditing ? "Recurring expense updated." : "Recurring expense set up.");
      close(false);
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  });

  return (
    <Dialog open={open} onOpenChange={close}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{isEditing ? "Edit recurring expense" : "Add recurring expense"}</DialogTitle>
          <DialogDescription>
            Created as a draft each month when the expenses screen is first opened, so the figure can be
            checked before it counts.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={onSubmit} className="space-y-4">
          <FormField htmlFor="categoryId" label="Category" required error={errors.categoryId?.message}>
            <Controller
              name="categoryId"
              control={control}
              rules={{ required: "Choose a category." }}
              render={({ field }) => (
                <Select value={field.value} onValueChange={field.onChange}>
                  <SelectTrigger id="categoryId">
                    <SelectValue placeholder="Choose a category" />
                  </SelectTrigger>
                  <SelectContent>
                    {categories.map((category) => (
                      <SelectItem key={category.id} value={category.id}>
                        {category.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
            />
          </FormField>

          <div className="grid gap-4 sm:grid-cols-2">
            <FormField htmlFor="amount" label="Amount" required>
              <Input
                {...register("amount", { required: true })}
                id="amount"
                inputMode="decimal"
                placeholder="50000"
                className="tabular"
              />
            </FormField>

            <FormField htmlFor="dayOfMonth" label="Day of month" required hint="Clamped in short months">
              <Input
                {...register("dayOfMonth", { required: true })}
                id="dayOfMonth"
                inputMode="numeric"
                placeholder="1"
                className="tabular"
              />
            </FormField>
          </div>

          <FormField htmlFor="paymentMethod" label="Payment method" required>
            <Controller
              name="paymentMethod"
              control={control}
              render={({ field }) => (
                <Select value={field.value} onValueChange={field.onChange}>
                  <SelectTrigger id="paymentMethod">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {EXPENSE_PAYMENT_METHODS.map((option) => (
                      <SelectItem key={option} value={option}>
                        {EXPENSE_PAYMENT_METHOD_LABELS[option]}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
            />
          </FormField>

          <FormField htmlFor="description" label="Description" hint="Optional">
            <Input {...register("description")} id="description" placeholder="Monthly rent" />
          </FormField>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => close(false)} disabled={save.isPending}>
              Cancel
            </Button>
            <Button type="submit" loading={save.isPending}>
              {isEditing ? "Save changes" : "Add"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
