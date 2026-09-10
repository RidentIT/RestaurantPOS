import { useEffect, useState } from "react";
import { toast } from "sonner";
import { useStewards } from "@/features/stewards";
import { toApiError } from "@/shared/api/problem";
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
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/shared/ui";

const NONE = "__none__";

export interface StewardDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /** The order's current steward id, or null. */
  currentStewardId: string | null;
  currentStewardName: string | null;
  onAssign: (stewardId: string | null) => Promise<void>;
  pending: boolean;
}

/** Picks the steward serving a table, from the roster an administrator maintains. */
export function StewardDialog({
  open,
  onOpenChange,
  currentStewardId,
  currentStewardName,
  onAssign,
  pending,
}: StewardDialogProps) {
  const { data: stewards, isLoading } = useStewards(true);
  const [selected, setSelected] = useState<string>(currentStewardId ?? NONE);

  useEffect(() => {
    if (open) setSelected(currentStewardId ?? NONE);
  }, [open, currentStewardId]);

  // A steward retired after this order was assigned to them won't be in the active list; keep the
  // name visible so the cashier can see who is on it before changing it.
  const retiredCurrent =
    currentStewardId && !(stewards ?? []).some((s) => s.id === currentStewardId) ? currentStewardName : null;

  const save = async () => {
    try {
      await onAssign(selected === NONE ? null : selected);
      onOpenChange(false);
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-sm">
        <DialogHeader>
          <DialogTitle>Steward</DialogTitle>
          <DialogDescription>Who is serving this table? It shows on the tile, the KOT and the sales report.</DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          {(stewards ?? []).length === 0 && !isLoading && (
            <Alert variant="info">
              <AlertDescription>
                No stewards have been added yet. An administrator adds them in User Management &amp; Roles.
              </AlertDescription>
            </Alert>
          )}

          <FormField htmlFor="order-steward" label="Serving steward">
            <Select value={selected} onValueChange={setSelected} disabled={isLoading || pending}>
              <SelectTrigger id="order-steward">
                <SelectValue placeholder="Choose a steward" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value={NONE}>No steward</SelectItem>
                {retiredCurrent && (
                  <SelectItem value={currentStewardId!}>{retiredCurrent} (retired)</SelectItem>
                )}
                {(stewards ?? []).map((steward) => (
                  <SelectItem key={steward.id} value={steward.id}>
                    {steward.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </FormField>
        </div>

        <DialogFooter>
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={pending}>
            Cancel
          </Button>
          <Button type="button" onClick={save} loading={pending}>
            Save
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
