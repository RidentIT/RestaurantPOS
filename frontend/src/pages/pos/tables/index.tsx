import { useState } from "react";
import { Link } from "react-router-dom";
import { ArrowLeft, LayoutGrid, MoreHorizontal, Pencil, Plus, ShieldCheck, ShieldOff } from "lucide-react";
import { toast } from "sonner";
import type { RestaurantTable } from "@/entities/table";
import { TableFormDialog, useTableMutations, useTables } from "@/features/tables";
import { toApiError } from "@/shared/api/problem";
import {
  Badge,
  Button,
  Card,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  EmptyState,
  LoadingState,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/shared/ui";

/** The floor plan's setup screen: which tables exist and what they are called. */
export default function TableManagementPage() {
  // undefined = closed, null = adding, a table = editing.
  const [form, setForm] = useState<RestaurantTable | null | undefined>(undefined);
  const { data: tables, isLoading } = useTables({ pollMs: 0 });
  const { setActive } = useTableMutations();

  const toggleActive = async (table: RestaurantTable) => {
    try {
      await setActive.mutateAsync({ id: table.id, isActive: !table.isActive });
      toast.success(
        table.isActive
          ? `Table ${table.number} is out of service.`
          : `Table ${table.number} is back in service.`,
      );
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  };

  return (
    <div className="space-y-6 p-8">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div className="space-y-1">
          <Button variant="ghost" size="sm" asChild className="-ml-2">
            <Link to="/pos">
              <ArrowLeft /> Back to floor plan
            </Link>
          </Button>
          <h1 className="text-2xl font-semibold">Tables</h1>
          <p className="text-sm text-muted-foreground">
            Every table customers can be seated at. A table with a live order cannot be taken out of service.
          </p>
        </div>
        <Button onClick={() => setForm(null)}>
          <Plus /> Add table
        </Button>
      </div>

      <Card>
        {isLoading ? (
          <LoadingState label="Loading tables…" />
        ) : !tables || tables.length === 0 ? (
          <EmptyState
            icon={<LayoutGrid className="size-6" />}
            title="No tables yet"
            description="Add the restaurant's tables so orders can be taken against them."
          />
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Number</TableHead>
                <TableHead>Seats</TableHead>
                <TableHead>Notes</TableHead>
                <TableHead>State</TableHead>
                <TableHead className="w-12" />
              </TableRow>
            </TableHeader>
            <TableBody>
              {tables.map((table) => (
                <TableRow key={table.id}>
                  <TableCell className="font-medium tabular">{table.number}</TableCell>
                  <TableCell className="tabular text-muted-foreground">{table.seats || "—"}</TableCell>
                  <TableCell className="max-w-56 truncate text-sm text-muted-foreground">
                    {table.notes ?? "—"}
                  </TableCell>
                  <TableCell>
                    {!table.isActive ? (
                      <Badge variant="destructive">Out of service</Badge>
                    ) : table.currentOrder ? (
                      <Badge variant="warning">Occupied</Badge>
                    ) : (
                      <Badge variant="success">Available</Badge>
                    )}
                  </TableCell>
                  <TableCell>
                    <DropdownMenu>
                      <DropdownMenuTrigger asChild>
                        <Button variant="ghost" size="icon">
                          <MoreHorizontal className="size-4" />
                        </Button>
                      </DropdownMenuTrigger>
                      <DropdownMenuContent align="end">
                        <DropdownMenuItem onSelect={() => setForm(table)}>
                          <Pencil /> Edit
                        </DropdownMenuItem>
                        <DropdownMenuItem
                          destructive={table.isActive}
                          onSelect={() => toggleActive(table)}
                        >
                          {table.isActive ? (
                            <>
                              <ShieldOff /> Take out of service
                            </>
                          ) : (
                            <>
                              <ShieldCheck /> Put back in service
                            </>
                          )}
                        </DropdownMenuItem>
                      </DropdownMenuContent>
                    </DropdownMenu>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </Card>

      <TableFormDialog
        open={form !== undefined}
        onOpenChange={(open) => !open && setForm(undefined)}
        table={form ?? undefined}
      />
    </div>
  );
}
