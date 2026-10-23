import { useState } from "react";
import { toast } from "sonner";
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
} from "@/shared/ui";
import { toApiError } from "@/shared/api/problem";
import { useShiftMutations } from "../model/useShifts";

export interface CheckInDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

/** Starts a shift. The float counted into the drawer now is what check-out is measured against. */
export function CheckInDialog({ open, onOpenChange }: CheckInDialogProps) {
  const { checkIn } = useShiftMutations();
  const [openingCash, setOpeningCash] = useState("0");

  const amount = Number(openingCash);
  const invalid = openingCash.trim() === "" || Number.isNaN(amount) || amount < 0;

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    if (invalid) return;

    try {
      await checkIn.mutateAsync(amount);
      toast.success("You're checked in. Your shift has started.");
      onOpenChange(false);
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-sm">
        <DialogHeader>
          <DialogTitle>Check in</DialogTitle>
          <DialogDescription>
            Start your shift. Everything you ring up from now until you check out is counted on it,
            and it starts from zero.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={submit} className="space-y-4">
          <FormField
            htmlFor="opening-cash"
            label="Opening cash in the drawer"
            hint="Count the float you were handed. Enter 0 if the drawer is empty."
            required
          >
            <Input
              id="opening-cash"
              type="number"
              inputMode="decimal"
              min={0}
              step="0.01"
              value={openingCash}
              onChange={(event) => setOpeningCash(event.target.value)}
              autoFocus
            />
          </FormField>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={checkIn.isPending}>
              Cancel
            </Button>
            <Button type="submit" loading={checkIn.isPending} disabled={invalid}>
              Check in
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
