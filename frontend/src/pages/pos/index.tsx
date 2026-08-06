import { useMemo, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { LayoutGrid, Plus, Search, Settings2 } from "lucide-react";
import { toast } from "sonner";
import type { RestaurantTable } from "@/entities/table";
import { tableDisplayStatus } from "@/entities/table";
import { useOrderMutations } from "@/features/orders";
import { useTables } from "@/features/tables";
import { toApiError } from "@/shared/api/problem";
import { Button, Card, EmptyState, Input, LoadingState } from "@/shared/ui";
import { TableCard, TableStatusLegend } from "./TableCard";

/**
 * The cashier's home screen: the whole room at a glance (POS-034), with every table one tap from
 * its bill (POS-035). Tapping a free table starts an order; tapping a busy one opens it.
 */
export default function PosDashboardPage() {
  const navigate = useNavigate();
  const [search, setSearch] = useState("");

  const { data: tables, isLoading } = useTables();
  const { create } = useOrderMutations();

  const visibleTables = useMemo(() => {
    const active = (tables ?? []).filter((t) => t.isActive);
    const term = search.trim().toLowerCase();

    if (!term) return active;

    return active.filter(
      (t) =>
        t.number.toLowerCase().includes(term) ||
        String(t.currentOrder?.orderNumber ?? "").includes(term),
    );
  }, [tables, search]);

  const openTables = (tables ?? []).filter((t) => t.currentOrder);
  const readyCount = (tables ?? []).filter((t) => tableDisplayStatus(t) === "Ready").length;

  const openTable = async (table: RestaurantTable) => {
    if (table.currentOrder) {
      navigate(`/pos/orders/${table.currentOrder.orderId}`);
      return;
    }

    try {
      const order = await create.mutateAsync(table.id);
      navigate(`/pos/orders/${order.id}`);
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  };

  return (
    <div className="space-y-6 p-8">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold">POS &amp; Billing</h1>
          <p className="text-sm text-muted-foreground">
            {openTables.length === 0
              ? "Every table is free."
              : `${openTables.length} table${openTables.length === 1 ? "" : "s"} open` +
                (readyCount > 0 ? ` · ${readyCount} ready to serve` : "")}
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <div className="relative">
            <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Table or order number"
              className="w-56 pl-9"
              aria-label="Search tables by number or order number"
            />
          </div>
          <Button variant="outline" asChild>
            <Link to="/pos/tables">
              <Settings2 /> Manage tables
            </Link>
          </Button>
        </div>
      </div>

      <TableStatusLegend />

      <Card className="p-4">
        {isLoading ? (
          <LoadingState label="Loading the floor plan…" />
        ) : visibleTables.length === 0 ? (
          <EmptyState
            icon={<LayoutGrid className="size-6" />}
            title={search ? "No table matches that" : "No tables yet"}
            description={
              search
                ? "Try a different table or order number."
                : "Add the restaurant's tables before taking orders."
            }
          />
        ) : (
          <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5">
            {visibleTables.map((table) => (
              <TableCard key={table.id} table={table} onOpen={openTable} busy={create.isPending} />
            ))}
          </div>
        )}
      </Card>

      {!isLoading && (tables ?? []).length === 0 && (
        <div className="flex justify-center">
          <Button asChild>
            <Link to="/pos/tables">
              <Plus /> Add your first table
            </Link>
          </Button>
        </div>
      )}
    </div>
  );
}
