import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import type {
  CreatePurchaseOrderPayload,
  PurchaseOrder,
  PurchaseOrderFilters,
  RecordSupplierPaymentPayload,
  SupplierPayment,
  UpdatePurchaseOrderPayload,
} from "@/entities/supplier";
import { purchaseOrdersApi } from "../api/purchaseOrdersApi";

const PURCHASE_ORDERS_KEY = "purchase-orders";
const PURCHASE_ORDER_KEY = "purchase-order";
const PURCHASE_ORDER_PAYMENTS_KEY = "purchase-order-payments";
const SUPPLIER_PERFORMANCE_KEY = "supplier-performance";

export function usePurchaseOrders(filters: PurchaseOrderFilters = {}, enabled = true) {
  return useQuery({
    queryKey: [PURCHASE_ORDERS_KEY, filters],
    queryFn: () => purchaseOrdersApi.list(filters),
    placeholderData: (previous) => previous,
    enabled,
  });
}

export function usePurchaseOrder(id: string | undefined) {
  return useQuery({
    queryKey: [PURCHASE_ORDER_KEY, id],
    queryFn: () => purchaseOrdersApi.getById(id!),
    enabled: !!id,
  });
}

export function usePurchaseOrderPayments(id: string | undefined) {
  return useQuery({
    queryKey: [PURCHASE_ORDER_PAYMENTS_KEY, id],
    queryFn: () => purchaseOrdersApi.payments(id!),
    enabled: !!id,
  });
}

/** Commands across a purchase order's lifecycle: draft edits, status transitions, and payments. */
export function usePurchaseOrderMutations() {
  const queryClient = useQueryClient();

  const invalidate = (id?: string) => {
    queryClient.invalidateQueries({ queryKey: [PURCHASE_ORDERS_KEY] });
    if (id) queryClient.invalidateQueries({ queryKey: [PURCHASE_ORDER_KEY, id] });
  };

  const create = useMutation<PurchaseOrder, Error, CreatePurchaseOrderPayload>({
    mutationFn: purchaseOrdersApi.create,
    onSuccess: () => invalidate(),
  });

  const update = useMutation<PurchaseOrder, Error, { id: string; payload: UpdatePurchaseOrderPayload }>({
    mutationFn: ({ id, payload }) => purchaseOrdersApi.update(id, payload),
    onSuccess: (_, { id }) => invalidate(id),
  });

  const submit = useMutation<PurchaseOrder, Error, string>({
    mutationFn: purchaseOrdersApi.submit,
    onSuccess: (_, id) => invalidate(id),
  });

  const confirm = useMutation<PurchaseOrder, Error, string>({
    mutationFn: purchaseOrdersApi.confirm,
    onSuccess: (_, id) => invalidate(id),
  });

  const cancel = useMutation<PurchaseOrder, Error, string>({
    mutationFn: purchaseOrdersApi.cancel,
    onSuccess: (_, id) => invalidate(id),
  });

  const recordPayment = useMutation<
    SupplierPayment,
    Error,
    { id: string; supplierId: string; payload: RecordSupplierPaymentPayload }
  >({
    mutationFn: ({ id, payload }) => purchaseOrdersApi.recordPayment(id, payload),
    onSuccess: (_, { id, supplierId }) => {
      invalidate(id);
      queryClient.invalidateQueries({ queryKey: [PURCHASE_ORDER_PAYMENTS_KEY, id] });
      queryClient.invalidateQueries({ queryKey: [SUPPLIER_PERFORMANCE_KEY, supplierId] });
    },
  });

  return { create, update, submit, confirm, cancel, recordPayment };
}
