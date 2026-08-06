import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import type { RestaurantTable, TablePayload } from "@/entities/table";
import { tablesApi } from "../api/tablesApi";

export const TABLES_KEY = "tables";

/**
 * The floor plan.
 *
 * Polled rather than fetched once: a table's tile shows kitchen progress, which changes on a
 * different screen in a different room. Without polling, a cashier would be looking at a board
 * that says "preparing" for food that was plated ten minutes ago.
 */
export function useTables(options: { isActive?: boolean; pollMs?: number } = {}) {
  const { isActive, pollMs = 10_000 } = options;

  return useQuery({
    queryKey: [TABLES_KEY, isActive],
    queryFn: () => tablesApi.list(isActive),
    refetchInterval: pollMs,
    placeholderData: (previous) => previous,
  });
}

export function useTableMutations() {
  const queryClient = useQueryClient();
  const invalidate = () => queryClient.invalidateQueries({ queryKey: [TABLES_KEY] });

  const create = useMutation<RestaurantTable, Error, TablePayload>({
    mutationFn: tablesApi.create,
    onSuccess: invalidate,
  });

  const update = useMutation<RestaurantTable, Error, { id: string; payload: TablePayload }>({
    mutationFn: ({ id, payload }) => tablesApi.update(id, payload),
    onSuccess: invalidate,
  });

  const setActive = useMutation<RestaurantTable, Error, { id: string; isActive: boolean }>({
    mutationFn: ({ id, isActive }) => tablesApi.setActive(id, isActive),
    onSuccess: invalidate,
  });

  return { create, update, setActive };
}
