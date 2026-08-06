import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import type {
  CreateGoodsReceivedNotePayload,
  CreateStockAdjustmentPayload,
  CreateStockReleasePayload,
  GoodsReceivedNote,
  StockMovementFilters,
  StockRelease,
} from "@/entities/inventory";
import { inventoryApi } from "../api/inventoryApi";

const MAIN_STORE_STOCK_KEY = "main-store-stock";
const MAIN_STORE_MOVEMENTS_KEY = "main-store-movements";
const KITCHEN_STOCK_KEY = "kitchen-stock";
const KITCHEN_MOVEMENTS_KEY = "kitchen-movements";
const GRNS_KEY = "goods-received-notes";
const RELEASES_KEY = "stock-releases";

export function useMainStoreStock(lowStockOnly = false) {
  return useQuery({
    queryKey: [MAIN_STORE_STOCK_KEY, lowStockOnly],
    queryFn: () => inventoryApi.mainStoreStock(lowStockOnly),
  });
}

export function useMainStoreMovements(filters: StockMovementFilters = {}) {
  return useQuery({
    queryKey: [MAIN_STORE_MOVEMENTS_KEY, filters],
    queryFn: () => inventoryApi.mainStoreMovements(filters),
  });
}

export function useKitchenStock(lowStockOnly = false) {
  return useQuery({
    queryKey: [KITCHEN_STOCK_KEY, lowStockOnly],
    queryFn: () => inventoryApi.kitchenStock(lowStockOnly),
  });
}

export function useKitchenMovements(filters: StockMovementFilters = {}) {
  return useQuery({
    queryKey: [KITCHEN_MOVEMENTS_KEY, filters],
    queryFn: () => inventoryApi.kitchenMovements(filters),
  });
}

export function useGoodsReceivedNotes() {
  return useQuery({ queryKey: [GRNS_KEY], queryFn: inventoryApi.goodsReceivedNotes });
}

export function useStockReleases() {
  return useQuery({ queryKey: [RELEASES_KEY], queryFn: inventoryApi.releases });
}

/** Commands that move stock. Each invalidates every view the change could affect. */
export function useInventoryMutations() {
  const queryClient = useQueryClient();

  const invalidateMainStore = () => {
    queryClient.invalidateQueries({ queryKey: [MAIN_STORE_STOCK_KEY] });
    queryClient.invalidateQueries({ queryKey: [MAIN_STORE_MOVEMENTS_KEY] });
  };

  const invalidateKitchen = () => {
    queryClient.invalidateQueries({ queryKey: [KITCHEN_STOCK_KEY] });
    queryClient.invalidateQueries({ queryKey: [KITCHEN_MOVEMENTS_KEY] });
  };

  const createGoodsReceivedNote = useMutation<GoodsReceivedNote, Error, CreateGoodsReceivedNotePayload>({
    mutationFn: inventoryApi.createGoodsReceivedNote,
    onSuccess: () => {
      invalidateMainStore();
      queryClient.invalidateQueries({ queryKey: [GRNS_KEY] });
    },
  });

  const createRelease = useMutation<StockRelease, Error, CreateStockReleasePayload>({
    mutationFn: inventoryApi.createRelease,
    onSuccess: () => {
      invalidateMainStore();
      invalidateKitchen();
      queryClient.invalidateQueries({ queryKey: [RELEASES_KEY] });
    },
  });

  const createMainStoreAdjustment = useMutation<
    Awaited<ReturnType<typeof inventoryApi.createAdjustment>>,
    Error,
    CreateStockAdjustmentPayload
  >({
    mutationFn: (payload) => inventoryApi.createAdjustment("MainStore", payload),
    onSuccess: invalidateMainStore,
  });

  const createKitchenAdjustment = useMutation<
    Awaited<ReturnType<typeof inventoryApi.createAdjustment>>,
    Error,
    CreateStockAdjustmentPayload
  >({
    mutationFn: (payload) => inventoryApi.createAdjustment("Kitchen", payload),
    onSuccess: invalidateKitchen,
  });

  return { createGoodsReceivedNote, createRelease, createMainStoreAdjustment, createKitchenAdjustment };
}
