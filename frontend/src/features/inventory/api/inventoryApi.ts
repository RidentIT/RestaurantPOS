import type {
  CreateGoodsReceivedNotePayload,
  CreateStockAdjustmentPayload,
  CreateStockReleasePayload,
  GoodsReceivedNote,
  GoodsReceivedNoteSummary,
  StockLevel,
  StockMovement,
  StockMovementFilters,
  StockRelease,
  StockReleaseSummary,
} from "@/entities/inventory";
import { API_ENDPOINTS, apiService } from "@/shared/api/endpoints";

export const inventoryApi = {
  mainStoreStock: (lowStockOnly = false) =>
    apiService.get<StockLevel[]>(API_ENDPOINTS.INVENTORY.MAIN_STORE_STOCK, { lowStockOnly }),

  mainStoreMovements: (filters: StockMovementFilters = {}) =>
    apiService.get<StockMovement[]>(API_ENDPOINTS.INVENTORY.MAIN_STORE_MOVEMENTS, {
      rawMaterialId: filters.rawMaterialId || undefined,
      from: filters.from || undefined,
      to: filters.to || undefined,
    }),

  createAdjustment: (store: "MainStore" | "Kitchen", payload: CreateStockAdjustmentPayload) =>
    apiService.post<StockMovement>(
      store === "MainStore" ? API_ENDPOINTS.INVENTORY.MAIN_STORE_ADJUSTMENTS : API_ENDPOINTS.INVENTORY.KITCHEN_ADJUSTMENTS,
      payload,
    ),

  goodsReceivedNotes: () => apiService.get<GoodsReceivedNoteSummary[]>(API_ENDPOINTS.INVENTORY.GOODS_RECEIVED),

  goodsReceivedNoteById: (id: string) =>
    apiService.get<GoodsReceivedNote>(API_ENDPOINTS.INVENTORY.GOODS_RECEIVED_BY_ID(id)),

  createGoodsReceivedNote: (payload: CreateGoodsReceivedNotePayload) =>
    apiService.post<GoodsReceivedNote>(API_ENDPOINTS.INVENTORY.GOODS_RECEIVED, payload),

  kitchenStock: (lowStockOnly = false) =>
    apiService.get<StockLevel[]>(API_ENDPOINTS.INVENTORY.KITCHEN_STOCK, { lowStockOnly }),

  kitchenMovements: (filters: StockMovementFilters = {}) =>
    apiService.get<StockMovement[]>(API_ENDPOINTS.INVENTORY.KITCHEN_MOVEMENTS, {
      rawMaterialId: filters.rawMaterialId || undefined,
      from: filters.from || undefined,
      to: filters.to || undefined,
    }),

  releases: () => apiService.get<StockReleaseSummary[]>(API_ENDPOINTS.INVENTORY.RELEASES),

  releaseById: (id: string) => apiService.get<StockRelease>(API_ENDPOINTS.INVENTORY.RELEASE_BY_ID(id)),

  createRelease: (payload: CreateStockReleasePayload) =>
    apiService.post<StockRelease>(API_ENDPOINTS.INVENTORY.RELEASES, payload),
};
