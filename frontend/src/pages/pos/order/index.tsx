import { useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { ArrowLeft, Ban, CreditCard, Percent, Send, UserRound } from "lucide-react";
import { toast } from "sonner";
import type { DiscountType, OrderItem } from "@/entities/order";
import { useMenuItems } from "@/features/menu-items";
import { ManagerPinDialog, useOrder, useOrderMutations } from "@/features/orders";
import { toApiError } from "@/shared/api/problem";
import { Badge, Button, Card, LoadingState } from "@/shared/ui";
import type { PickedMenuItem } from "./AddItemDialog";
import { AddItemDialog } from "./AddItemDialog";
import { BillPanel } from "./BillPanel";
import { DiscountDialog } from "./DiscountDialog";
import { MenuPicker } from "./MenuPicker";
import { StewardDialog } from "./StewardDialog";

/** A guarded action waiting on a manager's PIN. */
interface PendingApproval {
  action: string;
  run: (pin: string) => Promise<void>;
}

/**
 * One table's bill: the menu on the left, the running total on the right (POS-006).
 *
 * Which actions need a manager depends on whether the kitchen has the order yet. While it is a
 * draft the cashier edits freely; once confirmed, changing or removing a line needs approval
 * (BR-POS-007, BR-POS-008) — but adding never does (BR-POS-005).
 */
export default function OrderScreen() {
  const { orderId } = useParams<{ orderId: string }>();
  const navigate = useNavigate();

  const { data: order, isLoading } = useOrder(orderId);
  const { data: menuItems } = useMenuItems();
  const mutations = useOrderMutations();

  const [picked, setPicked] = useState<PickedMenuItem | null>(null);
  const [discountOpen, setDiscountOpen] = useState(false);
  const [stewardOpen, setStewardOpen] = useState(false);
  const [approval, setApproval] = useState<PendingApproval | null>(null);

  // A bill already frozen for payment has nothing to do here — the menu and the bill are both
  // read-only in this state, and only the checkout screen can move it forward. Reachable whenever
  // something other than "Back to the order" brings a cashier back to this screen mid-payment:
  // Electron's own back navigation, a table tapped again from the floor plan before that list
  // refreshes, or a tab left open on this order from an earlier visit.
  useEffect(() => {
    if (order?.status === "Checkout") {
      navigate(`/pos/orders/${order.id}/checkout`, { replace: true });
    }
  }, [order?.status, order?.id, navigate]);

  const busy =
    mutations.addItems.isPending ||
    mutations.changeQuantity.isPending ||
    mutations.voidItem.isPending ||
    mutations.confirm.isPending ||
    mutations.cancel.isPending ||
    mutations.setDiscount.isPending ||
    mutations.assignSteward.isPending ||
    mutations.startCheckout.isPending;

  if (isLoading || !order || order.status === "Checkout") {
    return <LoadingState label="Loading the bill…" className="h-96" />;
  }

  const isDraft = order.status === "Draft";
  const isOpen = order.status === "Open";
  const activeItems = order.items.filter((item) => !item.isCancelled);

  /** Runs a command directly on a draft, or behind a PIN prompt once the kitchen has the order. */
  const guarded = (action: string, run: (pin: string | null) => Promise<void>) => {
    if (isDraft) {
      void run(null).catch((error) => toast.error(toApiError(error).message));
      return;
    }

    setApproval({ action, run: (pin) => run(pin) });
  };

  const addItem = async (quantity: number, specialInstructions: string | null) => {
    if (!picked) return;

    try {
      await mutations.addItems.mutateAsync({
        id: order.id,
        items: [{ menuItemVariantId: picked.variant.id, quantity, specialInstructions }],
      });
      setPicked(null);
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  };

  const changeQuantity = (item: OrderItem, quantity: number) =>
    guarded(`Change ${item.menuItemName} from ${item.quantity} to ${quantity}.`, async (pin) => {
      await mutations.changeQuantity.mutateAsync({ id: order.id, itemId: item.id, quantity, pin });
    });

  const voidItem = (item: OrderItem) =>
    guarded(`Remove ${item.menuItemName} from the bill.`, async (pin) => {
      await mutations.voidItem.mutateAsync({ id: order.id, itemId: item.id, pin });
    });

  const cancelOrder = () =>
    guarded("Cancel this entire order.", async (pin) => {
      await mutations.cancel.mutateAsync({ id: order.id, pin, reason: null });
      toast.success("Order cancelled.");
      navigate("/pos");
    });

  const confirmOrder = async () => {
    try {
      await mutations.confirm.mutateAsync(order.id);
      toast.success("Order confirmed and sent to the kitchen.");
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  };

  const applyDiscount = async (type: DiscountType, value: number) => {
    try {
      await mutations.setDiscount.mutateAsync({ id: order.id, type, value });
      setDiscountOpen(false);
      toast.success(type === "None" ? "Discount removed." : "Discount applied.");
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  };

  const goToCheckout = async () => {
    try {
      await mutations.startCheckout.mutateAsync(order.id);
      navigate(`/pos/orders/${order.id}/checkout`);
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  };

  return (
    <div className="flex h-full flex-col gap-4 p-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="space-y-1">
          <Button variant="ghost" size="sm" asChild className="-ml-2">
            <Link to="/pos">
              <ArrowLeft /> POS &amp; Billing
            </Link>
          </Button>
          <div className="flex flex-wrap items-center gap-2">
            <h1 className="text-2xl font-semibold">
              {order.tableNumber ? `Table ${order.tableNumber}` : "Takeaway"}
            </h1>
            {order.orderNumber && (
              <Badge variant="secondary">Order #{String(order.orderNumber).padStart(3, "0")}</Badge>
            )}
            <Badge variant={isDraft ? "outline" : "default"}>{order.status}</Badge>
            {order.kitchenStatus && <Badge variant="warning">Kitchen: {order.kitchenStatus}</Badge>}
          </div>
          {order.tableId && (
            <Button
              variant={order.stewardName ? "ghost" : "outline"}
              size="sm"
              className="-ml-2"
              onClick={() => setStewardOpen(true)}
              disabled={busy}
            >
              <UserRound />
              {order.stewardName ? `Steward: ${order.stewardName}` : "Assign steward"}
            </Button>
          )}
        </div>

        <div className="flex flex-wrap gap-2">
          {(isDraft || isOpen) && (
            <Button variant="outline" onClick={() => setDiscountOpen(true)} disabled={busy}>
              <Percent /> Discount
            </Button>
          )}
          {(isDraft || isOpen) && (
            <Button variant="outline" onClick={cancelOrder} disabled={busy}>
              <Ban /> Cancel order
            </Button>
          )}
          {isDraft && (
            <Button
              onClick={confirmOrder}
              loading={mutations.confirm.isPending}
              disabled={activeItems.length === 0}
            >
              <Send /> Confirm &amp; send to kitchen
            </Button>
          )}
          {isOpen && (
            <Button
              onClick={goToCheckout}
              loading={mutations.startCheckout.isPending}
              disabled={activeItems.length === 0}
            >
              <CreditCard /> Checkout
            </Button>
          )}
        </div>
      </div>

      {isDraft && (
        <p className="rounded-lg border border-dashed bg-muted/40 px-4 py-2 text-sm text-muted-foreground">
          Nothing has been sent to the kitchen yet. Confirm the order to print the KOT — after that
          you can keep adding items without a manager PIN.
        </p>
      )}

      <div className="grid min-h-0 flex-1 gap-4 lg:grid-cols-[1fr_24rem]">
        <Card className="min-h-0 overflow-hidden p-4">
          <MenuPicker
            menuItems={(menuItems ?? []).filter((m) => m.isActive)}
            onPick={(item, variant) => setPicked({ item, variant })}
            disabled={busy || (!isDraft && !isOpen)}
          />
        </Card>

        <Card className="min-h-0 overflow-y-auto p-4">
          <h2 className="mb-3 font-semibold">Current bill</h2>
          <BillPanel
            order={order}
            onChangeQuantity={changeQuantity}
            onVoid={voidItem}
            busy={busy}
          />
        </Card>
      </div>

      <AddItemDialog
        picked={picked}
        onOpenChange={(open) => !open && setPicked(null)}
        onAdd={addItem}
        pending={mutations.addItems.isPending}
      />

      <DiscountDialog
        open={discountOpen}
        onOpenChange={setDiscountOpen}
        order={order}
        onApply={applyDiscount}
        pending={mutations.setDiscount.isPending}
      />

      <StewardDialog
        open={stewardOpen}
        onOpenChange={setStewardOpen}
        currentStewardId={order.stewardId}
        currentStewardName={order.stewardName}
        onAssign={(stewardId) =>
          mutations.assignSteward.mutateAsync({ id: order.id, stewardId }).then(() => undefined)
        }
        pending={mutations.assignSteward.isPending}
      />

      <ManagerPinDialog
        open={!!approval}
        onOpenChange={(open) => !open && setApproval(null)}
        action={approval?.action ?? ""}
        pending={busy}
        onConfirm={async (pin) => {
          try {
            await approval!.run(pin);
          } catch (error) {
            // Rethrown so the dialog stays open and shows how many attempts are left.
            throw new Error(toApiError(error).message);
          }
        }}
      />
    </div>
  );
}
