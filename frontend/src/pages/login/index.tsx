import { useState } from "react";
import { AlertCircle } from "lucide-react";
import { useForm } from "react-hook-form";
import { useLogin } from "@/features/auth";
import { settingsApi, useBranding } from "@/features/settings";
import { toApiError } from "@/shared/api/problem";
import {
  Alert,
  AlertDescription,
  BrandMark,
  Button,
  Card,
  CardContent,
  FormField,
  Input,
  PasswordInput,
} from "@/shared/ui";

interface LoginFormValues {
  username: string;
  password: string;
}

export default function LoginPage() {
  const login = useLogin();
  const { data: branding } = useBranding();
  const [formError, setFormError] = useState<string | null>(null);
  const name = branding?.name ?? "";

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormValues>({ defaultValues: { username: "", password: "" } });

  const onSubmit = handleSubmit(async ({ username, password }) => {
    setFormError(null);

    try {
      await login.mutateAsync({ username, password });
    } catch (error) {
      setFormError(toApiError(error).message);
    }
  });

  return (
    <div className="flex min-h-screen items-center justify-center bg-muted/40 p-4">
      <Card className="w-full max-w-sm">
        <CardContent className="pt-8">
          <div className="mb-8 text-center">
            <BrandMark
              src={name ? settingsApi.logoUrl() : undefined}
              name={name}
              className="mx-auto mb-4 size-12 text-lg"
            />
            <h1 className="text-xl font-semibold">{name}</h1>
            <p className="mt-1 text-sm text-muted-foreground">Sign in to the POS terminal</p>
          </div>

          <form onSubmit={onSubmit} className="space-y-4" noValidate>
            {formError && (
              <Alert variant="destructive">
                <AlertCircle />
                <AlertDescription>{formError}</AlertDescription>
              </Alert>
            )}

            <FormField htmlFor="username" label="Username" required error={errors.username?.message}>
              <Input
                {...register("username", { required: "Username is required." })}
                autoComplete="username"
                autoFocus
              />
            </FormField>

            <FormField htmlFor="password" label="Password" required error={errors.password?.message}>
              <PasswordInput
                {...register("password", { required: "Password is required." })}
                autoComplete="current-password"
              />
            </FormField>

            <Button type="submit" className="w-full" size="lg" loading={login.isPending}>
              Sign in
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
