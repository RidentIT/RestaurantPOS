import { zodResolver } from "@hookform/resolvers/zod";
import { Controller, useForm } from "react-hook-form";
import { toast } from "sonner";
import type { ModuleDescriptor, ModuleKey, User } from "@/entities/user";
import { toApiError } from "@/shared/api/problem";
import {
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  FormField,
  Input,
  PasswordInput,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/shared/ui";
import { useUserMutations } from "../model/useUsers";
import type { CreateUserForm, UpdateUserForm } from "../model/userSchema";
import {
  PASSWORD_HINT,
  createUserSchema,
  toNullableEmail,
  updateUserSchema,
} from "../model/userSchema";
import { ModulePermissionPicker } from "./ModulePermissionPicker";

export interface UserFormDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /** The account being edited, or omitted to create a new one. */
  user?: User;
  catalog: ModuleDescriptor[];
}

/**
 * Creates a staff account, or edits an existing one's profile, username, role and module grants.
 *
 * Rendered as two distinct form bodies rather than one form with optional fields: creation and
 * editing have genuinely different shapes (a starting password only makes sense once), and
 * giving each its own `useForm` keeps both strongly typed instead of forcing `react-hook-form`
 * through a union schema.
 */
export function UserFormDialog({ open, onOpenChange, user, catalog }: UserFormDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl">
        {user ? (
          <EditUserFormBody key={user.id} user={user} catalog={catalog} onDone={() => onOpenChange(false)} />
        ) : (
          <CreateUserFormBody key="create" catalog={catalog} onDone={() => onOpenChange(false)} />
        )}
      </DialogContent>
    </Dialog>
  );
}

function CreateUserFormBody({ catalog, onDone }: { catalog: ModuleDescriptor[]; onDone: () => void }) {
  const { create } = useUserMutations();

  const {
    register,
    handleSubmit,
    control,
    watch,
    formState: { errors },
  } = useForm<CreateUserForm>({
    resolver: zodResolver(createUserSchema),
    defaultValues: { username: "", fullName: "", email: "", password: "", role: "User", modules: [] },
  });

  const role = watch("role");

  const onSubmit = handleSubmit(async (values) => {
    try {
      await create.mutateAsync({
        username: values.username,
        fullName: values.fullName,
        email: toNullableEmail(values.email),
        password: values.password,
        role: values.role,
        modules: values.modules as ModuleKey[],
      });
      toast.success(`${values.fullName} was added. They'll set their own password at first sign-in.`);
      onDone();
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Something went wrong.");
    }
  });

  return (
    <>
      <DialogHeader>
        <DialogTitle>Add staff account</DialogTitle>
        <DialogDescription>
          The new account must choose its own password the first time it signs in.
        </DialogDescription>
      </DialogHeader>

      <form onSubmit={onSubmit} className="space-y-5">
        <div className="grid gap-4 sm:grid-cols-2">
          <FormField
            htmlFor="username"
            label="Username"
            required
            hint="Letters, digits, dots, hyphens and underscores only."
            error={errors.username?.message}
          >
            <Input {...register("username")} autoComplete="off" placeholder="cashier01" />
          </FormField>

          <FormField htmlFor="fullName" label="Full name" required error={errors.fullName?.message}>
            <Input {...register("fullName")} placeholder="Ravi Kumar" />
          </FormField>

          <FormField
            htmlFor="email"
            label="Email"
            hint="Optional — not required for floor staff."
            error={errors.email?.message}
          >
            <Input {...register("email")} type="email" placeholder="ravi@srilakshmi.lk" />
          </FormField>

          <FormField htmlFor="role" label="Role" required>
            <Controller
              name="role"
              control={control}
              render={({ field }) => (
                <Select value={field.value} onValueChange={field.onChange}>
                  <SelectTrigger id="role">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="User">User</SelectItem>
                    <SelectItem value="Admin">Admin</SelectItem>
                  </SelectContent>
                </Select>
              )}
            />
          </FormField>

          <FormField
            htmlFor="password"
            label="Starting password"
            required
            hint={PASSWORD_HINT}
            error={errors.password?.message}
          >
            <PasswordInput {...register("password")} autoComplete="new-password" />
          </FormField>
        </div>

        <div>
          <p className="mb-3 text-sm font-medium">Module access</p>
          <Controller
            name="modules"
            control={control}
            render={({ field }) => (
              <ModulePermissionPicker
                catalog={catalog}
                selected={field.value ?? []}
                onChange={field.onChange}
                isAdmin={role === "Admin"}
              />
            )}
          />
        </div>

        <DialogFooter>
          <Button type="button" variant="outline" onClick={onDone} disabled={create.isPending}>
            Cancel
          </Button>
          <Button type="submit" loading={create.isPending}>
            Create account
          </Button>
        </DialogFooter>
      </form>
    </>
  );
}

