import { zodResolver } from "@hookform/resolvers/zod";
import { KeyRound } from "lucide-react";
import { useForm } from "react-hook-form";
import { useNavigate } from "react-router-dom";
import { toast } from "sonner";
import { useAuth, useChangePassword } from "@/features/auth";
import { changePasswordSchema, ChangePasswordForm, PASSWORD_HINT } from "@/features/users/model/userSchema";
import { toApiError } from "@/shared/api/problem";
import {
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  FormField,
  PasswordInput,
} from "@/shared/ui";

/**
 * Forced for any account with a pending password change (the seeded admin, anyone just
 * created, anyone whose password was just reset); reachable voluntarily otherwise.
 */
export default function ChangePasswordPage() {
  const { mustChangePassword } = useAuth();
  const changePassword = useChangePassword();
  const navigate = useNavigate();

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<ChangePasswordForm>({
    resolver: zodResolver(changePasswordSchema),
    defaultValues: { currentPassword: "", newPassword: "", confirmPassword: "" },
  });

  const onSubmit = handleSubmit(async ({ currentPassword, newPassword }) => {
    try {
      await changePassword.mutateAsync({ currentPassword, newPassword });
      toast.success("Your password has been updated.");

      // This screen sits outside the app shell (no nav chrome), so a forced change must send
      // the user on into the app itself rather than leaving them stranded here.
      if (mustChangePassword) {
        navigate("/", { replace: true });
      }
    } catch (error) {
      const apiError = toApiError(error);

      if (apiError.code === "Auth.PasswordMismatch") {
        setError("currentPassword", { message: apiError.message });
      } else {
        setError("newPassword", { message: apiError.message });
      }
    }
  });

  return (
    <div className="flex min-h-screen items-center justify-center bg-muted/40 p-4">
      <Card className="w-full max-w-sm">
        <CardHeader className="items-center text-center">
          <div className="mb-2 flex size-12 items-center justify-center rounded-full bg-primary/10 text-primary">
            <KeyRound className="size-6" />
          </div>
          <CardTitle>{mustChangePassword ? "Choose a new password" : "Change your password"}</CardTitle>
          <CardDescription>
            {mustChangePassword
              ? "For your security, you must set your own password before continuing."
              : "Signing out everywhere else you're currently signed in."}
          </CardDescription>
        </CardHeader>

        <CardContent>
          <form onSubmit={onSubmit} className="space-y-4" noValidate>
            <FormField
              htmlFor="currentPassword"
              label="Current password"
              required
              error={errors.currentPassword?.message}
            >
              <PasswordInput {...register("currentPassword")} autoComplete="current-password" autoFocus />
            </FormField>

            <FormField
              htmlFor="newPassword"
              label="New password"
              required
              hint={PASSWORD_HINT}
              error={errors.newPassword?.message}
            >
              <PasswordInput {...register("newPassword")} autoComplete="new-password" />
            </FormField>

            <FormField
              htmlFor="confirmPassword"
              label="Confirm new password"
              required
              error={errors.confirmPassword?.message}
            >
              <PasswordInput {...register("confirmPassword")} autoComplete="new-password" />
            </FormField>

            <Button type="submit" className="w-full" size="lg" loading={changePassword.isPending}>
              Update password
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
