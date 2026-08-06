import { useState } from "react";
import { History, Pencil, Tag } from "lucide-react";
import type { Supplier } from "@/entities/supplier";
import type { RawMaterial } from "@/entities/raw-material";
import {
  SetSupplierPriceDialog,
  SupplierPriceHistoryDialog,
  useSupplierPrices,
} from "@/features/suppliers";
import {
  Button,
  EmptyState,
  LoadingState,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/shared/ui";

export function SupplierPricingPanel({ suppliers, rawMaterials }: { suppliers: Supplier[]; rawMaterials: RawMaterial[] }) {
  const [supplierId, setSupplierId] = useState<string>(suppliers[0]?.id ?? "");
  const { data: prices, isLoading } = useSupplierPrices(supplierId || undefined);

  const [priceDialog, setPriceDialog] = useState<{ rawMaterialId?: string } | null>(null);
  const [historyEntry, setHistoryEntry] = useState<{ rawMaterialId: string; rawMaterialName: string } | null>(null);

  const supplier = suppliers.find((s) => s.id === supplierId);

  if (suppliers.length === 0) {
    return <EmptyState icon={<Tag className="size-6" />} title="Add a supplier first" description="Pricing is recorded per supplier." />;
  }

  return (
    <div className="space-y-4 p-4">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div className="w-64 space-y-1.5">
          <Select value={supplierId} onValueChange={setSupplierId}>
            <SelectTrigger>
              <SelectValue placeholder="Choose a supplier" />
            </SelectTrigger>
            <SelectContent>
              {suppliers.map((s) => (
                <SelectItem key={s.id} value={s.id}>
                  {s.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <Button size="sm" onClick={() => setPriceDialog({})} disabled={!supplierId}>
          <Tag /> Set price
        </Button>
      </div>

      {isLoading ? (
        <LoadingState label="Loading prices…" />
      ) : !prices || prices.length === 0 ? (
        <EmptyState icon={<Tag className="size-6" />} title="No prices recorded yet" description="Set what this supplier charges for a raw material." />
      ) : (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Raw material</TableHead>
              <TableHead className="text-right">Price</TableHead>
              <TableHead>Updated</TableHead>
              <TableHead className="w-24" />
            </TableRow>
          </TableHeader>
          <TableBody>
            {prices.map((price) => (
              <TableRow key={price.rawMaterialId}>
                <TableCell className="font-medium">{price.rawMaterialName}</TableCell>
                <TableCell className="text-right tabular">
                  {price.price.toFixed(2)}
                  <span className="ml-1 text-xs text-muted-foreground">/ {price.unitOfMeasurement}</span>
                </TableCell>
                <TableCell className="whitespace-nowrap text-sm text-muted-foreground">
                  {new Date(price.updatedAtUtc).toLocaleDateString()}
                </TableCell>
                <TableCell>
                  <div className="flex justify-end gap-1">
                    <Button
                      variant="ghost"
                      size="icon"
                      aria-label={`Update price of ${price.rawMaterialName}`}
                      onClick={() => setPriceDialog({ rawMaterialId: price.rawMaterialId })}
                    >
                      <Pencil className="size-4" />
                    </Button>
                    <Button
                      variant="ghost"
                      size="icon"
                      aria-label={`View price history of ${price.rawMaterialName}`}
                      onClick={() =>
                        setHistoryEntry({ rawMaterialId: price.rawMaterialId, rawMaterialName: price.rawMaterialName })
                      }
                    >
                      <History className="size-4" />
                    </Button>
                  </div>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}

      {priceDialog && supplier && (
        <SetSupplierPriceDialog
          open
          onOpenChange={(open) => !open && setPriceDialog(null)}
          supplierId={supplier.id}
          supplierName={supplier.name}
          rawMaterials={rawMaterials}
          rawMaterialId={priceDialog.rawMaterialId}
        />
      )}

      {historyEntry && supplierId && (
        <SupplierPriceHistoryDialog
          open
          onOpenChange={(open) => !open && setHistoryEntry(null)}
          supplierId={supplierId}
          rawMaterialId={historyEntry.rawMaterialId}
          rawMaterialName={historyEntry.rawMaterialName}
        />
      )}
    </div>
  );
}
