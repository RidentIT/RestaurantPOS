import { describe, it, expect } from "vitest";
import { formatCurrency } from "@/shared/utils";

describe("Shared Utilities Unit Tests", () => {
  it("formats currency correctly", () => {
    expect(formatCurrency(12.5)).toBe("$12.50");
  });
});
