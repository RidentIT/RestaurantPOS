import { useState } from "react";
import { AlertTriangle, ClipboardList, History, Package, Plus, Warehouse } from "lucide-react";
import type { RawMaterial } from "@/entities/raw-material";
import { GoodsReceivedNoteDialog, StockAdjustmentDialog, useGoodsReceivedNotes, useMainStoreMovements, useMainStoreStock } from "@/features/inventory";
import { RawMaterialFormDialog, useRawMaterials } from "@/features/raw-materials";
import { useSuppliers } from "@/features/suppliers";
import { Button, Card, SegmentedTabs } from "@/shared/ui";
import { GoodsReceivedTable } from "./GoodsReceivedTable";
import { MovementHistoryTable } from "./MovementHistoryTable";
import { RawMaterialsTable } from "./RawMaterialsTable";
import { StockTable } from "./StockTable";

type Tab = "stock" | "raw-materials" | "goods-received" | "history";

const TABS: ReadonlyArray<{ value: Tab; label: string; icon: React.ReactNode }> = [
  { value: "stock", label: "Current Stock", icon: <Warehouse className="size-4" /> },
  { value: "raw-materials", label: "Raw Materials", icon: <Package className="size-4" /> },
  { value: "goods-received", label: "Goods Received", icon: <ClipboardList className="size-4" /> },
  { value: "history", label: "History", icon: <History className="size-4" /> },
];

export default function MainStorePage() {
  const [tab, setTab] = useState<Tab>("stock");

  const { data: stock, isLoading: stockLoading } = useMainStoreStock();
  const { data: rawMaterials, isLoading: rawMaterialsLoading } = useRawMaterials();
  // Suppliers are managed on their own screen; this page only needs the active list for the GRN dialog.
  const { data: suppliers } = useSuppliers({ isActive: true });
  const { data: goodsReceived, isLoading: goodsReceivedLoading } = useGoodsReceivedNotes();
  const { data: movements, isLoading: movementsLoading } = useMainStoreMovements();

  const activeRawMaterials = (rawMaterials ?? []).filter((r) => r.isActive);
  const activeSuppliers = suppliers ?? [];
  const lowStockCount = (stock ?? []).filter((s) => s.isLowStock).length;

  const [rawMaterialForm, setRawMaterialForm] = useState<RawMaterial | null | undefined>(undefined);
  const [grnDialogOpen, setGrnDialogOpen] = useState(false);
  const [adjustmentDialogOpen, setAdjustmentDialogOpen] = useState(false);

  return (
    <div className="space-y-6 p-8">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold">Store Stock Management</h1>
          <p className="text-sm text-muted-foreground">
            Receive goods from suppliers and keep the Main Store's stock accurate.
          </p>
        </div>
        <div className="flex flex-wrap gap-2">
          <Button variant="outline" onClick={() => setAdjustmentDialogOpen(true)}>
            Adjust stock
          </Button>
          <Button onClick={() => setGrnDialogOpen(true)}>
            <Plus /> Receive goods
          </Button>
        </div>
      </div>

      {lowStockCount > 0 && (
        <div className="flex items-center gap-2 rounded-lg border border-warning/30 bg-warning/5 px-4 py-3 text-sm">
          <AlertTriangle className="size-4 shrink-0 text-warning" />
          <span>
            {lowStockCount} raw material{lowStockCount === 1 ? " is" : "s are"} at or below its reorder level.
          </span>
        </div>
      )}

      <SegmentedTabs tabs={TABS} value={tab} onValueChange={(v) => setTab(v as Tab)} />

      <Card>
        {tab === "stock" && <StockTable stock={stock} isLoading={stockLoading} />}

        {tab === "raw-materials" && (
          <>
            <div className="flex justify-end p-4 pb-0">
              <Button size="sm" onClick={() => setRawMaterialForm(null)}>
                <Plus /> Add raw material
              </Button>
            </div>
            <RawMaterialsTable
              rawMaterials={rawMaterials}
              isLoading={rawMaterialsLoading}
              onEdit={setRawMaterialForm}
            />
          </>
        )}

        {tab === "goods-received" && <GoodsReceivedTable notes={goodsReceived} isLoading={goodsReceivedLoading} />}

        {tab === "history" && <MovementHistoryTable movements={movements} isLoading={movementsLoading} />}
      </Card>

      <RawMaterialFormDialog
        open={rawMaterialForm !== undefined}
        onOpenChange={(open) => !open && setRawMaterialForm(undefined)}
        rawMaterial={rawMaterialForm ?? undefined}
      />

      <GoodsReceivedNoteDialog
        open={grnDialogOpen}
        onOpenChange={setGrnDialogOpen}
        suppliers={activeSuppliers}
        rawMaterials={activeRawMaterials}
      />

      <StockAdjustmentDialog
        open={adjustmentDialogOpen}
        onOpenChange={setAdjustmentDialogOpen}
        store="MainStore"
        rawMaterials={activeRawMaterials}
      />
    </div>
  );
}
