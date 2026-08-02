import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { useAuth, useChangePassword } from "@/features/auth";
import { ChangePasswordForm, PASSWORD_HINT, changePasswordSchema } from "@/features/users/model/userSchema";
import { toApiError } from "@/shared/api/problem";
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  FormField,
  Input,
} from "@/shared/ui";
import { ApprovalPinCard } from "./ApprovalPinCard";

export default function AccountPage() {
  const { user } = useAuth();

  if (!user) return null;

  return (
    <div className="mx-auto max-w-3xl space-y-6 p-8">
      <div>
        <h1 className="text-2xl font-semibold">My account</h1>
        <p className="text-sm text-muted-foreground">
          Manage your sign-in details{user.role === "Admin" ? " and approval PIN" : ""}.
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Profile</CardTitle>
        </CardHeader>
        <CardContent className="grid gap-4 sm:grid-cols-2">
          <div>
            <p className="text-xs uppercase tracking-wide text-muted-foreground">Full name</p>
            <p className="font-medium">{user.fullName}</p>
          </div>
          <div>
            <p className="text-xs uppercase tracking-wide text-muted-foreground">Username</p>
            <p className="font-medium">@{user.username}</p>
          </div>
          <div>
            <p className="text-xs uppercase tracking-wide text-muted-foreground">Email</p>
            <p className="font-medium">{user.email ?? "—"}</p>
          </div>
          <div>
            <p className="text-xs uppercase tracking-wide text-muted-foreground">Role</p>
            <Badge variant={user.role === "Admin" ? "default" : "secondary"}>{user.role}</Badge>
          </div>
        </CardContent>
      </Card>

      <ChangePasswordCard />

      {user.role === "Admin" && <ApprovalPinCard />}
    </div>
  );
}

function ChangePasswordCard() {
  const changePassword = useChangePassword();

  const {
    register,
    handleSubmit,
    reset,
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
      reset();
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
    <Card>
      <CardHeader>
        <CardTitle>Change password</CardTitle>
        <CardDescription>This signs you out on every other device you're signed in on.</CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={onSubmit} className="grid gap-4 sm:grid-cols-3" noValidate>
          <FormField
            htmlFor="currentPassword"
            label="Current password"
            required
            error={errors.currentPassword?.message}
          >
            <Input {...register("currentPassword")} type="password" autoComplete="current-password" />
          </FormField>

          <FormField
            htmlFor="newPassword"
            label="New password"
            required
            hint={PASSWORD_HINT}
            error={errors.newPassword?.message}
          >
            <Input {...register("newPassword")} type="password" autoComplete="new-password" />
          </FormField>

          <FormField
            htmlFor="confirmPassword"
            label="Confirm new password"
            required
            error={errors.confirmPassword?.message}
          >
            <Input {...register("confirmPassword")} type="password" autoComplete="new-password" />
          </FormField>

          <div className="sm:col-span-3">
            <Button type="submit" loading={changePassword.isPending}>
              Update password
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}
