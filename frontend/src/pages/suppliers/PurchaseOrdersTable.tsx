import { ClipboardList, Eye, MoreHorizontal, Pencil, Send, ShieldCheck, XCircle } from "lucide-react";
import { toast } from "sonner";
import type { PurchaseOrderSummary } from "@/entities/supplier";
import { PURCHASE_ORDER_STATUS_BADGE, usePurchaseOrderMutations } from "@/features/purchase-orders";
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

export function PurchaseOrdersTable({
  orders,
  isLoading,
  onView,
  onEdit,
}: {
  orders: PurchaseOrderSummary[] | undefined;
  isLoading: boolean;
  onView: (order: PurchaseOrderSummary) => void;
  onEdit: (order: PurchaseOrderSummary) => void;
}) {
  const { submit, confirm, cancel } = usePurchaseOrderMutations();

  const submitOrder = async (order: PurchaseOrderSummary) => {
    try {
      await submit.mutateAsync(order.id);
      toast.success("Purchase order submitted to the supplier.");
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  };

  const confirmOrder = async (order: PurchaseOrderSummary) => {
    try {
      await confirm.mutateAsync(order.id);
      toast.success("Purchase order confirmed.");
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  };

  const cancelOrder = async (order: PurchaseOrderSummary) => {
    try {
      await cancel.mutateAsync(order.id);
      toast.success("Purchase order cancelled.");
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  };

  if (isLoading) {
    return <LoadingState label="Loading purchase orders…" />;
  }

  if (!orders || orders.length === 0) {
    return (
      <EmptyState
        icon={<ClipboardList className="size-6" />}
        title="No purchase orders yet"
        description="Create one to start ordering from a supplier."
      />
    );
  }

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Date</TableHead>
          <TableHead>Supplier</TableHead>
          <TableHead>Status</TableHead>
          <TableHead className="text-right">Total</TableHead>
          <TableHead className="text-right">Balance</TableHead>
          <TableHead className="w-12" />
        </TableRow>
      </TableHeader>
      <TableBody>
        {orders.map((order) => (
          <TableRow key={order.id}>
            <TableCell className="whitespace-nowrap text-sm text-muted-foreground">
              {new Date(order.createdAtUtc).toLocaleDateString()}
            </TableCell>
            <TableCell className="font-medium">{order.supplierName}</TableCell>
            <TableCell>
              <Badge variant={PURCHASE_ORDER_STATUS_BADGE[order.status]}>{order.status}</Badge>
            </TableCell>
            <TableCell className="text-right tabular">{order.totalAmount.toFixed(2)}</TableCell>
            <TableCell className="text-right tabular">{order.balance.toFixed(2)}</TableCell>
            <TableCell>
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="ghost" size="icon">
                    <MoreHorizontal className="size-4" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end">
                  <DropdownMenuItem onSelect={() => onView(order)}>
                    <Eye /> View details
                  </DropdownMenuItem>
                  {order.status === "Draft" && (
                    <>
                      <DropdownMenuItem onSelect={() => onEdit(order)}>
                        <Pencil /> Edit draft
                      </DropdownMenuItem>
                      <DropdownMenuItem onSelect={() => submitOrder(order)}>
                        <Send /> Submit to supplier
                      </DropdownMenuItem>
                    </>
                  )}
                  {order.status === "Submitted" && (
                    <DropdownMenuItem onSelect={() => confirmOrder(order)}>
                      <ShieldCheck /> Mark confirmed
                    </DropdownMenuItem>
                  )}
                  {order.status !== "Delivered" && order.status !== "Cancelled" && (
                    <DropdownMenuItem destructive onSelect={() => cancelOrder(order)}>
                      <XCircle /> Cancel order
                    </DropdownMenuItem>
                  )}
                </DropdownMenuContent>
              </DropdownMenu>
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}
