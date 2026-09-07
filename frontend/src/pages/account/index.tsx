import { zodResolver } from "@hookform/resolvers/zod";
import { Pencil, X } from "lucide-react";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { useAuth, useChangePassword, useUpdateProfile } from "@/features/auth";
import {
  ChangePasswordForm,
  PASSWORD_HINT,
  UpdateProfileForm,
  changePasswordSchema,
  toNullableEmail,
  updateProfileSchema,
} from "@/features/users/model/userSchema";
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
  PasswordInput,
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

      <ProfileCard />

      <ChangePasswordCard />

      {user.role === "Admin" && <ApprovalPinCard />}
    </div>
  );
}

function ProfileCard() {
  const { user } = useAuth();
  const updateProfile = useUpdateProfile();
  const [isEditing, setIsEditing] = useState(false);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<UpdateProfileForm>({
    resolver: zodResolver(updateProfileSchema),
    defaultValues: { fullName: user?.fullName ?? "", email: user?.email ?? "" },
  });

  if (!user) return null;

  const startEditing = () => {
    reset({ fullName: user.fullName, email: user.email ?? "" });
    setIsEditing(true);
  };

  const onSubmit = handleSubmit(async ({ fullName, email }) => {
    try {
      await updateProfile.mutateAsync({ fullName, email: toNullableEmail(email) });
      toast.success("Your profile has been updated.");
      setIsEditing(false);
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  });

  return (
    <Card>
      <CardHeader className="flex-row items-center justify-between space-y-0">
        <CardTitle>Profile</CardTitle>
        {!isEditing && (
          <Button type="button" variant="ghost" size="icon" onClick={startEditing} title="Edit profile">
            <Pencil className="size-4" />
          </Button>
        )}
      </CardHeader>
      <CardContent>
        {isEditing ? (
          <form onSubmit={onSubmit} className="grid gap-4 sm:grid-cols-2" noValidate>
            <FormField htmlFor="fullName" label="Full name" required error={errors.fullName?.message}>
              <Input {...register("fullName")} autoFocus />
            </FormField>

            <FormField
              htmlFor="email"
              label="Email"
              hint="Optional — not required for floor staff."
              error={errors.email?.message}
            >
              <Input {...register("email")} type="email" />
            </FormField>

            <div className="flex gap-2 sm:col-span-2">
              <Button type="submit" loading={updateProfile.isPending}>
                Save changes
              </Button>
              <Button
                type="button"
                variant="outline"
                onClick={() => setIsEditing(false)}
                disabled={updateProfile.isPending}
              >
                <X className="size-4" /> Cancel
              </Button>
            </div>
          </form>
        ) : (
          <div className="grid gap-4 sm:grid-cols-2">
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
          </div>
        )}
      </CardContent>
    </Card>
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
            <PasswordInput {...register("currentPassword")} autoComplete="current-password" />
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
