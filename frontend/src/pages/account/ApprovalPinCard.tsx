import { useState } from "react";
import { Copy, KeyRound, ShieldCheck } from "lucide-react";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import { useApprovalPin, useAuth } from "@/features/auth";
import { toApiError } from "@/shared/api/problem";
import {
  Alert,
  AlertDescription,
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

interface PinFormValues {
  currentPassword: string;
  pin: string;
}

/**
 * Lets an administrator set, regenerate or remove their 4-digit approval PIN — the code other
 * staff will ask them to type in at the till to authorise things like an order cancellation.
 */
export function ApprovalPinCard() {
  const { user } = useAuth();
  const [revealedPin, setRevealedPin] = useState<string | null>(null);

  const { set, clear } = useApprovalPin();

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<PinFormValues>({ defaultValues: { currentPassword: "", pin: "" } });

  if (!user || user.role !== "Admin") return null;

  const onGenerate = handleSubmit(async ({ currentPassword }) => {
    try {
      const result = await set.mutateAsync({ currentPassword, pin: null });
      setRevealedPin(result.pin);
      reset();
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  });

  const onSetChosen = handleSubmit(async ({ currentPassword, pin }) => {
    if (!/^\d{4}$/.test(pin)) {
      toast.error("The approval PIN must be exactly 4 digits.");
      return;
    }

    try {
      await set.mutateAsync({ currentPassword, pin });
      setRevealedPin(pin);
      reset();
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  });

  const onClear = async () => {
    try {
      await clear.mutateAsync();
      setRevealedPin(null);
      toast.success("Your approval PIN was removed.");
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  };

  const copyPin = async () => {
    if (!revealedPin) return;

    await navigator.clipboard.writeText(revealedPin);
    toast.success("PIN copied to clipboard.");
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <ShieldCheck className="size-5 text-primary" />
          Approval PIN
        </CardTitle>
        <CardDescription>
          A 4-digit PIN staff will ask you to enter at the till to authorise actions like
          cancelling an order. Only you know it — it is stored as a one-way hash, never in
          plain text.
        </CardDescription>
      </CardHeader>

      <CardContent className="space-y-4">
        {revealedPin && (
          <Alert variant="success">
            <KeyRound />
            <AlertDescription className="flex items-center justify-between gap-3">
              <span>
                Your new PIN is <span className="font-mono text-base font-bold">{revealedPin}</span>.
                Make a note of it now — it won't be shown again.
              </span>
              <Button type="button" variant="ghost" size="icon" onClick={copyPin} title="Copy">
                <Copy className="size-4" />
              </Button>
            </AlertDescription>
          </Alert>
        )}

        <p className="text-sm">
          Status:{" "}
          <span className="font-medium">
            {user.hasApprovalPin ? "A PIN is currently set." : "No PIN has been set yet."}
          </span>
        </p>

        <div className="grid gap-4 sm:grid-cols-2">
          <FormField
            htmlFor="pinCurrentPassword"
            label="Your current password"
            required
            error={errors.currentPassword?.message}
            className="sm:col-span-2"
          >
            <PasswordInput {...register("currentPassword", { required: true })} autoComplete="current-password" />
          </FormField>

          <FormField
            htmlFor="pinCode"
            label="Choose a specific PIN (optional)"
            hint="Leave blank to generate a random one."
            error={errors.pin?.message}
          >
            <Input
              {...register("pin")}
              inputMode="numeric"
              maxLength={4}
              placeholder="e.g. 4821"
            />
          </FormField>
        </div>

        <div className="flex flex-wrap gap-2">
          <Button type="button" onClick={onGenerate} loading={set.isPending}>
            Generate random PIN
          </Button>
          <Button type="button" variant="outline" onClick={onSetChosen} disabled={set.isPending}>
            Use PIN above
          </Button>
          {user.hasApprovalPin && (
            <Button type="button" variant="destructive" onClick={onClear} loading={clear.isPending}>
              Remove PIN
            </Button>
          )}
        </div>
      </CardContent>
    </Card>
  );
}
