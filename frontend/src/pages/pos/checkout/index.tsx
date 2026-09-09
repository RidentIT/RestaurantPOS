import { useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { ArrowLeft, Banknote, CheckCircle2, Plus, Printer, Trash2 } from "lucide-react";
import { toast } from "sonner";
import type { OrderPaymentMethod } from "@/entities/order";
import { ORDER_PAYMENT_METHODS, PAYMENT_METHOD_LABELS } from "@/entities/order";
import { useOrder, useOrderMutations } from "@/features/orders";
import { toApiError } from "@/shared/api/problem";
import {
  Alert,
  AlertDescription,
  Badge,
  Button,
  Card,
  Input,
  Label,
  LoadingState,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Separator,
} from "@/shared/ui";
import { cn } from "@/shared/lib/utils";

/** One tender line being keyed in. Amounts stay as strings until submitted so the field can be empty. */
interface TenderDraft {
  id: string;
  method: OrderPaymentMethod;
  amount: string;
  tendered: string;
}

const newTender = (amount: string): TenderDraft => ({
  id: crypto.randomUUID(),
  method: "Cash",
  amount,
  tendered: "",
});

/**
 * The payment screen (POS-023, POS-024).
 *
 * Starts with a single tender pre-filled to the whole bill, since that is what almost every table
 * does; splitting is one tap away for the ones that do not. Nothing is sent until the tenders add
 * up to the bill exactly (BR-POS-013), so the button says why it is disabled rather than failing
 * after the customer has handed over money.
 */
export default function CheckoutScreen() {
  const { orderId } = useParams<{ orderId: string }>();
  const navigate = useNavigate();

  const { data: order, isLoading } = useOrder(orderId);
  const { pay, reopen, reprintReceipt } = useOrderMutations();

  const [tenders, setTenders] = useState<TenderDraft[] | null>(null);

  // Seeded from the bill on first render, once the order has actually loaded.
  const rows = useMemo(() => {
    if (tenders) return tenders;
    if (!order) return [];

    return [newTender(order.total.toFixed(2))];
  }, [tenders, order]);

  if (isLoading || !order) {
    return <LoadingState label="Loading the bill…" className="h-96" />;
  }

  const isSettled = order.status === "Completed";
  const paidTotal = rows.reduce((sum, row) => sum + (Number(row.amount) || 0), 0);
  const remaining = Math.round((order.total - paidTotal) * 100) / 100;

  const changeDue = rows.reduce((sum, row) => {
    const tendered = Number(row.tendered);
    const amount = Number(row.amount) || 0;

    return sum + (Number.isFinite(tendered) && tendered > amount ? tendered - amount : 0);
  }, 0);

  const update = (id: string, patch: Partial<TenderDraft>) =>
    setTenders(rows.map((row) => (row.id === id ? { ...row, ...patch } : row)));

  const addTender = () => setTenders([...rows, newTender(remaining > 0 ? remaining.toFixed(2) : "")]);

  const removeTender = (id: string) => setTenders(rows.filter((row) => row.id !== id));

  const completePayment = async () => {
    try {
      await pay.mutateAsync({
        id: order.id,
        payments: rows.map((row) => ({
          method: row.method,
          amount: Number(row.amount),
          tenderedAmount: row.tendered.trim() === "" ? null : Number(row.tendered),
          reference: null,
        })),
      });
      toast.success("Payment complete. Receipt printed.");
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  };

  const backToOrder = async () => {
    try {
      await reopen.mutateAsync(order.id);
      navigate(`/pos/orders/${order.id}`);
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  };

  if (isSettled) {
    return (
      <div className="mx-auto max-w-lg space-y-6 p-8">
        <Card className="space-y-4 p-8 text-center">
          <CheckCircle2 className="mx-auto size-14 text-success" />
          <div>
            <h1 className="text-2xl font-semibold">Payment complete</h1>
            <p className="text-sm text-muted-foreground">
              {order.tableNumber ? `Table ${order.tableNumber} is free again` : "Takeaway order settled"} · Receipt{" "}
              {order.receiptNumber}
            </p>
          </div>
          <p className="text-3xl font-semibold tabular">{order.total.toFixed(2)}</p>
          {order.changeDue > 0 && (
            <p className="text-sm text-muted-foreground">
              Change given: <span className="font-medium tabular">{order.changeDue.toFixed(2)}</span>
            </p>
          )}

          <div className="flex justify-center gap-2 pt-2">
            <Button
              variant="outline"
              onClick={() => reprintReceipt.mutate(order.id)}
              loading={reprintReceipt.isPending}
            >
              <Printer /> Reprint receipt
            </Button>
            <Button onClick={() => navigate("/pos")}>Back to POS &amp; Billing</Button>
          </div>
        </Card>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-3xl space-y-4 p-6">
      <div className="space-y-1">
        <Button variant="ghost" size="sm" onClick={backToOrder} disabled={reopen.isPending} className="-ml-2">
          <ArrowLeft /> Back to the order
        </Button>
        <div className="flex flex-wrap items-center gap-2">
          <h1 className="text-2xl font-semibold">
            Checkout · {order.tableNumber ? `Table ${order.tableNumber}` : "Takeaway"}
          </h1>
          {order.orderNumber && (
            <Badge variant="secondary">Order #{String(order.orderNumber).padStart(3, "0")}</Badge>
          )}
        </div>
      </div>

      <Card className="p-5">
        <h2 className="mb-3 font-semibold">Bill</h2>
        <ul className="space-y-1.5 text-sm">
          {order.items
            .filter((item) => !item.isCancelled)
            .map((item) => (
              <li key={item.id} className="flex justify-between gap-4">
                <span>
                  {item.menuItemName} <span className="text-muted-foreground">× {item.quantity}</span>
                </span>
                <span className="tabular">{item.lineTotal.toFixed(2)}</span>
              </li>
            ))}
        </ul>

        <Separator className="my-3" />

        <dl className="space-y-1 text-sm">
          <div className="flex justify-between">
            <dt className="text-muted-foreground">Subtotal</dt>
            <dd className="tabular">{order.subtotal.toFixed(2)}</dd>
          </div>
          <div className="flex justify-between">
            <dt className="text-muted-foreground">Discount</dt>
            <dd className="tabular">−{order.discountAmount.toFixed(2)}</dd>
          </div>
          {order.serviceChargeAmount > 0 && (
            <div className="flex justify-between">
              <dt className="text-muted-foreground">Service charge ({order.serviceChargeRatePercent}%)</dt>
              <dd className="tabular">{order.serviceChargeAmount.toFixed(2)}</dd>
            </div>
          )}
          {order.taxAmount > 0 && (
            <div className="flex justify-between">
              <dt className="text-muted-foreground">Tax / VAT ({order.taxRatePercent}%)</dt>
              <dd className="tabular">{order.taxAmount.toFixed(2)}</dd>
            </div>
          )}
          <div className="flex justify-between text-xl font-semibold">
            <dt>Total</dt>
            <dd className="tabular">{order.total.toFixed(2)}</dd>
          </div>
        </dl>
      </Card>

      <Card className="space-y-3 p-5">
        <div className="flex items-center justify-between">
          <h2 className="font-semibold">Payment</h2>
          <Button variant="outline" size="sm" onClick={addTender}>
            <Plus /> Split payment
          </Button>
        </div>

        {rows.map((row, index) => (
          <div key={row.id} className="grid gap-2 sm:grid-cols-[10rem_1fr_1fr_auto] sm:items-end">
            <div className="space-y-1.5">
              <Label htmlFor={`method-${row.id}`}>Method</Label>
              <Select
                value={row.method}
                onValueChange={(value) => update(row.id, { method: value as OrderPaymentMethod })}
              >
                <SelectTrigger id={`method-${row.id}`}>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {ORDER_PAYMENT_METHODS.map((method) => (
                    <SelectItem key={method} value={method}>
                      {PAYMENT_METHOD_LABELS[method]}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-1.5">
              <Label htmlFor={`amount-${row.id}`}>Amount</Label>
              <Input
                id={`amount-${row.id}`}
                inputMode="decimal"
                value={row.amount}
                onChange={(event) => update(row.id, { amount: event.target.value })}
                className="tabular"
              />
            </div>

            <div className="space-y-1.5">
              <Label htmlFor={`tendered-${row.id}`}>
                {row.method === "Cash" ? "Cash given" : "Reference"}
              </Label>
              <Input
                id={`tendered-${row.id}`}
                inputMode="decimal"
                value={row.tendered}
                onChange={(event) => update(row.id, { tendered: event.target.value })}
                placeholder={row.method === "Cash" ? "For change" : "—"}
                disabled={row.method !== "Cash"}
                className="tabular"
              />
            </div>

            <Button
              variant="ghost"
              size="icon"
              aria-label={`Remove payment ${index + 1}`}
              disabled={rows.length === 1}
              onClick={() => removeTender(row.id)}
            >
              <Trash2 className="size-4 text-destructive" />
            </Button>
          </div>
        ))}

        <Separator />

        <div className="space-y-1 text-sm">
          <div className="flex justify-between">
            <span className="text-muted-foreground">Total paid</span>
            <span className="tabular">{paidTotal.toFixed(2)}</span>
          </div>
          <div className={cn("flex justify-between", remaining !== 0 && "font-medium text-destructive")}>
            <span className={cn(remaining === 0 && "text-muted-foreground")}>
              {remaining > 0 ? "Still to pay" : remaining < 0 ? "Over by" : "Balance"}
            </span>
            <span className="tabular">{Math.abs(remaining).toFixed(2)}</span>
          </div>
          {changeDue > 0 && (
            <div className="flex justify-between text-lg font-semibold">
              <span>Change</span>
              <span className="tabular">{changeDue.toFixed(2)}</span>
            </div>
          )}
        </div>

        {remaining !== 0 && (
          <Alert variant="warning">
            <Banknote className="size-4" />
            <AlertDescription>
              The payments must come to exactly {order.total.toFixed(2)} before the bill can be settled.
            </AlertDescription>
          </Alert>
        )}

        <Button
          className="w-full"
          size="lg"
          onClick={completePayment}
          loading={pay.isPending}
          disabled={remaining !== 0}
        >
          Complete payment &amp; print receipt
        </Button>
      </Card>
    </div>
  );
}
