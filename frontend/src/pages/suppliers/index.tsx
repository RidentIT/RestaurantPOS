import { useState } from "react";
import { BarChart3, ClipboardList, Plus, Search, Tag, Truck } from "lucide-react";
import type { PurchaseOrderStatus, PurchaseOrderSummary, Supplier } from "@/entities/supplier";
import { PURCHASE_ORDER_STATUSES } from "@/entities/supplier";
import {
  PurchaseOrderDetailDialog,
  PurchaseOrderFormDialog,
  usePurchaseOrder,
  usePurchaseOrders,
} from "@/features/purchase-orders";
import { useRawMaterials } from "@/features/raw-materials";
import { SupplierFormDialog, useSuppliers } from "@/features/suppliers";
import {
  Button,
  Card,
  Input,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  SegmentedTabs,
} from "@/shared/ui";
import { PurchaseOrdersTable } from "./PurchaseOrdersTable";
import { SuppliersTable } from "./SuppliersTable";
import { SupplierPerformancePanel } from "./SupplierPerformancePanel";
import { SupplierPricingPanel } from "./SupplierPricingPanel";

type Tab = "suppliers" | "purchase-orders" | "pricing" | "performance";

const TABS: ReadonlyArray<{ value: Tab; label: string; icon: React.ReactNode }> = [
  { value: "suppliers", label: "Suppliers", icon: <Truck className="size-4" /> },
  { value: "purchase-orders", label: "Purchase Orders", icon: <ClipboardList className="size-4" /> },
  { value: "pricing", label: "Pricing", icon: <Tag className="size-4" /> },
  { value: "performance", label: "Performance", icon: <BarChart3 className="size-4" /> },
];

type SupplierStatusFilter = "all" | "active" | "inactive";

export default function SuppliersPage() {
  const [tab, setTab] = useState<Tab>("suppliers");
  const [poStatusFilter, setPoStatusFilter] = useState<PurchaseOrderStatus | "All">("All");
  const [supplierSearch, setSupplierSearch] = useState("");
  const [supplierStatusFilter, setSupplierStatusFilter] = useState<SupplierStatusFilter>("all");

  const { data: suppliers, isLoading: suppliersLoading } = useSuppliers();
  const { data: rawMaterials } = useRawMaterials();
  const { data: orders, isLoading: ordersLoading } = usePurchaseOrders(
    poStatusFilter === "All" ? {} : { status: poStatusFilter },
  );

  const activeSuppliers = (suppliers ?? []).filter((s) => s.isActive);
  const activeRawMaterials = (rawMaterials ?? []).filter((r) => r.isActive);

  const filteredSuppliers = (suppliers ?? []).filter((s) => {
    const matchesSearch = !supplierSearch || s.name.toLowerCase().includes(supplierSearch.trim().toLowerCase());
    const matchesStatus =
      supplierStatusFilter === "all" ? true : supplierStatusFilter === "active" ? s.isActive : !s.isActive;
    return matchesSearch && matchesStatus;
  });
  const isSupplierFilterActive = supplierSearch !== "" || supplierStatusFilter !== "all";

  const [supplierForm, setSupplierForm] = useState<Supplier | null | undefined>(undefined);
  const [poCreateOpen, setPoCreateOpen] = useState(false);
  const [poEditId, setPoEditId] = useState<string | null>(null);
  const [poViewId, setPoViewId] = useState<string | null>(null);

  const { data: editingOrder } = usePurchaseOrder(poEditId ?? undefined);

  return (
    <div className="space-y-6 p-8">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold">Supplier Management</h1>
          <p className="text-sm text-muted-foreground">
            Suppliers, purchase orders, negotiated pricing and delivery performance.
          </p>
        </div>
        <div className="flex flex-wrap gap-2">
          {tab === "suppliers" && (
            <Button onClick={() => setSupplierForm(null)}>
              <Plus /> Add supplier
            </Button>
          )}
          {tab === "purchase-orders" && (
            <Button onClick={() => setPoCreateOpen(true)} disabled={activeSuppliers.length === 0}>
              <Plus /> New purchase order
            </Button>
          )}
        </div>
      </div>

      <SegmentedTabs tabs={TABS} value={tab} onValueChange={(v) => setTab(v as Tab)} />

      <Card>
        {tab === "suppliers" && (
          <>
            <div className="flex flex-wrap gap-3 p-4 pb-0">
              <div className="relative w-full max-w-xs">
                <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  value={supplierSearch}
                  onChange={(e) => setSupplierSearch(e.target.value)}
                  placeholder="Search by name…"
                  className="pl-9"
                />
              </div>

              <Select
                value={supplierStatusFilter}
                onValueChange={(v) => setSupplierStatusFilter(v as SupplierStatusFilter)}
              >
                <SelectTrigger className="w-40">
                  <SelectValue placeholder="Status" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All statuses</SelectItem>
                  <SelectItem value="active">Active</SelectItem>
                  <SelectItem value="inactive">Deactivated</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <SuppliersTable
              suppliers={filteredSuppliers}
              isLoading={suppliersLoading}
              onEdit={setSupplierForm}
              isFiltered={isSupplierFilterActive}
            />
          </>
        )}

        {tab === "purchase-orders" && (
          <>
            <div className="flex justify-end p-4 pb-0">
              <div className="w-48">
                <Select value={poStatusFilter} onValueChange={(v) => setPoStatusFilter(v as PurchaseOrderStatus | "All")}>
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="All">All statuses</SelectItem>
                    {PURCHASE_ORDER_STATUSES.map((status) => (
                      <SelectItem key={status} value={status}>
                        {status}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            </div>
            <PurchaseOrdersTable
              orders={orders}
              isLoading={ordersLoading}
              onView={(order: PurchaseOrderSummary) => setPoViewId(order.id)}
              onEdit={(order: PurchaseOrderSummary) => setPoEditId(order.id)}
            />
          </>
        )}

        {tab === "pricing" && <SupplierPricingPanel suppliers={activeSuppliers} rawMaterials={activeRawMaterials} />}

        {tab === "performance" && <SupplierPerformancePanel suppliers={activeSuppliers} />}
      </Card>

      <SupplierFormDialog
        open={supplierForm !== undefined}
        onOpenChange={(open) => !open && setSupplierForm(undefined)}
        supplier={supplierForm ?? undefined}
      />

      <PurchaseOrderFormDialog
        open={poCreateOpen || (!!poEditId && !!editingOrder)}
        onOpenChange={(open) => {
          if (!open) {
            setPoCreateOpen(false);
            setPoEditId(null);
          }
        }}
        suppliers={activeSuppliers}
        rawMaterials={activeRawMaterials}
        order={poEditId ? editingOrder : undefined}
      />

      {poViewId && (
        <PurchaseOrderDetailDialog
          open={!!poViewId}
          onOpenChange={(open) => !open && setPoViewId(null)}
          purchaseOrderId={poViewId}
        />
      )}
    </div>
  );
}
