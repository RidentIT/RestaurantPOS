import { useState } from "react";
import { BarChart3, ClipboardList, Plus, Tag, Truck } from "lucide-react";
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

export default function SuppliersPage() {
  const [tab, setTab] = useState<Tab>("suppliers");
  const [statusFilter, setStatusFilter] = useState<PurchaseOrderStatus | "All">("All");

  const { data: suppliers, isLoading: suppliersLoading } = useSuppliers();
  const { data: rawMaterials } = useRawMaterials();
  const { data: orders, isLoading: ordersLoading } = usePurchaseOrders(
    statusFilter === "All" ? {} : { status: statusFilter },
  );

  const activeSuppliers = (suppliers ?? []).filter((s) => s.isActive);
  const activeRawMaterials = (rawMaterials ?? []).filter((r) => r.isActive);

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
          <SuppliersTable suppliers={suppliers} isLoading={suppliersLoading} onEdit={setSupplierForm} />
        )}

        {tab === "purchase-orders" && (
          <>
            <div className="flex justify-end p-4 pb-0">
              <div className="w-48">
                <Select value={statusFilter} onValueChange={(v) => setStatusFilter(v as PurchaseOrderStatus | "All")}>
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
