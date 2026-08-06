import { useState } from "react";
import { Plus, Trash2 } from "lucide-react";
import { toast } from "sonner";
import type { RawMaterial } from "@/entities/raw-material";
import { UNIT_ABBREVIATIONS } from "@/entities/raw-material";
import type { StockLineInput } from "@/entities/inventory";
import {
  Button,
  EmptyState,
  Input,
  Label,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/shared/ui";

export interface StockLinesEditorProps {
  /** Raw materials that may be picked. Callers pass an already-filtered active list. */
  rawMaterials: RawMaterial[];
  lines: StockLineInput[];
  onChange: (lines: StockLineInput[]) => void;
  emptyMessage?: string;
}

/**
 * Add/remove/quantity editor for a list of (raw material, quantity) lines — the shared shape
 * behind both a Goods Received Note and a stock release.
 */
export function StockLinesEditor({
  rawMaterials,
  lines,
  onChange,
  emptyMessage = "Add a raw material below to get started.",
}: StockLinesEditorProps) {
  const [pendingRawMaterialId, setPendingRawMaterialId] = useState("");
  const [pendingQuantity, setPendingQuantity] = useState("");

  const byId = (id: string) => rawMaterials.find((r) => r.id === id);
  const availableToAdd = rawMaterials.filter((r) => !lines.some((l) => l.rawMaterialId === r.id));

  const addLine = () => {
    const quantity = Number(pendingQuantity);

    if (!pendingRawMaterialId || !Number.isFinite(quantity) || quantity <= 0) {
      toast.error("Choose a raw material and enter a quantity greater than zero.");
      return;
    }

    onChange([...lines, { rawMaterialId: pendingRawMaterialId, quantity }]);
    setPendingRawMaterialId("");
    setPendingQuantity("");
  };

  const removeLine = (rawMaterialId: string) =>
    onChange(lines.filter((l) => l.rawMaterialId !== rawMaterialId));

  const updateQuantity = (rawMaterialId: string, quantity: string) => {
    const parsed = Number(quantity);
    onChange(
      lines.map((l) => (l.rawMaterialId === rawMaterialId ? { ...l, quantity: Number.isFinite(parsed) ? parsed : l.quantity } : l)),
    );
  };

  return (
    <div className="space-y-3">
      {lines.length === 0 ? (
        <EmptyState title="No raw materials added yet" description={emptyMessage} />
      ) : (
        <div className="space-y-2">
          {lines.map((line) => {
            const material = byId(line.rawMaterialId);

            return (
              <div key={line.rawMaterialId} className="flex items-center gap-3 rounded-lg border p-3">
                <div className="min-w-0 flex-1">
                  <p className="truncate font-medium">{material?.name ?? "Unknown"}</p>
                </div>
                <Input
                  type="number"
                  inputMode="decimal"
                  min="0"
                  step="any"
                  value={line.quantity}
                  onChange={(e) => updateQuantity(line.rawMaterialId, e.target.value)}
                  className="w-24"
                  aria-label={`Quantity of ${material?.name ?? "raw material"}`}
                />
                <span className="w-16 shrink-0 text-sm text-muted-foreground">
                  {material ? UNIT_ABBREVIATIONS[material.unitOfMeasurement] : ""}
                </span>
                <Button
                  type="button"
                  variant="ghost"
                  size="icon"
                  onClick={() => removeLine(line.rawMaterialId)}
                  aria-label={`Remove ${material?.name ?? "raw material"}`}
                >
                  <Trash2 className="size-4 text-destructive" />
                </Button>
              </div>
            );
          })}
        </div>
      )}

      {availableToAdd.length > 0 && (
        <div className="flex items-end gap-2 rounded-lg border border-dashed p-3">
          <div className="min-w-0 flex-1 space-y-1.5">
            <Label htmlFor="stock-line-material">Raw material</Label>
            <Select value={pendingRawMaterialId} onValueChange={setPendingRawMaterialId}>
              <SelectTrigger id="stock-line-material">
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
            <Label htmlFor="stock-line-quantity">Quantity</Label>
            <Input
              id="stock-line-quantity"
              type="number"
              inputMode="decimal"
              min="0"
              step="any"
              value={pendingQuantity}
              onChange={(e) => setPendingQuantity(e.target.value)}
              placeholder="5"
            />
          </div>
          <Button type="button" variant="secondary" onClick={addLine}>
            <Plus /> Add
          </Button>
        </div>
      )}
    </div>
  );
}
