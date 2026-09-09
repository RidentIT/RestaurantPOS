import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import type {
  AddOrderItemInput,
  DiscountType,
  Order,
  OrderMutationResult,
  OrderPaymentInput,
  ReceiptDocument,
} from "@/entities/order";
import { printKot, printReceipt } from "@/features/printing";
import { TABLES_KEY } from "@/features/tables";
import { ordersApi, type OrderFilters } from "../api/ordersApi";

export const ORDERS_KEY = "orders";
export const ORDER_KEY = "order";

export function useOrders(filters: OrderFilters = {}, pollMs = 10_000) {
  return useQuery({
    queryKey: [ORDERS_KEY, filters],
    queryFn: () => ordersApi.list(filters),
    refetchInterval: pollMs,
    placeholderData: (previous) => previous,
  });
}

export function useOrder(id: string | undefined) {
  return useQuery({
    queryKey: [ORDER_KEY, id],
    queryFn: () => ordersApi.getById(id!),
    enabled: !!id,
  });
}

/**
 * Every command the till can issue against a bill.
 *
 * Any response carrying a kitchen slip is printed here rather than at the call site. The rule
 * that the kitchen is told whenever a confirmed order changes (BR-POS-004, BR-POS-006, POS-020)
 * is not something a cashier should be able to forget, and it would be forgotten eventually if
 * each of the six screens that can change an order had to remember it separately.
 */
export function useOrderMutations() {
  const queryClient = useQueryClient();

  const refresh = (orderId?: string) => {
    queryClient.invalidateQueries({ queryKey: [ORDERS_KEY] });
    queryClient.invalidateQueries({ queryKey: [TABLES_KEY] });
    if (orderId) queryClient.invalidateQueries({ queryKey: [ORDER_KEY, orderId] });
  };

  const handleMutation = async (result: OrderMutationResult) => {
    refresh(result.order.id);

    if (!result.kot) {
      return;
    }

    const outcome = await printKot(result.kot);

    if (!outcome.success) {
      // The order itself is already saved, so this is a printing problem, not an ordering one —
      // said plainly so the cashier walks the slip to the kitchen rather than re-keying anything.
      toast.warning(`Order saved, but the kitchen slip did not print: ${outcome.message}`);
    }
  };

  const create = useMutation<Order, Error, string | null>({
    mutationFn: ordersApi.create,
    onSuccess: (order) => refresh(order.id),
  });

  const addItems = useMutation<OrderMutationResult, Error, { id: string; items: AddOrderItemInput[] }>({
    mutationFn: ({ id, items }) => ordersApi.addItems(id, items),
    onSuccess: handleMutation,
  });

  const confirm = useMutation<OrderMutationResult, Error, string>({
    mutationFn: ordersApi.confirm,
    onSuccess: handleMutation,
  });

  const changeQuantity = useMutation<
    OrderMutationResult,
    Error,
    { id: string; itemId: string; quantity: number; pin: string | null }
  >({
    mutationFn: ({ id, itemId, quantity, pin }) => ordersApi.changeItemQuantity(id, itemId, quantity, pin),
    onSuccess: handleMutation,
  });

  const voidItem = useMutation<
    OrderMutationResult,
    Error,
    { id: string; itemId: string; pin: string | null }
  >({
    mutationFn: ({ id, itemId, pin }) => ordersApi.voidItem(id, itemId, pin),
    onSuccess: handleMutation,
  });

  const cancel = useMutation<
    OrderMutationResult,
    Error,
    { id: string; pin: string | null; reason: string | null }
  >({
    mutationFn: ({ id, pin, reason }) => ordersApi.cancel(id, pin, reason),
    onSuccess: handleMutation,
  });

  const setDiscount = useMutation<
    OrderMutationResult,
    Error,
    { id: string; type: DiscountType; value: number }
  >({
    mutationFn: ({ id, type, value }) => ordersApi.setDiscount(id, type, value),
    onSuccess: handleMutation,
  });

  const startCheckout = useMutation<OrderMutationResult, Error, string>({
    mutationFn: ordersApi.startCheckout,
    onSuccess: handleMutation,
  });

  const reopen = useMutation<OrderMutationResult, Error, string>({
    mutationFn: ordersApi.reopen,
    onSuccess: handleMutation,
  });

  const pay = useMutation<ReceiptDocument, Error, { id: string; payments: OrderPaymentInput[] }>({
    mutationFn: ({ id, payments }) => ordersApi.pay(id, payments),
    onSuccess: async (receipt, { id }) => {
      refresh(id);
      const outcome = await printReceipt(receipt);

      if (!outcome.success) {
        toast.warning(`Payment recorded, but the receipt did not print: ${outcome.message}`);
      }
    },
  });

  const reprintReceipt = useMutation<ReceiptDocument, Error, string>({
    mutationFn: ordersApi.reprintReceipt,
    onSuccess: async (receipt) => {
      const outcome = await printReceipt(receipt);

      if (!outcome.success) {
        toast.error(`The receipt did not print: ${outcome.message}`);
      }
    },
  });

  return {
    create,
    addItems,
    confirm,
    changeQuantity,
    voidItem,
    cancel,
    setDiscount,
    startCheckout,
    reopen,
    pay,
    reprintReceipt,
  };
}