function EditUserFormBody({
  user,
  catalog,
  onDone,
}: {
  user: User;
  catalog: ModuleDescriptor[];
  onDone: () => void;
}) {
  const { update } = useUserMutations();

  const {
    register,
    handleSubmit,
    control,
    watch,
    setError,
    formState: { errors },
  } = useForm<UpdateUserForm>({
    resolver: zodResolver(updateUserSchema),
    defaultValues: {
      username: user.username,
      fullName: user.fullName,
      email: user.email ?? "",
      role: user.role,
      modules: user.modules,
    },
  });

  const role = watch("role");

  const onSubmit = handleSubmit(async (values) => {
    try {
      await update.mutateAsync({
        id: user.id,
        payload: {
          username: values.username,
          fullName: values.fullName,
          email: toNullableEmail(values.email),
          role: values.role,
          modules: values.modules as ModuleKey[],
        },
      });
      toast.success(`${values.fullName}'s account was updated.`);
      onDone();
    } catch (error) {
      const apiError = toApiError(error);

      if (apiError.code === "User.UsernameTaken") {
        setError("username", { message: apiError.message });
      } else {
        toast.error(apiError.message);
      }
    }
  });

  return (
    <>
      <DialogHeader>
        <DialogTitle>Edit staff account</DialogTitle>
        <DialogDescription>
          Changes to username, role or modules take effect the next time this user signs in.
        </DialogDescription>
      </DialogHeader>

      <form onSubmit={onSubmit} className="space-y-5">
        <div className="grid gap-4 sm:grid-cols-2">
          <FormField
            htmlFor="username"
            label="Username"
            required
            hint="Letters, digits, dots, hyphens and underscores only."
            error={errors.username?.message}
          >
            <Input {...register("username")} autoComplete="off" placeholder="cashier01" />
          </FormField>

          <FormField htmlFor="fullName" label="Full name" required error={errors.fullName?.message}>
            <Input {...register("fullName")} placeholder="Ravi Kumar" />
          </FormField>

          <FormField
            htmlFor="email"
            label="Email"
            hint="Optional — not required for floor staff."
            error={errors.email?.message}
          >
            <Input {...register("email")} type="email" placeholder="ravi@srilakshmi.lk" />
          </FormField>

          <FormField htmlFor="role" label="Role" required>
            <Controller
              name="role"
              control={control}
              render={({ field }) => (
                <Select
                  value={field.value}
                  onValueChange={field.onChange}
                  disabled={user.isSystemAdmin}
                >
                  <SelectTrigger id="role">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="User">User</SelectItem>
                    <SelectItem value="Admin">Admin</SelectItem>
                  </SelectContent>
                </Select>
              )}
            />
          </FormField>
        </div>

        <div>
          <p className="mb-3 text-sm font-medium">Module access</p>
          <Controller
            name="modules"
            control={control}
            render={({ field }) => (
              <ModulePermissionPicker
                catalog={catalog}
                selected={field.value ?? []}
                onChange={field.onChange}
                isAdmin={role === "Admin"}
              />
            )}
          />
        </div>

        <DialogFooter>
          <Button type="button" variant="outline" onClick={onDone} disabled={update.isPending}>
            Cancel
          </Button>
          <Button type="submit" loading={update.isPending}>
            Save changes
          </Button>
        </DialogFooter>
      </form>
    </>
  );
}
