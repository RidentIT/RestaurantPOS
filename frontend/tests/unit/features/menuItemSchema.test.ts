import { describe, expect, it } from "vitest";
import { menuItemSchema, toPriceNumber } from "@/features/menu-items/model/menuItemSchema";

const valid = { name: "Chicken Fried Rice", category: "Rice & Curry", price: "850" };

describe("menuItemSchema", () => {
  it("accepts a well-formed submission", () => {
    expect(menuItemSchema.safeParse(valid).success).toBe(true);
  });

  it("requires a name and category", () => {
    expect(menuItemSchema.safeParse({ ...valid, name: "" }).success).toBe(false);
    expect(menuItemSchema.safeParse({ ...valid, category: "" }).success).toBe(false);
  });

  it.each(["", "abc", "-1", "-0.01"])("rejects an invalid price: %s", (price) => {
    expect(menuItemSchema.safeParse({ ...valid, price }).success).toBe(false);
  });

  it.each(["0", "850", "12.50"])("accepts a valid non-negative price: %s", (price) => {
    expect(menuItemSchema.safeParse({ ...valid, price }).success).toBe(true);
  });
});

describe("toPriceNumber", () => {
  it("converts the form's string price to a number", () => {
    expect(toPriceNumber("850")).toBe(850);
    expect(toPriceNumber("12.5")).toBe(12.5);
  });
});
