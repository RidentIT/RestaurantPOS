import { useState } from "react";
import { AlertTriangle, ClipboardList, History } from "lucide-react";
import { StockAdjustmentDialog, useKitchenMovements, useKitchenStock } from "@/features/inventory";
import { useRawMaterials } from "@/features/raw-materials";
import { Button, Card, SegmentedTabs } from "@/shared/ui";
import { MovementHistoryTable } from "../main-store/MovementHistoryTable";
import { StockTable } from "../main-store/StockTable";

type Tab = "stock" | "history";

const TABS: ReadonlyArray<{ value: Tab; label: string; icon: React.ReactNode }> = [
  { value: "stock", label: "Current Stock", icon: <ClipboardList className="size-4" /> },
  { value: "history", label: "History", icon: <History className="size-4" /> },
];

export default function KitchenPage() {
  const [tab, setTab] = useState<Tab>("stock");
  const [adjustmentDialogOpen, setAdjustmentDialogOpen] = useState(false);

  const { data: stock, isLoading: stockLoading } = useKitchenStock();
  const { data: movements, isLoading: movementsLoading } = useKitchenMovements();
  const { data: rawMaterials } = useRawMaterials();

  const activeRawMaterials = (rawMaterials ?? []).filter((r) => r.isActive);
  const lowStockCount = (stock ?? []).filter((s) => s.isLowStock).length;

  return (
    <div className="space-y-6 p-8">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold">Kitchen Stock Tracking</h1>
          <p className="text-sm text-muted-foreground">
            Stock here comes only from an approved release from the Main Store, and is consumed automatically as
            orders are prepared.
          </p>
        </div>
        <Button variant="outline" onClick={() => setAdjustmentDialogOpen(true)}>
          Adjust stock
        </Button>
      </div>

      {lowStockCount > 0 && (
        <div className="flex items-center gap-2 rounded-lg border border-warning/30 bg-warning/5 px-4 py-3 text-sm">
          <AlertTriangle className="size-4 shrink-0 text-warning" />
          <span>
            {lowStockCount} raw material{lowStockCount === 1 ? " is" : "s are"} running low — consider requesting a
            release from the Main Store.
          </span>
        </div>
      )}

      <SegmentedTabs tabs={TABS} value={tab} onValueChange={(v) => setTab(v as Tab)} />

      <Card>
        {tab === "stock" && <StockTable stock={stock} isLoading={stockLoading} />}
        {tab === "history" && <MovementHistoryTable movements={movements} isLoading={movementsLoading} />}
      </Card>

      <StockAdjustmentDialog
        open={adjustmentDialogOpen}
        onOpenChange={setAdjustmentDialogOpen}
        store="Kitchen"
        rawMaterials={activeRawMaterials}
      />
    </div>
  );
}
