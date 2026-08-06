import { useState } from "react";
import { Controller, useForm } from "react-hook-form";
import { toast } from "sonner";
import { Paperclip, Trash2 } from "lucide-react";
import type { Expense, ExpenseCategory, ExpensePaymentMethod } from "@/entities/expense";
import { EXPENSE_PAYMENT_METHODS, EXPENSE_PAYMENT_METHOD_LABELS, requiresReference } from "@/entities/expense";
import { toApiError } from "@/shared/api/problem";
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
import { expensesApi } from "../api/expensesApi";
import { useExpenseMutations } from "../model/useExpenses";

export interface ExpenseFormDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  categories: ExpenseCategory[];
  /** The expense being corrected, or omitted to record a new one. */
  expense?: Expense;
}

interface FormValues {
  expenseDate: string;
  categoryId: string;
  amount: string;
  description: string;
  paymentMethod: ExpensePaymentMethod;
  paymentReference: string;
  paymentDate: string;
  isPaid: boolean;
}

const today = () => new Date().toISOString().slice(0, 10);

/** Records money paid out, or corrects an expense nobody has ruled on yet (EXP-001 to EXP-008). */
export function ExpenseFormDialog({ open, onOpenChange, categories, expense }: ExpenseFormDialogProps) {
  const isEditing = !!expense;
  const { create, update, addAttachment, removeAttachment } = useExpenseMutations();
  const [pendingFile, setPendingFile] = useState<File | null>(null);

  const pending = create.isPending || update.isPending || addAttachment.isPending;

  const defaults: FormValues = {
    expenseDate: expense?.expenseDate ?? today(),
    categoryId: expense?.categoryId ?? "",
    amount: expense ? String(expense.amount) : "",
    description: expense?.description ?? "",
    paymentMethod: expense?.paymentMethod ?? "Cash",
    paymentReference: expense?.paymentReference ?? "",
    paymentDate: expense?.paymentDate ?? "",
    isPaid: expense?.isPaid ?? false,
  };

  const {
    control,
    register,
    handleSubmit,
    reset,
    watch,
    formState: { errors },
  } = useForm<FormValues>({ defaultValues: defaults });

  const method = watch("paymentMethod");
  const referenceRequired = requiresReference(method);

  const close = (isOpen: boolean) => {
    if (!isOpen) {
      reset(defaults);
      setPendingFile(null);
    }
    onOpenChange(isOpen);
  };

  const build = (values: FormValues) => ({
    expenseDate: values.expenseDate,
    categoryId: values.categoryId,
    amount: Number(values.amount),
    description: values.description.trim() || null,
    paymentMethod: values.paymentMethod,
    paymentReference: values.paymentReference.trim() || null,
    paymentDate: values.paymentDate || null,
    isPaid: values.isPaid,
  });

  const save = (submitForApproval: boolean) =>
    handleSubmit(async (values) => {
      const amount = Number(values.amount);

      if (!Number.isFinite(amount) || amount <= 0) {
        toast.error("Enter an amount greater than zero.");
        return;
      }

      if (values.expenseDate > today()) {
        toast.error("An expense cannot be dated in the future.");
        return;
      }

      try {
        const saved = isEditing
          ? await update.mutateAsync({ id: expense.id, payload: build(values) })
          : await create.mutateAsync({ ...build(values), submitForApproval });

        if (pendingFile) {
          await addAttachment.mutateAsync({ id: saved.id, file: pendingFile });
        }

        toast.success(
          isEditing
            ? `${saved.expenseNumber} was updated.`
            : `${saved.expenseNumber} recorded${submitForApproval ? " and submitted for approval" : " as a draft"}.`,
        );
        close(false);
      } catch (error) {
        toast.error(toApiError(error).message);
      }
    })();

  const dropAttachment = async (attachmentId: string) => {
    try {
      await removeAttachment.mutateAsync({ id: expense!.id, attachmentId });
      toast.success("Receipt removed.");
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  };

  return (
    <Dialog open={open} onOpenChange={close}>
      <DialogContent className="max-h-[90vh] max-w-xl overflow-y-auto">
        <DialogHeader>
          <DialogTitle>{isEditing ? `Edit ${expense.expenseNumber}` : "Record expense"}</DialogTitle>
          <DialogDescription>
            {isEditing
              ? "An expense can be corrected until it is approved."
              : "Save it as a draft, or send it straight for approval."}
          </DialogDescription>
        </DialogHeader>

        <form className="space-y-4">
          <div className="grid gap-4 sm:grid-cols-2">
            <FormField htmlFor="expenseDate" label="Expense date" required error={errors.expenseDate?.message}>
              <Input
                {...register("expenseDate", { required: "A date is required." })}
                id="expenseDate"
                type="date"
                max={today()}
              />
            </FormField>

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
                          {category.parentCategoryName
                            ? `${category.parentCategoryName} › ${category.name}`
                            : category.name}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
            </FormField>
          </div>

          <FormField htmlFor="amount" label="Amount" required error={errors.amount?.message}>
            <Input
              {...register("amount", { required: "An amount is required." })}
              id="amount"
              inputMode="decimal"
              placeholder="2500.00"
              className="tabular"
            />
          </FormField>

          <FormField
            htmlFor="description"
            label="Description"
            hint="Optional, up to 500 characters"
            error={errors.description?.message}
          >
            <Input
              {...register("description", { maxLength: { value: 500, message: "Up to 500 characters." } })}
              id="description"
              placeholder="e.g. Gas cylinder refill for kitchen"
            />
          </FormField>

          <div className="grid gap-4 sm:grid-cols-2">
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

            <FormField
              htmlFor="paymentReference"
              label={method === "Cheque" ? "Cheque number" : "Reference"}
              required={referenceRequired}
              hint={referenceRequired ? undefined : "Optional for cash"}
              error={errors.paymentReference?.message}
            >
              <Input
                {...register("paymentReference", {
                  validate: (value) =>
                    !requiresReference(method) || value.trim().length > 0
                      ? true
                      : "A reference is required for cheque, card and bank transfer.",
                })}
                id="paymentReference"
                placeholder={method === "Cheque" ? "CHK-001" : "TRF-WTR-12345"}
              />
            </FormField>
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
            <FormField htmlFor="paymentDate" label="Payment date" hint="When the money leaves. Optional.">
              <Input {...register("paymentDate")} id="paymentDate" type="date" />
            </FormField>

            <div className="flex items-end pb-2">
              <Controller
                name="isPaid"
                control={control}
                render={({ field }) => (
                  <label className="flex cursor-pointer items-center gap-2 text-sm">
                    <Checkbox
                      checked={field.value}
                      onCheckedChange={(value) => field.onChange(value === true)}
                    />
                    <Label className="cursor-pointer">Already paid</Label>
                  </label>
                )}
              />
            </div>
          </div>

          <div className="space-y-2 rounded-lg border border-dashed p-3">
            <p className="text-sm font-medium">Receipt / invoice</p>

            {isEditing && expense.attachments.length > 0 && (
              <ul className="space-y-1">
                {expense.attachments.map((attachment) => (
                  <li key={attachment.id} className="flex items-center gap-2 text-sm">
                    <Paperclip className="size-3.5 shrink-0 text-muted-foreground" />
                    <a
                      href={expensesApi.attachmentUrl(attachment.id)}
                      target="_blank"
                      rel="noreferrer"
                      className="min-w-0 flex-1 truncate underline-offset-2 hover:underline"
                    >
                      {attachment.fileName}
                    </a>
                    {expense.isEditable && (
                      <Button
                        type="button"
                        variant="ghost"
                        size="icon"
                        className="size-7"
                        aria-label={`Remove ${attachment.fileName}`}
                        onClick={() => dropAttachment(attachment.id)}
                      >
                        <Trash2 className="size-3.5 text-destructive" />
                      </Button>
                    )}
                  </li>
                ))}
              </ul>
            )}

            <Input
              type="file"
              accept="image/jpeg,image/png,image/webp,application/pdf"
              onChange={(event) => setPendingFile(event.target.files?.[0] ?? null)}
              className="cursor-pointer text-sm file:mr-3 file:cursor-pointer file:rounded file:border-0 file:bg-muted file:px-2 file:py-1"
            />
            <p className="text-xs text-muted-foreground">JPEG, PNG, WebP or PDF, up to 10 MB. Optional.</p>
          </div>

          <DialogFooter className="gap-2">
            <Button type="button" variant="outline" onClick={() => close(false)} disabled={pending}>
              Cancel
            </Button>
            <Button type="button" variant="secondary" onClick={() => save(false)} loading={pending}>
              {isEditing ? "Save changes" : "Save as draft"}
            </Button>
            {!isEditing && (
              <Button type="button" onClick={() => save(true)} loading={pending}>
                Submit for approval
              </Button>
            )}
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
