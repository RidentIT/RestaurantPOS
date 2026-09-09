import { useState } from "react";
import { Plus, Trash2 } from "lucide-react";
import { toast } from "sonner";
import type { RecipeLineInput } from "@/entities/recipe";
import { UNIT_ABBREVIATIONS } from "@/entities/raw-material";
import { useRawMaterials } from "@/features/raw-materials";
import {
  Button,
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

export interface RecipeEditorProps {
  menuItemVariantId: string;
  /** Display name for toasts, e.g. "Chicken Fried Rice (Full)". */
  menuItemName: string;
}

/**
 * Attaches, edits or removes one menu item size's recipe: which raw materials a single sale
 * consumes, and how much of each. Every size gets its own — a Full doesn't necessarily use exactly
 * proportionally more of everything, so recipes are never scaled from a smaller size's.
 *
 * Plain content rather than a dialog — it's meant to sit inline on the menu item's own page,
 * right below the item's own details, so building a dish and stocking what it consumes happen in
 * one place instead of two separate round trips.
 */
export function RecipeEditor({ menuItemVariantId, menuItemName }: RecipeEditorProps) {
  const { data: recipe, isLoading: recipeLoading } = useRecipe(menuItemVariantId);
  const { data: rawMaterials, isLoading: materialsLoading } = useRawMaterials({ isActive: true });
  const { upsert, setEnabled, remove } = useRecipeMutations(menuItemVariantId);

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
      toast.success(`Recipe saved for ${menuItemName}.`);
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
      toast.success(`Recipe removed for ${menuItemName}.`);
    } catch (error) {
      toast.error(error instanceof Error ? error.message : "Something went wrong.");
    }
  };

  const loading = recipeLoading || materialsLoading;

  if (loading) {
    return <LoadingState label="Loading recipe…" />;
  }

  return (
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

      <div className="flex flex-wrap items-center justify-between gap-2 border-t pt-4">
        {recipe ? (
          <Button type="button" variant="destructive" onClick={handleRemove} loading={remove.isPending}>
            Remove recipe
          </Button>
        ) : (
          <span />
        )}
        <Button type="button" onClick={handleSave} loading={upsert.isPending}>
          Save recipe
        </Button>
      </div>
    </div>
  );
}

function toLineInput(line: { rawMaterialId: string; quantity: number }): RecipeLineInput {
  return { rawMaterialId: line.rawMaterialId, quantity: line.quantity };
}
