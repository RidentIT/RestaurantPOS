import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import type { User } from "@/entities/user";
import {
  Alert,
  AlertDescription,
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  FormField,
  PasswordInput,
} from "@/shared/ui";
import { useUserMutations } from "../model/useUsers";
import { PASSWORD_HINT, ResetPasswordForm, resetPasswordSchema } from "../model/userSchema";

export interface ResetPasswordDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  user: User;
}

/**
 * Sets a temporary password for a staff account on an administrator's behalf.
 *
 * The user is forced to choose their own password at their next sign-in, and every session
 * they currently hold open is ended — this is meant for "I forgot my password", not a way to
 * quietly access someone else's account.
 */
export function ResetPasswordDialog({ open, onOpenChange, user }: ResetPasswordDialogProps) {
  const { resetPassword } = useUserMutations();

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<ResetPasswordForm>({
    resolver: zodResolver(resetPasswordSchema),
    defaultValues: { newPassword: "" },
  });

  const close = (isOpen: boolean) => {
    if (!isOpen) reset({ newPassword: "" });
    onOpenChange(isOpen);
  };

  const onSubmit = handleSubmit(async ({ newPassword }) => {
    try {
      await resetPassword.mutateAsync({ id: user.id, newPassword });
      toast.success(`${user.fullName}'s password was reset. They must choose a new one at sign-in.`);
      close(false);
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Something went wrong.");
    }
  });

  return (
    <Dialog open={open} onOpenChange={close}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Reset password</DialogTitle>
          <DialogDescription>
            Set a temporary password for <span className="font-medium">{user.fullName}</span>.
          </DialogDescription>
        </DialogHeader>

        <Alert variant="warning">
          <AlertDescription>
            This immediately signs {user.fullName} out everywhere. They will need the temporary
            password to sign back in, and will be asked to choose their own straight away.
          </AlertDescription>
        </Alert>

        <form onSubmit={onSubmit} className="space-y-4">
          <FormField
            htmlFor="newPassword"
            label="Temporary password"
            required
            hint={PASSWORD_HINT}
            error={errors.newPassword?.message}
          >
            <PasswordInput {...register("newPassword")} autoComplete="new-password" autoFocus />
          </FormField>

          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={() => close(false)}
              disabled={resetPassword.isPending}
            >
              Cancel
            </Button>
            <Button type="submit" loading={resetPassword.isPending}>
              Reset password
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
