import { z } from "zod";

const optionalTrimmed = z.string().trim().max(200).optional().or(z.literal(""));

const optionalNonNegativeInteger = z
  .string()
  .trim()
  .refine((v) => v === "" || (Number.isInteger(Number(v)) && Number(v) >= 0), {
    message: "Enter a whole number of zero or more, or leave blank.",
  });

const nonNegativeInteger = z
  .string()
  .trim()
  .refine((v) => Number.isInteger(Number(v)) && Number(v) >= 0, {
    message: "Enter a whole number of zero or more.",
  });

const optionalNonNegativeNumber = z
  .string()
  .trim()
  .refine((v) => v === "" || (!Number.isNaN(Number(v)) && Number(v) >= 0), {
    message: "Enter a non-negative number, or leave blank.",
  });

export const supplierSchema = z.object({
  name: z.string().trim().min(1, "Name is required.").max(200),
  contactName: optionalTrimmed,
  phone: optionalTrimmed,
  email: z.string().trim().max(200).email("Enter a valid email address.").optional().or(z.literal("")),
  address: optionalTrimmed,
  paymentTermsDays: nonNegativeInteger,
  creditLimit: optionalNonNegativeNumber,
  leadTimeDays: optionalNonNegativeInteger,
});

export type SupplierForm = z.infer<typeof supplierSchema>;

/** Converts a form's empty-string "not set" sentinel to the `null` the API expects. */
export const toNullableString = (value: string | undefined): string | null => (value ? value : null);

export const toNullableNumber = (value: string): number | null => (value === "" ? null : Number(value));
