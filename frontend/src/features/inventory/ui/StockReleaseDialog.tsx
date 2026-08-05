import { useState } from "react";
import { useForm } from "react-hook-form";
import { toast } from "sonner";
import type { StockLineInput } from "@/entities/inventory";
import type { RawMaterial } from "@/entities/raw-material";
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
  Input,
} from "@/shared/ui";
import { useInventoryMutations } from "../model/useInventory";
import { StockLinesEditor } from "./StockLinesEditor";

export interface StockReleaseDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  rawMaterials: RawMaterial[];
}

interface FormValues {
  pin: string;
  notes: string;
}

/**
 * Releases stock from the Main Store to the Kitchen. An administrator's approval PIN travels in
 * the same request — there is no separate pending/approve step, so the release only ever exists
 * already authorised.
 */
export function StockReleaseDialog({ open, onOpenChange, rawMaterials }: StockReleaseDialogProps) {
  const { createRelease } = useInventoryMutations();
  const [lines, setLines] = useState<StockLineInput[]>([]);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<FormValues>({ defaultValues: { pin: "", notes: "" } });

  const close = (isOpen: boolean) => {
    if (!isOpen) {
      reset({ pin: "", notes: "" });
      setLines([]);
    }
    onOpenChange(isOpen);
  };

  const onSubmit = handleSubmit(async (values) => {
    if (lines.length === 0) {
      toast.error("Add at least one raw material.");
      return;
    }

    try {
      const release = await createRelease.mutateAsync({ lines, pin: values.pin, notes: values.notes.trim() || null });
      toast.success(`Release approved by ${release.approvedByName}.`);
      close(false);
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Something went wrong.");
    }
  });

  return (
    <Dialog open={open} onOpenChange={close}>
      <DialogContent className="max-w-xl">
        <DialogHeader>
          <DialogTitle>Release stock to the kitchen</DialogTitle>
          <DialogDescription>An administrator's approval PIN is required to authorise this.</DialogDescription>
        </DialogHeader>

        <form onSubmit={onSubmit} className="space-y-4">
          <div>
            <p className="mb-2 text-sm font-medium">Raw materials to release</p>
            <StockLinesEditor
              rawMaterials={rawMaterials}
              lines={lines}
              onChange={setLines}
              emptyMessage="Add the raw materials the kitchen needs, with how much of each."
            />
          </div>

          <FormField htmlFor="notes" label="Notes" hint="Optional, e.g. what this release is for.">
            <Input {...register("notes")} placeholder="e.g. Morning prep" />
          </FormField>

          <Alert variant="info">
            <AlertDescription>An administrator must type their PIN to approve this release.</AlertDescription>
          </Alert>

          <FormField htmlFor="pin" label="Approval PIN" required error={errors.pin?.message}>
            <Input
              {...register("pin", { required: "An approval PIN is required." })}
              inputMode="numeric"
              maxLength={4}
              placeholder="4-digit PIN"
              autoComplete="off"
            />
          </FormField>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => close(false)} disabled={createRelease.isPending}>
              Cancel
            </Button>
            <Button type="submit" loading={createRelease.isPending}>
              Release stock
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
