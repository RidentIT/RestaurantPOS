import { useMemo, useState } from "react";
import { AlertTriangle, ClipboardList, History, Package, Plus, Search, SearchX, Warehouse } from "lucide-react";
import type { GoodsReceivedNoteSummary, StockLevel, StockMovement } from "@/entities/inventory";
import type { RawMaterial } from "@/entities/raw-material";
import { GoodsReceivedNoteDialog, StockAdjustmentDialog, useGoodsReceivedNotes, useMainStoreMovements, useMainStoreStock } from "@/features/inventory";
import { RawMaterialFormDialog, useRawMaterials } from "@/features/raw-materials";
import { useSuppliers } from "@/features/suppliers";
import { Button, Card, EmptyState, Input, SegmentedTabs } from "@/shared/ui";
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

const SEARCH_PLACEHOLDERS: Record<Tab, string> = {
  stock: "Search raw materials…",
  "raw-materials": "Search by name…",
  "goods-received": "Search supplier, raw material, notes…",
  history: "Search raw material, type, notes…",
};

/** Loose, case-insensitive "does this row mention the search term anywhere" check. */
const hit = (term: string, ...fields: Array<string | null | undefined>) =>
  fields.some((field) => field?.toLowerCase().includes(term));

export default function MainStorePage() {
  const [tab, setTab] = useState<Tab>("stock");
  const [search, setSearch] = useState("");

  const { data: stock, isLoading: stockLoading } = useMainStoreStock();
  const { data: rawMaterials, isLoading: rawMaterialsLoading } = useRawMaterials();
  // Suppliers are managed on their own screen; this page only needs the active list for the GRN dialog.
  const { data: suppliers } = useSuppliers({ isActive: true });
  const { data: goodsReceived, isLoading: goodsReceivedLoading } = useGoodsReceivedNotes();
  const { data: movements, isLoading: movementsLoading } = useMainStoreMovements();

  const activeRawMaterials = (rawMaterials ?? []).filter((r) => r.isActive);
  const activeSuppliers = suppliers ?? [];
  const lowStockCount = (stock ?? []).filter((s) => s.isLowStock).length;

  const term = search.trim().toLowerCase();

  // Every list here already loads in full with no pagination, so filtering client-side keeps the
  // four tabs behaving identically instead of some searching instantly and others waiting on a
  // round trip.
  const filteredStock = useMemo<StockLevel[] | undefined>(
    () => (term && stock ? stock.filter((s) => hit(term, s.rawMaterialName)) : stock),
    [stock, term],
  );

  const filteredRawMaterials = useMemo<RawMaterial[] | undefined>(
    () => (term && rawMaterials ? rawMaterials.filter((r) => hit(term, r.name)) : rawMaterials),
    [rawMaterials, term],
  );

  const filteredGoodsReceived = useMemo<GoodsReceivedNoteSummary[] | undefined>(
    () =>
      term && goodsReceived
        ? goodsReceived.filter((n) =>
            hit(term, n.supplierName, n.receivedByName, n.notes, ...n.rawMaterialNames),
          )
        : goodsReceived,
    [goodsReceived, term],
  );

  const filteredMovements = useMemo<StockMovement[] | undefined>(
    () =>
      term && movements
        ? movements.filter((m) => hit(term, m.rawMaterialName, m.performedByName, m.notes))
        : movements,
    [movements, term],
  );

  const [rawMaterialForm, setRawMaterialForm] = useState<RawMaterial | null | undefined>(undefined);
  const [grnDialogOpen, setGrnDialogOpen] = useState(false);
  const [adjustmentDialogOpen, setAdjustmentDialogOpen] = useState(false);

  // True once the active tab's own data has loaded and the search found nothing in it — the cue
  // to show "no matches" instead of delegating to a table that would otherwise show its generic
  // "nothing here yet" empty state, which would say the wrong thing while a search is active.
  const noMatches =
    !!term &&
    {
      stock: !!stock && filteredStock?.length === 0,
      "raw-materials": !!rawMaterials && filteredRawMaterials?.length === 0,
      "goods-received": !!goodsReceived && filteredGoodsReceived?.length === 0,
      history: !!movements && filteredMovements?.length === 0,
    }[tab];

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

            {tab === "raw-materials" && (
              <>
                <div className="flex justify-end p-4 pb-0">
                  <Button size="sm" onClick={() => setRawMaterialForm(null)}>
                    <Plus /> Add raw material
                  </Button>
                </div>
                <RawMaterialsTable
                  rawMaterials={filteredRawMaterials}
                  isLoading={rawMaterialsLoading}
                  onEdit={setRawMaterialForm}
                />
              </>
            )}

            {tab === "goods-received" && (
              <GoodsReceivedTable notes={filteredGoodsReceived} isLoading={goodsReceivedLoading} />
            )}

            {tab === "history" && <MovementHistoryTable movements={filteredMovements} isLoading={movementsLoading} />}
          </>
        )}
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
