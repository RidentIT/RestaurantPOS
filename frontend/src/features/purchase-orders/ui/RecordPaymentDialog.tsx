import { Controller, useForm } from "react-hook-form";
import { toast } from "sonner";
import { PAYMENT_METHODS, type PaymentMethod } from "@/entities/supplier";
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

export interface RecordPaymentDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  purchaseOrderId: string;
  supplierId: string;
  /** Shown so the person recording the payment can see how much is left to pay. */
  balance: number;
}

interface FormValues {
  amount: string;
  paymentDateUtc: string;
  method: PaymentMethod;
  invoiceReference: string;
  notes: string;
}

const todayIso = () => new Date().toISOString().slice(0, 10);

/** Records a payment toward a purchase order's outstanding balance. */
export function RecordPaymentDialog({
  open,
  onOpenChange,
  purchaseOrderId,
  supplierId,
  balance,
}: RecordPaymentDialogProps) {
  const { recordPayment } = usePurchaseOrderMutations();

  const defaults: FormValues = {
    amount: balance > 0 ? balance.toFixed(2) : "",
    paymentDateUtc: todayIso(),
    method: "Cash",
    invoiceReference: "",
    notes: "",
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

    if (!Number.isFinite(amount) || amount <= 0) {
      toast.error("Enter a payment amount greater than zero.");
      return;
    }

    try {
      await recordPayment.mutateAsync({
        id: purchaseOrderId,
        supplierId,
        payload: {
          amount,
          paymentDateUtc: new Date(values.paymentDateUtc).toISOString(),
          method: values.method,
          invoiceReference: values.invoiceReference.trim() || null,
          notes: values.notes.trim() || null,
        },
      });
      toast.success("Payment recorded.");
      close(false);
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  });

  return (
    <Dialog open={open} onOpenChange={close}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Record payment</DialogTitle>
          <DialogDescription>Balance outstanding: {balance.toFixed(2)}</DialogDescription>
        </DialogHeader>

        <form onSubmit={onSubmit} className="space-y-4">
          <div className="grid gap-4 sm:grid-cols-2">
            <FormField htmlFor="amount" label="Amount" required error={errors.amount?.message}>
              <Input {...register("amount")} inputMode="decimal" autoFocus />
            </FormField>

            <FormField htmlFor="paymentDateUtc" label="Payment date" required>
              <Input {...register("paymentDateUtc")} type="date" />
            </FormField>
          </div>

          <FormField htmlFor="method" label="Method" required>
            <Controller
              name="method"
              control={control}
              render={({ field }) => (
                <Select value={field.value} onValueChange={field.onChange}>
                  <SelectTrigger id="method">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {PAYMENT_METHODS.map((method) => (
                      <SelectItem key={method} value={method}>
                        {method}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
            />
          </FormField>

          <FormField htmlFor="invoiceReference" label="Invoice reference" hint="Optional">
            <Input {...register("invoiceReference")} placeholder="e.g. INV-2044" />
          </FormField>

          <FormField htmlFor="notes" label="Notes" hint="Optional">
            <Input {...register("notes")} />
          </FormField>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => close(false)} disabled={recordPayment.isPending}>
              Cancel
            </Button>
            <Button type="submit" loading={recordPayment.isPending}>
              Record payment
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
