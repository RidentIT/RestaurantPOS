import { MoreHorizontal, Pencil, ShieldCheck, ShieldOff, Truck } from "lucide-react";
import { toast } from "sonner";
import type { Supplier } from "@/entities/supplier";
import { useSupplierMutations } from "@/features/suppliers";
import { toApiError } from "@/shared/api/problem";
import {
  Badge,
  Button,
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

export function SuppliersTable({
  suppliers,
  isLoading,
  onEdit,
}: {
  suppliers: Supplier[] | undefined;
  isLoading: boolean;
  onEdit: (supplier: Supplier) => void;
}) {
  const { setActive } = useSupplierMutations();

  const toggleActive = async (supplier: Supplier) => {
    try {
      await setActive.mutateAsync({ id: supplier.id, isActive: !supplier.isActive });
      toast.success(supplier.isActive ? `${supplier.name} was deactivated.` : `${supplier.name} was reactivated.`);
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  };

  if (isLoading) {
    return <LoadingState label="Loading suppliers…" />;
  }

  if (!suppliers || suppliers.length === 0) {
    return (
      <EmptyState icon={<Truck className="size-6" />} title="No suppliers yet" description="Add one to get started." />
    );
  }

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Name</TableHead>
          <TableHead>Contact</TableHead>
          <TableHead>Phone</TableHead>
          <TableHead>Terms</TableHead>
          <TableHead>Status</TableHead>
          <TableHead className="w-12" />
        </TableRow>
      </TableHeader>
      <TableBody>
        {suppliers.map((supplier) => (
          <TableRow key={supplier.id}>
            <TableCell className="font-medium">{supplier.name}</TableCell>
            <TableCell className="text-sm text-muted-foreground">{supplier.contactName ?? "—"}</TableCell>
            <TableCell className="text-sm text-muted-foreground">{supplier.phone ?? "—"}</TableCell>
            <TableCell className="text-sm text-muted-foreground">
              {supplier.paymentTermsDays === 0 ? "Cash on delivery" : `${supplier.paymentTermsDays} days`}
            </TableCell>
            <TableCell>
              {supplier.isActive ? (
                <Badge variant="success">Active</Badge>
              ) : (
                <Badge variant="destructive">Deactivated</Badge>
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
                  <DropdownMenuItem onSelect={() => onEdit(supplier)}>
                    <Pencil /> Edit
                  </DropdownMenuItem>
                  <DropdownMenuItem destructive={supplier.isActive} onSelect={() => toggleActive(supplier)}>
                    {supplier.isActive ? (
                      <>
                        <ShieldOff /> Deactivate
                      </>
                    ) : (
                      <>
                        <ShieldCheck /> Reactivate
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
  );
}
