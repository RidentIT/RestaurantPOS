import { describe, expect, it } from "vitest";
import {
  approvalPinSchema,
  changePasswordSchema,
  createUserSchema,
  toNullableEmail,
  updateUserSchema,
} from "@/features/users/model/userSchema";

const validCreate = {
  username: "cashier01",
  fullName: "Ravi Kumar",
  email: "",
  password: "Cashier@2026",
  role: "User" as const,
  modules: ["PosBilling"],
};

describe("createUserSchema", () => {
  it("accepts a well-formed submission", () => {
    expect(createUserSchema.safeParse(validCreate).success).toBe(true);
  });

  it.each(["ab", "has spaces", "bad!char"])("rejects a malformed username %s", (username) => {
    const result = createUserSchema.safeParse({ ...validCreate, username });
    expect(result.success).toBe(false);
  });

  it.each(["short1A", "alllowercase1", "ALLUPPERCASE1", "NoDigitsHere"])(
    "rejects a password that fails the policy: %s",
    (password) => {
      const result = createUserSchema.safeParse({ ...validCreate, password });
      expect(result.success).toBe(false);
    },
  );

  it("allows an empty email but rejects a malformed one", () => {
    expect(createUserSchema.safeParse({ ...validCreate, email: "" }).success).toBe(true);
    expect(createUserSchema.safeParse({ ...validCreate, email: "not-an-email" }).success).toBe(false);
    expect(createUserSchema.safeParse({ ...validCreate, email: "ravi@srilakshmi.lk" }).success).toBe(true);
  });
});

describe("updateUserSchema", () => {
  it("does not require a username or password", () => {
    const result = updateUserSchema.safeParse({
      fullName: "Ravi Kumar",
      email: "",
      role: "User",
      modules: [],
    });

    expect(result.success).toBe(true);
  });
});

describe("toNullableEmail", () => {
  it("converts the empty-string sentinel to null and leaves real addresses alone", () => {
    expect(toNullableEmail("")).toBeNull();
    expect(toNullableEmail("ravi@srilakshmi.lk")).toBe("ravi@srilakshmi.lk");
  });
});

describe("changePasswordSchema", () => {
  const base = {
    currentPassword: "Current@2026",
    newPassword: "Brand@2026New",
    confirmPassword: "Brand@2026New",
  };

  it("accepts matching, policy-compliant passwords", () => {
    expect(changePasswordSchema.safeParse(base).success).toBe(true);
  });

  it("rejects when the confirmation does not match", () => {
    const result = changePasswordSchema.safeParse({ ...base, confirmPassword: "Different@2026" });

    expect(result.success).toBe(false);
    expect(result.success ? undefined : result.error.issues[0].path).toEqual(["confirmPassword"]);
  });

  it("rejects reusing the current password as the new one", () => {
    const result = changePasswordSchema.safeParse({
      ...base,
      newPassword: base.currentPassword,
      confirmPassword: base.currentPassword,
    });

    expect(result.success).toBe(false);
    expect(result.success ? undefined : result.error.issues[0].path).toEqual(["newPassword"]);
  });
});

describe("approvalPinSchema", () => {
  it.each(["4821", "0000", "9999"])("accepts a 4-digit PIN: %s", (pin) => {
    expect(approvalPinSchema.safeParse(pin).success).toBe(true);
  });

  it.each(["123", "12345", "abcd", ""])("rejects an invalid PIN: %s", (pin) => {
    expect(approvalPinSchema.safeParse(pin).success).toBe(false);
  });
});
