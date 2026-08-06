import type {
  CreatePurchaseOrderPayload,
  PurchaseOrder,
  PurchaseOrderFilters,
  PurchaseOrderSummary,
  RecordSupplierPaymentPayload,
  SupplierPayment,
  UpdatePurchaseOrderPayload,
} from "@/entities/supplier";
import { API_ENDPOINTS, apiService } from "@/shared/api/endpoints";

export const purchaseOrdersApi = {
  list: (filters: PurchaseOrderFilters = {}) =>
    apiService.get<PurchaseOrderSummary[]>(API_ENDPOINTS.PURCHASE_ORDERS.BASE, {
      supplierId: filters.supplierId,
      status: filters.status,
    }),

  getById: (id: string) => apiService.get<PurchaseOrder>(API_ENDPOINTS.PURCHASE_ORDERS.BY_ID(id)),

  create: (payload: CreatePurchaseOrderPayload) =>
    apiService.post<PurchaseOrder>(API_ENDPOINTS.PURCHASE_ORDERS.BASE, payload),

  update: (id: string, payload: UpdatePurchaseOrderPayload) =>
    apiService.put<PurchaseOrder>(API_ENDPOINTS.PURCHASE_ORDERS.BY_ID(id), payload),

  submit: (id: string) => apiService.post<PurchaseOrder>(API_ENDPOINTS.PURCHASE_ORDERS.SUBMIT(id)),

  confirm: (id: string) => apiService.post<PurchaseOrder>(API_ENDPOINTS.PURCHASE_ORDERS.CONFIRM(id)),

  cancel: (id: string) => apiService.post<PurchaseOrder>(API_ENDPOINTS.PURCHASE_ORDERS.CANCEL(id)),

  payments: (id: string) => apiService.get<SupplierPayment[]>(API_ENDPOINTS.PURCHASE_ORDERS.PAYMENTS(id)),

  recordPayment: (id: string, payload: RecordSupplierPaymentPayload) =>
    apiService.post<SupplierPayment>(API_ENDPOINTS.PURCHASE_ORDERS.PAYMENTS(id), payload),
};
