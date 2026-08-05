import { useState } from "react";
import { Plus, Trash2 } from "lucide-react";
import { toast } from "sonner";
import type { MenuItem } from "@/entities/menu-item";
import type { RecipeLineInput } from "@/entities/recipe";
import { UNIT_ABBREVIATIONS } from "@/entities/raw-material";
import { useRawMaterials } from "@/features/raw-materials";
import {
  Button,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  EmptyState,
  Input,
  Label,
  LoadingState,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Switch,
} from "@/shared/ui";
import { useRecipe, useRecipeMutations } from "../model/useRecipe";

export interface RecipeEditorDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  menuItem: MenuItem;
}

/**
 * Attaches, edits or removes a menu item's recipe: which raw materials a single sale consumes,
 * and how much of each.
 */
export function RecipeEditorDialog({ open, onOpenChange, menuItem }: RecipeEditorDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-xl">
        <RecipeEditorBody key={menuItem.id} menuItem={menuItem} onDone={() => onOpenChange(false)} />
      </DialogContent>
    </Dialog>
  );
}

function RecipeEditorBody({ menuItem, onDone }: { menuItem: MenuItem; onDone: () => void }) {
  const { data: recipe, isLoading: recipeLoading } = useRecipe(menuItem.id);
  const { data: rawMaterials, isLoading: materialsLoading } = useRawMaterials({ isActive: true });
  const { upsert, setEnabled, remove } = useRecipeMutations(menuItem.id);

  const [lines, setLines] = useState<RecipeLineInput[]>(() => recipe?.lines?.map(toLineInput) ?? []);
  const [linesInitialised, setLinesInitialised] = useState(false);
  const [pendingRawMaterialId, setPendingRawMaterialId] = useState("");
  const [pendingQuantity, setPendingQuantity] = useState("");

  // The recipe query resolves after the initial render, so seed local editable state the first
  // time real data (or its absence) arrives rather than trying to derive it inline.
  if (!linesInitialised && !recipeLoading) {
    setLines(recipe?.lines?.map(toLineInput) ?? []);
    setLinesInitialised(true);
  }

  const nameFor = (rawMaterialId: string) =>
    recipe?.lines?.find((l) => l.rawMaterialId === rawMaterialId)?.rawMaterialName ??
    rawMaterials?.find((r) => r.id === rawMaterialId)?.name ??
    "Unknown";

  const unitFor = (rawMaterialId: string) =>
    recipe?.lines?.find((l) => l.rawMaterialId === rawMaterialId)?.unitOfMeasurement ??
    rawMaterials?.find((r) => r.id === rawMaterialId)?.unitOfMeasurement;

  const availableToAdd = (rawMaterials ?? []).filter((r) => !lines.some((l) => l.rawMaterialId === r.id));

  const addLine = () => {
    const quantity = Number(pendingQuantity);

    if (!pendingRawMaterialId || !Number.isFinite(quantity) || quantity <= 0) {
      toast.error("Choose a raw material and enter a quantity greater than zero.");
      return;
    }

    setLines((prev) => [...prev, { rawMaterialId: pendingRawMaterialId, quantity }]);
    setPendingRawMaterialId("");
    setPendingQuantity("");
  };

  const removeLine = (rawMaterialId: string) =>
    setLines((prev) => prev.filter((l) => l.rawMaterialId !== rawMaterialId));

  const updateQuantity = (rawMaterialId: string, quantity: string) => {
    const parsed = Number(quantity);
    setLines((prev) =>
      prev.map((l) => (l.rawMaterialId === rawMaterialId ? { ...l, quantity: Number.isFinite(parsed) ? parsed : l.quantity } : l)),
    );
  };

  const handleSave = async () => {
    if (lines.length === 0) {
      toast.error("Add at least one raw material before saving.");
      return;
    }

    try {
      await upsert.mutateAsync(lines);
      toast.success(`Recipe saved for ${menuItem.name}.`);
      onDone();
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Something went wrong.");
    }
  };

  const handleToggleEnabled = async (checked: boolean) => {
    try {
      await setEnabled.mutateAsync(checked);
      toast.success(checked ? "Recipe enabled." : "Recipe disabled — it can't be used for a new sale.");
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Something went wrong.");
    }
  };

  const handleRemove = async () => {
    try {
      await remove.mutateAsync();
      toast.success(`Recipe removed for ${menuItem.name}.`);
      onDone();
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Something went wrong.");
    }
  };

  const loading = recipeLoading || materialsLoading;

  return (
    <>
      <DialogHeader>
        <DialogTitle>Recipe for {menuItem.name}</DialogTitle>
        <DialogDescription>
          Ingredients here are deducted from Kitchen stock automatically whenever this item is sold.
        </DialogDescription>
      </DialogHeader>

      {loading ? (
        <LoadingState label="Loading recipe…" />
      ) : (
        <div className="space-y-5">
          {recipe && (
            <div className="flex items-center justify-between rounded-lg border p-3">
              <div>
                <Label htmlFor="recipe-enabled">Recipe enabled</Label>
                <p className="text-xs text-muted-foreground">
                  {recipe.isEnabled ? "Used for new sales." : "Disabled — cannot be used for a new sale."}
                </p>
              </div>
              <Switch
                id="recipe-enabled"
                checked={recipe.isEnabled}
                onCheckedChange={handleToggleEnabled}
                disabled={setEnabled.isPending}
              />
            </div>
          )}

          <div className="space-y-2">
            {lines.length === 0 ? (
              <EmptyState title="No ingredients yet" description="Add raw materials below to build this recipe." />
            ) : (
              lines.map((line) => (
                <div key={line.rawMaterialId} className="flex items-center gap-3 rounded-lg border p-3">
                  <div className="min-w-0 flex-1">
                    <p className="truncate font-medium">{nameFor(line.rawMaterialId)}</p>
                  </div>
                  <Input
                    type="number"
                    inputMode="decimal"
                    min="0"
                    step="any"
                    value={line.quantity}
                    onChange={(e) => updateQuantity(line.rawMaterialId, e.target.value)}
                    className="w-24"
                    aria-label={`Quantity of ${nameFor(line.rawMaterialId)}`}
                  />
                  <span className="w-16 shrink-0 text-sm text-muted-foreground">
                    {UNIT_ABBREVIATIONS[unitFor(line.rawMaterialId)!]}
                  </span>
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    onClick={() => removeLine(line.rawMaterialId)}
                    aria-label={`Remove ${nameFor(line.rawMaterialId)}`}
                  >
                    <Trash2 className="size-4 text-destructive" />
                  </Button>
                </div>
              ))
            )}
          </div>

          {availableToAdd.length > 0 && (
            <div className="flex items-end gap-2 rounded-lg border border-dashed p-3">
              <div className="min-w-0 flex-1 space-y-1.5">
                <Label htmlFor="add-raw-material">Add ingredient</Label>
                <Select value={pendingRawMaterialId} onValueChange={setPendingRawMaterialId}>
                  <SelectTrigger id="add-raw-material">
                    <SelectValue placeholder="Choose a raw material" />
                  </SelectTrigger>
                  <SelectContent>
                    {availableToAdd.map((material) => (
                      <SelectItem key={material.id} value={material.id}>
                        {material.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div className="w-24 space-y-1.5">
                <Label htmlFor="add-quantity">Quantity</Label>
                <Input
                  id="add-quantity"
                  type="number"
                  inputMode="decimal"
                  min="0"
                  step="any"
                  value={pendingQuantity}
                  onChange={(e) => setPendingQuantity(e.target.value)}
                  placeholder="0.25"
                />
              </div>
              <Button type="button" variant="secondary" onClick={addLine}>
                <Plus /> Add
              </Button>
            </div>
          )}
        </div>
      )}

      <DialogFooter className="items-center sm:justify-between">
        {recipe ? (
          <Button type="button" variant="destructive" onClick={handleRemove} loading={remove.isPending}>
            Remove recipe
          </Button>
        ) : (
          <span />
        )}
        <div className="flex gap-2">
          <Button type="button" variant="outline" onClick={onDone} disabled={upsert.isPending}>
            Cancel
          </Button>
          <Button type="button" onClick={handleSave} loading={upsert.isPending} disabled={loading}>
            Save recipe
          </Button>
        </div>
      </DialogFooter>
    </>
  );
}

function toLineInput(line: { rawMaterialId: string; quantity: number }): RecipeLineInput {
  return { rawMaterialId: line.rawMaterialId, quantity: line.quantity };
}
