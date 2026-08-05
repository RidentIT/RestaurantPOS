import { z } from "zod";

export const menuItemSchema = z.object({
  name: z.string().trim().min(1, "Name is required.").max(150),
  category: z.string().trim().min(1, "Category is required.").max(80),
  price: z
    .string()
    .trim()
    .refine((v) => v !== "" && !Number.isNaN(Number(v)) && Number(v) >= 0, {
      message: "Enter a valid, non-negative price.",
    }),
});

export type MenuItemForm = z.infer<typeof menuItemSchema>;

/** Converts the form's string price field to the number the API expects. */
export const toPriceNumber = (value: string): number => Number(value);
