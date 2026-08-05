import { describe, expect, it } from "vitest";
import { rawMaterialSchema, toNullableThreshold } from "@/features/raw-materials/model/rawMaterialSchema";

const valid = {
  name: "Rice",
  unitOfMeasurement: "Kilogram",
  mainStoreReorderLevel: "",
  kitchenParLevel: "",
};

describe("rawMaterialSchema", () => {
  it("accepts a submission with no thresholds set", () => {
    expect(rawMaterialSchema.safeParse(valid).success).toBe(true);
  });

  it("accepts thresholds when provided", () => {
    const result = rawMaterialSchema.safeParse({ ...valid, mainStoreReorderLevel: "10", kitchenParLevel: "2" });
    expect(result.success).toBe(true);
  });

  it("rejects a negative threshold", () => {
    expect(rawMaterialSchema.safeParse({ ...valid, mainStoreReorderLevel: "-1" }).success).toBe(false);
  });

  it("rejects an unrecognised unit of measurement", () => {
    expect(rawMaterialSchema.safeParse({ ...valid, unitOfMeasurement: "Stone" }).success).toBe(false);
  });

  it("requires a name", () => {
    expect(rawMaterialSchema.safeParse({ ...valid, name: "" }).success).toBe(false);
  });
});

describe("toNullableThreshold", () => {
  it("converts the empty-string sentinel to null", () => {
    expect(toNullableThreshold("")).toBeNull();
  });

  it("converts a populated value to a number", () => {
    expect(toNullableThreshold("10")).toBe(10);
  });
});
