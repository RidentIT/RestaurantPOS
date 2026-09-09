import { useState } from "react";
import { toast } from "sonner";
import {
  Badge,
  Button,
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  LoadingState,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/shared/ui";
import { toApiError } from "@/shared/api/problem";
import { usePurchaseOrder, usePurchaseOrderMutations, usePurchaseOrderPayments } from "../model/usePurchaseOrders";
import { PURCHASE_ORDER_STATUS_BADGE } from "../lib/status";
import { purchaseOrderRef } from "../lib/describe";
import { RecordPaymentDialog } from "./RecordPaymentDialog";

export interface PurchaseOrderDetailDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  purchaseOrderId: string;
}

/** A purchase order's full lines, totals, status actions and payment history. */
export function PurchaseOrderDetailDialog({ open, onOpenChange, purchaseOrderId }: PurchaseOrderDetailDialogProps) {
  const { data: order, isLoading } = usePurchaseOrder(open ? purchaseOrderId : undefined);
  const { data: payments } = usePurchaseOrderPayments(open ? purchaseOrderId : undefined);
  const { submit, confirm, cancel } = usePurchaseOrderMutations();
  const [paymentDialogOpen, setPaymentDialogOpen] = useState(false);

  const runAction = async (action: "submit" | "confirm" | "cancel") => {
    try {
      if (action === "submit") {
        await submit.mutateAsync(purchaseOrderId);
        toast.success("Purchase order submitted to the supplier.");
      } else if (action === "confirm") {
        await confirm.mutateAsync(purchaseOrderId);
        toast.success("Purchase order confirmed.");
      } else {
        await cancel.mutateAsync(purchaseOrderId);
        toast.success("Purchase order cancelled.");
      }
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  };

  return (
    <>
      <Dialog open={open} onOpenChange={onOpenChange}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>{order ? `Purchase order ${purchaseOrderRef(order.id)}` : "Purchase order"}</DialogTitle>
          </DialogHeader>

          {isLoading || !order ? (
            <LoadingState label="Loading purchase order…" />
          ) : (
            <div className="space-y-4">
              <div className="flex flex-wrap items-start justify-between gap-2">
                <div>
                  <p className="font-medium">{order.supplierName}</p>
                  <p className="text-sm text-muted-foreground">
                    Created by {order.createdByName} on {new Date(order.createdAtUtc).toLocaleDateString()}
                  </p>
                  {order.expectedDeliveryDate && (
                    <p className="text-sm text-muted-foreground">
                      Expected delivery: {new Date(order.expectedDeliveryDate).toLocaleDateString()}
                    </p>
                  )}
                </div>
                <Badge variant={PURCHASE_ORDER_STATUS_BADGE[order.status]}>{order.status}</Badge>
              </div>

              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Raw material</TableHead>
                    <TableHead className="text-right">Qty</TableHead>
                    <TableHead className="text-right">Unit price</TableHead>
                    <TableHead className="text-right">Line total</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {order.lines.map((line) => (
                    <TableRow key={line.rawMaterialId}>
                      <TableCell className="font-medium">{line.rawMaterialName}</TableCell>
                      <TableCell className="text-right tabular">{line.quantity}</TableCell>
                      <TableCell className="text-right tabular">{line.unitPrice.toFixed(2)}</TableCell>
                      <TableCell className="text-right tabular">{line.lineTotal.toFixed(2)}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>

              <div className="flex justify-end gap-6 text-sm">
                <span>
                  Total: <span className="font-medium tabular">{order.totalAmount.toFixed(2)}</span>
                </span>
                <span>
                  Paid: <span className="font-medium tabular">{order.amountPaid.toFixed(2)}</span>
                </span>
                <span>
                  Balance: <span className="font-medium tabular">{order.balance.toFixed(2)}</span>
                </span>
              </div>

              {order.notes && <p className="text-sm text-muted-foreground">Notes: {order.notes}</p>}

              {payments && payments.length > 0 && (
                <div>
                  <p className="mb-2 text-sm font-medium">Payments</p>
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>Date</TableHead>
                        <TableHead>Method</TableHead>
                        <TableHead className="text-right">Amount</TableHead>
                        <TableHead>Recorded by</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {payments.map((payment) => (
                        <TableRow key={payment.id}>
                          <TableCell className="whitespace-nowrap text-sm">
                            {new Date(payment.paymentDateUtc).toLocaleDateString()}
                          </TableCell>
                          <TableCell className="text-sm">{payment.method}</TableCell>
                          <TableCell className="text-right tabular">{payment.amount.toFixed(2)}</TableCell>
                          <TableCell className="text-sm text-muted-foreground">{payment.recordedByName}</TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </div>
              )}
            </div>
          )}

          <DialogFooter className="flex-wrap justify-between sm:justify-between">
            <div className="flex flex-wrap gap-2">
              {order?.status === "Draft" && (
                <Button variant="secondary" loading={submit.isPending} onClick={() => runAction("submit")}>
                  Submit to supplier
                </Button>
              )}
              {order?.status === "Submitted" && (
                <Button variant="secondary" loading={confirm.isPending} onClick={() => runAction("confirm")}>
                  Mark confirmed
                </Button>
              )}
              {order && order.status !== "Delivered" && order.status !== "Cancelled" && (
                <Button variant="outline" loading={cancel.isPending} onClick={() => runAction("cancel")}>
                  Cancel order
                </Button>
              )}
              {order && order.status !== "Draft" && order.status !== "Cancelled" && order.balance > 0 && (
                <Button onClick={() => setPaymentDialogOpen(true)}>Record payment</Button>
              )}
            </div>
            <Button variant="outline" onClick={() => onOpenChange(false)}>
              Close
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {order && (
        <RecordPaymentDialog
          open={paymentDialogOpen}
          onOpenChange={setPaymentDialogOpen}
          purchaseOrderId={order.id}
          supplierId={order.supplierId}
          balance={order.balance}
        />
      )}
    </>
  );
}
