import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import type { Supplier } from "@/entities/supplier";
import {
  Button,
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  FormField,
  Input,
} from "@/shared/ui";
import { toApiError } from "@/shared/api/problem";
import { useSupplierMutations } from "../model/useSuppliers";
import { SupplierForm, supplierSchema, toNullableNumber, toNullableString } from "../model/supplierSchema";

export interface SupplierFormDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  supplier?: Supplier;
}

/** Creates or edits a supplier: contact details, credit terms and lead time. */
export function SupplierFormDialog({ open, onOpenChange, supplier }: SupplierFormDialogProps) {
  const isEditing = !!supplier;
  const { create, update } = useSupplierMutations();
  const pending = create.isPending || update.isPending;

  const defaults: SupplierForm = {
    name: supplier?.name ?? "",
    contactName: supplier?.contactName ?? "",
    phone: supplier?.phone ?? "",
    email: supplier?.email ?? "",
    address: supplier?.address ?? "",
    paymentTermsDays: String(supplier?.paymentTermsDays ?? 0),
    creditLimit: supplier?.creditLimit != null ? String(supplier.creditLimit) : "",
    leadTimeDays: supplier?.leadTimeDays != null ? String(supplier.leadTimeDays) : "",
  };

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<SupplierForm>({ resolver: zodResolver(supplierSchema), defaultValues: defaults });

  const close = (isOpen: boolean) => {
    if (!isOpen) reset(defaults);
    onOpenChange(isOpen);
  };

  const onSubmit = handleSubmit(async (values) => {
    const payload = {
      name: values.name,
      contactName: toNullableString(values.contactName),
      phone: toNullableString(values.phone),
      email: toNullableString(values.email),
      address: toNullableString(values.address),
      paymentTermsDays: Number(values.paymentTermsDays),
      creditLimit: toNullableNumber(values.creditLimit),
      leadTimeDays: toNullableNumber(values.leadTimeDays),
    };

    try {
      if (isEditing) {
        await update.mutateAsync({ id: supplier.id, payload });
        toast.success(`${values.name} was updated.`);
      } else {
        await create.mutateAsync(payload);
        toast.success(`${values.name} was added.`);
      }
      close(false);
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  });

  return (
    <Dialog open={open} onOpenChange={close}>
      <DialogContent className="max-w-xl">
        <DialogHeader>
          <DialogTitle>{isEditing ? "Edit supplier" : "Add supplier"}</DialogTitle>
        </DialogHeader>

        <form onSubmit={onSubmit} className="space-y-4">
          <FormField htmlFor="name" label="Name" required error={errors.name?.message}>
            <Input {...register("name")} placeholder="ABC Wholesale" autoFocus />
          </FormField>

          <div className="grid gap-4 sm:grid-cols-2">
            <FormField htmlFor="contactName" label="Contact name" error={errors.contactName?.message}>
              <Input {...register("contactName")} placeholder="Mr. Perera" />
            </FormField>

            <FormField htmlFor="phone" label="Phone" error={errors.phone?.message}>
              <Input {...register("phone")} placeholder="0771234567" />
            </FormField>
          </div>

          <FormField htmlFor="email" label="Email" error={errors.email?.message}>
            <Input {...register("email")} type="email" placeholder="contact@supplier.lk" />
          </FormField>

          <FormField htmlFor="address" label="Address" error={errors.address?.message}>
            <Input {...register("address")} placeholder="123 Galle Rd, Colombo" />
          </FormField>

          <div className="grid gap-4 sm:grid-cols-3">
            <FormField
              htmlFor="paymentTermsDays"
              label="Payment terms"
              hint="Days"
              required
              error={errors.paymentTermsDays?.message}
            >
              <Input {...register("paymentTermsDays")} inputMode="numeric" placeholder="0" />
            </FormField>

            <FormField
              htmlFor="creditLimit"
              label="Credit limit"
              hint="Optional"
              error={errors.creditLimit?.message}
            >
              <Input {...register("creditLimit")} inputMode="decimal" placeholder="e.g. 100000" />
            </FormField>

            <FormField
              htmlFor="leadTimeDays"
              label="Lead time"
              hint="Days, optional"
              error={errors.leadTimeDays?.message}
            >
              <Input {...register("leadTimeDays")} inputMode="numeric" placeholder="e.g. 3" />
            </FormField>
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => close(false)} disabled={pending}>
              Cancel
            </Button>
            <Button type="submit" loading={pending}>
              {isEditing ? "Save changes" : "Add supplier"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
