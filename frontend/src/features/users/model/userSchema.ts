import { z } from "zod";

/**
 * Mirrors the server's password policy so the user is told what is wrong before a round trip.
 * The server remains the authority; this only spares them a failed submit.
 */
export const passwordSchema = z
  .string()
  .min(8, "Password must be at least 8 characters.")
  .max(128, "Password cannot exceed 128 characters.")
  .regex(/[A-Z]/, "Password must contain an upper-case letter.")
  .regex(/[a-z]/, "Password must contain a lower-case letter.")
  .regex(/[0-9]/, "Password must contain a digit.");

export const PASSWORD_HINT =
  "At least 8 characters, with an upper-case letter, a lower-case letter and a digit.";

const usernameSchema = z
  .string()
  .trim()
  .toLowerCase()
  .min(3, "Username must be at least 3 characters.")
  .max(32, "Username cannot exceed 32 characters.")
  .regex(
    /^[a-z0-9._-]+$/,
    "Use only letters, digits, dots, hyphens and underscores.",
  );

// Kept as a plain string (not transformed to null) so the input/output types stay identical —
// react-hook-form's zodResolver requires that when useForm is given a single type argument.
// Callers convert "" to null with `toNullableEmail` immediately before sending it to the API.
const emailSchema = z
  .string()
  .trim()
  .max(200)
  .refine((value) => value === "" || z.string().email().safeParse(value).success, {
    message: "Enter a valid email address.",
  });

/** Converts the form's empty-string "no email" sentinel to the `null` the API expects. */
export const toNullableEmail = (value: string): string | null => (value === "" ? null : value);

const baseUserFields = {
  fullName: z.string().trim().min(1, "Full name is required.").max(120),
  email: emailSchema,
  role: z.enum(["Admin", "User"]),
  modules: z.array(z.string()),
};

/** New account: a username and starting password are required. */
export const createUserSchema = z.object({
  ...baseUserFields,
  username: usernameSchema,
  password: passwordSchema,
});

/** Existing account: the username is immutable and the password is changed separately. */
export const updateUserSchema = z.object(baseUserFields);

export const resetPasswordSchema = z.object({
  newPassword: passwordSchema,
});

export const changePasswordSchema = z
  .object({
    currentPassword: z.string().min(1, "Your current password is required."),
    newPassword: passwordSchema,
    confirmPassword: z.string().min(1, "Please confirm your new password."),
  })
  .refine((data) => data.newPassword === data.confirmPassword, {
    message: "The two passwords do not match.",
    path: ["confirmPassword"],
  })
  .refine((data) => data.newPassword !== data.currentPassword, {
    message: "The new password must be different from your current one.",
    path: ["newPassword"],
  });

export const approvalPinSchema = z
  .string()
  .regex(/^[0-9]{4}$/, "The approval PIN must be exactly 4 digits.");

export type CreateUserForm = z.infer<typeof createUserSchema>;
export type UpdateUserForm = z.infer<typeof updateUserSchema>;
export type ResetPasswordForm = z.infer<typeof resetPasswordSchema>;
export type ChangePasswordForm = z.infer<typeof changePasswordSchema>;
