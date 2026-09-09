import { z } from "zod";

const priceSchema = z
  .string()
  .trim()
  .refine((v) => v !== "" && !Number.isNaN(Number(v)) && Number(v) >= 0, {
    message: "Enter a valid, non-negative price.",
  });

export const menuItemVariantSchema = z.object({
  /** Set for an existing size being edited; absent for a new one. */
  id: z.string().optional(),
  name: z.string().trim().max(40, "Keep it under 40 characters."),
  price: priceSchema,
});

export const menuItemSchema = z
  .object({
    name: z.string().trim().min(1, "Name is required.").max(150),
    category: z.string().trim().min(1, "Category is required.").max(80),
    variants: z.array(menuItemVariantSchema).min(1, "Add at least one size."),
  })
  .refine((form) => form.variants.length === 1 || form.variants.every((v) => v.name !== ""), {
    message: "Every size needs its own name once there is more than one size.",
    path: ["variants"],
  })
  .refine(
    (form) => {
      const named = form.variants.map((v) => v.name.trim().toLowerCase()).filter((n) => n !== "");
      return new Set(named).size === named.length;
    },
    { message: "Two sizes on the same item cannot share a name.", path: ["variants"] },
  );

export type MenuItemForm = z.infer<typeof menuItemSchema>;

/** Converts a form price field to the number the API expects. */
export const toPriceNumber = (value: string): number => Number(value);
