import type {
  Supplier,
  SupplierPayload,
  SupplierFilters,
  SupplierPrice,
  SupplierPriceHistoryEntry,
  SupplierPerformance,
} from "@/entities/supplier";
import { API_ENDPOINTS, apiService } from "@/shared/api/endpoints";

export const suppliersApi = {
  list: (filters: SupplierFilters = {}) =>
    apiService.get<Supplier[]>(API_ENDPOINTS.SUPPLIERS.BASE, {
      search: filters.search || undefined,
      isActive: filters.isActive,
    }),

  create: (payload: SupplierPayload) => apiService.post<Supplier>(API_ENDPOINTS.SUPPLIERS.BASE, payload),

  update: (id: string, payload: SupplierPayload) =>
    apiService.put<Supplier>(API_ENDPOINTS.SUPPLIERS.BY_ID(id), payload),

  setActive: (id: string, isActive: boolean) =>
    apiService.put<Supplier>(API_ENDPOINTS.SUPPLIERS.STATUS(id), { isActive }),

  prices: (supplierId?: string, rawMaterialId?: string) =>
    apiService.get<SupplierPrice[]>(API_ENDPOINTS.SUPPLIERS.PRICES, { supplierId, rawMaterialId }),

  setPrice: (supplierId: string, rawMaterialId: string, price: number) =>
    apiService.put<SupplierPrice>(API_ENDPOINTS.SUPPLIERS.SET_PRICE(supplierId), { rawMaterialId, price }),

  priceHistory: (supplierId: string, rawMaterialId: string) =>
    apiService.get<SupplierPriceHistoryEntry[]>(API_ENDPOINTS.SUPPLIERS.PRICE_HISTORY(supplierId, rawMaterialId)),

  performance: (supplierId: string) =>
    apiService.get<SupplierPerformance>(API_ENDPOINTS.SUPPLIERS.PERFORMANCE(supplierId)),
};
