import type {
  AddOrderItemInput,
  DiscountType,
  Order,
  OrderMutationResult,
  OrderPaymentInput,
  OrderStatus,
  OrderSummary,
  ReceiptDocument,
} from "@/entities/order";
import { API_ENDPOINTS, apiService } from "@/shared/api/endpoints";

export interface OrderFilters {
  openOnly?: boolean;
  status?: OrderStatus;
  search?: string;
}

export const ordersApi = {
  list: (filters: OrderFilters = {}) =>
    apiService.get<OrderSummary[]>(API_ENDPOINTS.ORDERS.BASE, {
      openOnly: filters.openOnly,
      status: filters.status,
      search: filters.search || undefined,
    }),

  getById: (id: string) => apiService.get<Order>(API_ENDPOINTS.ORDERS.BY_ID(id)),

  create: (tableId: string) => apiService.post<Order>(API_ENDPOINTS.ORDERS.BASE, { tableId }),

  addItems: (id: string, items: AddOrderItemInput[]) =>
    apiService.post<OrderMutationResult>(API_ENDPOINTS.ORDERS.ITEMS(id), { items }),

  confirm: (id: string) => apiService.post<OrderMutationResult>(API_ENDPOINTS.ORDERS.CONFIRM(id)),

  changeItemQuantity: (id: string, itemId: string, quantity: number, pin: string | null) =>
    apiService.put<OrderMutationResult>(API_ENDPOINTS.ORDERS.ITEM_QUANTITY(id, itemId), { quantity, pin }),

  voidItem: (id: string, itemId: string, pin: string | null) =>
    apiService.post<OrderMutationResult>(API_ENDPOINTS.ORDERS.VOID_ITEM(id, itemId), { pin }),

  cancel: (id: string, pin: string | null, reason: string | null) =>
    apiService.post<OrderMutationResult>(API_ENDPOINTS.ORDERS.CANCEL(id), { pin, reason }),

  setDiscount: (id: string, type: DiscountType, value: number) =>
    apiService.put<OrderMutationResult>(API_ENDPOINTS.ORDERS.DISCOUNT(id), { type, value }),

  startCheckout: (id: string) => apiService.post<OrderMutationResult>(API_ENDPOINTS.ORDERS.CHECKOUT(id)),

  reopen: (id: string) => apiService.post<OrderMutationResult>(API_ENDPOINTS.ORDERS.REOPEN(id)),

  pay: (id: string, payments: OrderPaymentInput[]) =>
    apiService.post<ReceiptDocument>(API_ENDPOINTS.ORDERS.PAYMENTS(id), { payments }),

  reprintReceipt: (id: string) =>
    apiService.post<ReceiptDocument>(API_ENDPOINTS.ORDERS.REPRINT_RECEIPT(id)),
};
