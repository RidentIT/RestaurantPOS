import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import type { Steward } from "@/entities/steward";
import { stewardsApi } from "../api/stewardsApi";

export const STEWARDS_KEY = "stewards";

/**
 * The steward roster. Pass `isActive: true` for the order screen's picker; the admin screen asks
 * for everyone so it can show and reinstate retired stewards.
 */
export function useStewards(isActive?: boolean) {
  return useQuery({
    queryKey: [STEWARDS_KEY, isActive ?? "all"],
    queryFn: () => stewardsApi.list(isActive),
  });
}

export function useStewardMutations() {
  const queryClient = useQueryClient();
  const invalidate = () => queryClient.invalidateQueries({ queryKey: [STEWARDS_KEY] });

  const create = useMutation<Steward, Error, string>({
    mutationFn: stewardsApi.create,
    onSuccess: invalidate,
  });

  const rename = useMutation<Steward, Error, { id: string; name: string }>({
    mutationFn: ({ id, name }) => stewardsApi.rename(id, name),
    onSuccess: invalidate,
  });

  const setActive = useMutation<Steward, Error, { id: string; isActive: boolean }>({
    mutationFn: ({ id, isActive }) => stewardsApi.setActive(id, isActive),
    onSuccess: invalidate,
  });

  return { create, rename, setActive };
}
