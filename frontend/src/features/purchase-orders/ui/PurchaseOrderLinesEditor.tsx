import { useState } from "react";
import { Plus, Trash2 } from "lucide-react";
import { toast } from "sonner";
import type { RawMaterial } from "@/entities/raw-material";
import { UNIT_ABBREVIATIONS } from "@/entities/raw-material";
import type { PurchaseOrderLineInput } from "@/entities/supplier";
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

export interface PurchaseOrderLinesEditorProps {
  /** Raw materials that may be picked. Callers pass an already-filtered active list. */
  rawMaterials: RawMaterial[];
  lines: PurchaseOrderLineInput[];
  onChange: (lines: PurchaseOrderLineInput[]) => void;
}

/** Add/remove/quantity/price editor for a purchase order's lines, with a running total. */
export function PurchaseOrderLinesEditor({ rawMaterials, lines, onChange }: PurchaseOrderLinesEditorProps) {
  const [pendingRawMaterialId, setPendingRawMaterialId] = useState("");
  const [pendingQuantity, setPendingQuantity] = useState("");
  const [pendingUnitPrice, setPendingUnitPrice] = useState("");

  const byId = (id: string) => rawMaterials.find((r) => r.id === id);
  const availableToAdd = rawMaterials.filter((r) => !lines.some((l) => l.rawMaterialId === r.id));
  const total = lines.reduce((sum, l) => sum + l.quantity * l.unitPrice, 0);

  const addLine = () => {
    const quantity = Number(pendingQuantity);
    const unitPrice = Number(pendingUnitPrice);

    if (!pendingRawMaterialId || !Number.isFinite(quantity) || quantity <= 0) {
      toast.error("Choose a raw material and enter a quantity greater than zero.");
      return;
    }

    if (!Number.isFinite(unitPrice) || unitPrice < 0) {
      toast.error("Enter a unit price of zero or more.");
      return;
    }

    onChange([...lines, { rawMaterialId: pendingRawMaterialId, quantity, unitPrice }]);
    setPendingRawMaterialId("");
    setPendingQuantity("");
    setPendingUnitPrice("");
  };

  const removeLine = (rawMaterialId: string) =>
    onChange(lines.filter((l) => l.rawMaterialId !== rawMaterialId));

  const updateLine = (rawMaterialId: string, field: "quantity" | "unitPrice", value: string) => {
    const parsed = Number(value);
    onChange(
      lines.map((l) =>
        l.rawMaterialId === rawMaterialId && Number.isFinite(parsed) ? { ...l, [field]: parsed } : l,
      ),
    );
  };

  return (
    <div className="space-y-3">
      {lines.length === 0 ? (
        <EmptyState title="No raw materials added yet" description="Add one below to get started." />
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
                  onChange={(e) => updateLine(line.rawMaterialId, "quantity", e.target.value)}
                  className="w-24"
                  aria-label={`Quantity of ${material?.name ?? "raw material"}`}
                />
                <span className="w-14 shrink-0 text-sm text-muted-foreground">
                  {material ? UNIT_ABBREVIATIONS[material.unitOfMeasurement] : ""}
                </span>
                <span className="shrink-0 text-sm text-muted-foreground">@</span>
                <Input
                  type="number"
                  inputMode="decimal"
                  min="0"
                  step="any"
                  value={line.unitPrice}
                  onChange={(e) => updateLine(line.rawMaterialId, "unitPrice", e.target.value)}
                  className="w-28"
                  aria-label={`Unit price of ${material?.name ?? "raw material"}`}
                />
                <span className="w-24 shrink-0 text-right text-sm tabular text-muted-foreground">
                  = {(line.quantity * line.unitPrice).toFixed(2)}
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

          <div className="flex justify-end pr-11 text-sm font-medium">
            Total: <span className="ml-2 tabular">{total.toFixed(2)}</span>
          </div>
        </div>
      )}

      {availableToAdd.length > 0 && (
        <div className="flex flex-wrap items-end gap-2 rounded-lg border border-dashed p-3">
          <div className="min-w-40 flex-1 space-y-1.5">
            <Label htmlFor="po-line-material">Raw material</Label>
            <Select value={pendingRawMaterialId} onValueChange={setPendingRawMaterialId}>
              <SelectTrigger id="po-line-material">
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
            <Label htmlFor="po-line-quantity">Quantity</Label>
            <Input
              id="po-line-quantity"
              type="number"
              inputMode="decimal"
              min="0"
              step="any"
              value={pendingQuantity}
              onChange={(e) => setPendingQuantity(e.target.value)}
              placeholder="5"
            />
          </div>
          <div className="w-28 space-y-1.5">
            <Label htmlFor="po-line-price">Unit price</Label>
            <Input
              id="po-line-price"
              type="number"
              inputMode="decimal"
              min="0"
              step="any"
              value={pendingUnitPrice}
              onChange={(e) => setPendingUnitPrice(e.target.value)}
              placeholder="250.00"
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
