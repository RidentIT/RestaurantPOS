import { z } from "zod";
import { UNITS_OF_MEASUREMENT } from "@/entities/raw-material";

const optionalNonNegativeNumber = z
  .string()
  .trim()
  .refine((v) => v === "" || (!Number.isNaN(Number(v)) && Number(v) >= 0), {
    message: "Enter a non-negative number, or leave blank.",
  });

export const rawMaterialSchema = z.object({
  name: z.string().trim().min(1, "Name is required.").max(150),
  unitOfMeasurement: z.enum(UNITS_OF_MEASUREMENT as [string, ...string[]]),
  mainStoreReorderLevel: optionalNonNegativeNumber,
  kitchenParLevel: optionalNonNegativeNumber,
});

export type RawMaterialForm = z.infer<typeof rawMaterialSchema>;

/** Converts the form's empty-string "no threshold" sentinel to the `null` the API expects. */
export const toNullableThreshold = (value: string): number | null => (value === "" ? null : Number(value));
