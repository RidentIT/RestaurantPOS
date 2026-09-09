import { describe, expect, it } from "vitest";
import { menuItemSchema, toPriceNumber } from "@/features/menu-items/model/menuItemSchema";

const valid = {
  name: "Chicken Fried Rice",
  category: "Rice & Curry",
  variants: [{ name: "", price: "850" }],
};

describe("menuItemSchema", () => {
  it("accepts a well-formed submission", () => {
    expect(menuItemSchema.safeParse(valid).success).toBe(true);
  });

  it("requires a name and category", () => {
    expect(menuItemSchema.safeParse({ ...valid, name: "" }).success).toBe(false);
    expect(menuItemSchema.safeParse({ ...valid, category: "" }).success).toBe(false);
  });

  it("requires at least one size", () => {
    expect(menuItemSchema.safeParse({ ...valid, variants: [] }).success).toBe(false);
  });

  it.each(["", "abc", "-1", "-0.01"])("rejects an invalid price: %s", (price) => {
    expect(menuItemSchema.safeParse({ ...valid, variants: [{ name: "", price }] }).success).toBe(false);
  });

  it.each(["0", "850", "12.50"])("accepts a valid non-negative price: %s", (price) => {
    expect(menuItemSchema.safeParse({ ...valid, variants: [{ name: "", price }] }).success).toBe(true);
  });

  it("requires every size to be named once there is more than one", () => {
    const result = menuItemSchema.safeParse({
      ...valid,
      variants: [
        { name: "Normal", price: "650" },
        { name: "", price: "850" },
      ],
    });

    expect(result.success).toBe(false);
  });

  it("accepts multiple sizes when each is named", () => {
    const result = menuItemSchema.safeParse({
      ...valid,
      variants: [
        { name: "Normal", price: "650" },
        { name: "Full", price: "850" },
      ],
    });

    expect(result.success).toBe(true);
  });

  it("rejects two sizes sharing a name, regardless of casing", () => {
    const result = menuItemSchema.safeParse({
      ...valid,
      variants: [
        { name: "Full", price: "650" },
        { name: "FULL", price: "850" },
      ],
    });

    expect(result.success).toBe(false);
  });
});

describe("toPriceNumber", () => {
  it("converts the form's string price to a number", () => {
    expect(toPriceNumber("850")).toBe(850);
    expect(toPriceNumber("12.5")).toBe(12.5);
  });
});
