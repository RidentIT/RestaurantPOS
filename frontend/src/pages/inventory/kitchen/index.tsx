import { useMemo, useState } from "react";
import { AlertTriangle, ClipboardList, History, Search, SearchX } from "lucide-react";
import type { StockLevel, StockMovement } from "@/entities/inventory";
import { StockAdjustmentDialog, useKitchenMovements, useKitchenStock } from "@/features/inventory";
import { useRawMaterials } from "@/features/raw-materials";
import { Button, Card, EmptyState, Input, SegmentedTabs } from "@/shared/ui";
import { MovementHistoryTable } from "../main-store/MovementHistoryTable";
import { StockTable } from "../main-store/StockTable";

type Tab = "stock" | "history";

const TABS: ReadonlyArray<{ value: Tab; label: string; icon: React.ReactNode }> = [
  { value: "stock", label: "Current Stock", icon: <ClipboardList className="size-4" /> },
  { value: "history", label: "History", icon: <History className="size-4" /> },
];

const SEARCH_PLACEHOLDERS: Record<Tab, string> = {
  stock: "Search raw materials…",
  history: "Search raw material, type, notes…",
};

/** Loose, case-insensitive "does this row mention the search term anywhere" check. */
const hit = (term: string, ...fields: Array<string | null | undefined>) =>
  fields.some((field) => field?.toLowerCase().includes(term));

export default function KitchenPage() {
  const [tab, setTab] = useState<Tab>("stock");
  const [search, setSearch] = useState("");
  const [adjustmentDialogOpen, setAdjustmentDialogOpen] = useState(false);

  const { data: stock, isLoading: stockLoading } = useKitchenStock();
  const { data: movements, isLoading: movementsLoading } = useKitchenMovements();
  const { data: rawMaterials } = useRawMaterials();

  const activeRawMaterials = (rawMaterials ?? []).filter((r) => r.isActive);
  const lowStockCount = (stock ?? []).filter((s) => s.isLowStock).length;

  const term = search.trim().toLowerCase();

  const filteredStock = useMemo<StockLevel[] | undefined>(
    () => (term && stock ? stock.filter((s) => hit(term, s.rawMaterialName)) : stock),
    [stock, term],
  );

  const filteredMovements = useMemo<StockMovement[] | undefined>(
    () =>
      term && movements
        ? movements.filter((m) => hit(term, m.rawMaterialName, m.performedByName, m.notes))
        : movements,
    [movements, term],
  );

  const noMatches =
    !!term &&
    (tab === "stock" ? !!stock && filteredStock?.length === 0 : !!movements && filteredMovements?.length === 0);

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

      <div className="flex flex-wrap items-center justify-between gap-3">
        <SegmentedTabs tabs={TABS} value={tab} onValueChange={(v) => setTab(v as Tab)} />

        <div className="relative w-full max-w-xs">
          <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder={SEARCH_PLACEHOLDERS[tab]}
            className="pl-9"
            aria-label={SEARCH_PLACEHOLDERS[tab]}
          />
        </div>
      </div>

      <Card>
        {noMatches ? (
          <EmptyState
            icon={<SearchX className="size-6" />}
            title="Nothing matches that search"
            description="Try a different name or clear the search."
          />
        ) : (
          <>
            {tab === "stock" && <StockTable stock={filteredStock} isLoading={stockLoading} />}
            {tab === "history" && <MovementHistoryTable movements={filteredMovements} isLoading={movementsLoading} />}
          </>
        )}
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
