import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import type { Supplier, SupplierFilters, SupplierPayload, SupplierPrice } from "@/entities/supplier";
import { suppliersApi } from "../api/suppliersApi";

const SUPPLIERS_KEY = "suppliers";
const SUPPLIER_PRICES_KEY = "supplier-prices";
const SUPPLIER_PRICE_HISTORY_KEY = "supplier-price-history";
const SUPPLIER_PERFORMANCE_KEY = "supplier-performance";

export function useSuppliers(filters: SupplierFilters = {}) {
  return useQuery({
    queryKey: [SUPPLIERS_KEY, filters],
    queryFn: () => suppliersApi.list(filters),
    placeholderData: (previous) => previous,
  });
}

export function useSupplierMutations() {
  const queryClient = useQueryClient();
  const invalidate = () => queryClient.invalidateQueries({ queryKey: [SUPPLIERS_KEY] });

  const create = useMutation<Supplier, Error, SupplierPayload>({
    mutationFn: suppliersApi.create,
    onSuccess: invalidate,
  });

  const update = useMutation<Supplier, Error, { id: string; payload: SupplierPayload }>({
    mutationFn: ({ id, payload }) => suppliersApi.update(id, payload),
    onSuccess: invalidate,
  });

  const setActive = useMutation<Supplier, Error, { id: string; isActive: boolean }>({
    mutationFn: ({ id, isActive }) => suppliersApi.setActive(id, isActive),
    onSuccess: invalidate,
  });

  return { create, update, setActive };
}

/** The current price list, filterable by supplier (a price list) or raw material (a comparison). */
export function useSupplierPrices(supplierId?: string, rawMaterialId?: string) {
  return useQuery({
    queryKey: [SUPPLIER_PRICES_KEY, supplierId, rawMaterialId],
    queryFn: () => suppliersApi.prices(supplierId, rawMaterialId),
    enabled: !!supplierId || !!rawMaterialId,
  });
}

export function useSupplierPriceHistory(supplierId: string, rawMaterialId: string, enabled: boolean) {
  return useQuery({
    queryKey: [SUPPLIER_PRICE_HISTORY_KEY, supplierId, rawMaterialId],
    queryFn: () => suppliersApi.priceHistory(supplierId, rawMaterialId),
    enabled,
  });
}

export function useSetSupplierPrice() {
  const queryClient = useQueryClient();

  return useMutation<SupplierPrice, Error, { supplierId: string; rawMaterialId: string; price: number }>({
    mutationFn: ({ supplierId, rawMaterialId, price }) => suppliersApi.setPrice(supplierId, rawMaterialId, price),
    onSuccess: (_, { supplierId, rawMaterialId }) => {
      queryClient.invalidateQueries({ queryKey: [SUPPLIER_PRICES_KEY] });
      queryClient.invalidateQueries({ queryKey: [SUPPLIER_PRICE_HISTORY_KEY, supplierId, rawMaterialId] });
    },
  });
}

export function useSupplierPerformance(supplierId: string, enabled: boolean) {
  return useQuery({
    queryKey: [SUPPLIER_PERFORMANCE_KEY, supplierId],
    queryFn: () => suppliersApi.performance(supplierId),
    enabled,
  });
}
