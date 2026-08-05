import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import type {
  CreateRawMaterialPayload,
  RawMaterial,
  RawMaterialFilters,
  UpdateRawMaterialPayload,
} from "@/entities/raw-material";
import { rawMaterialsApi } from "../api/rawMaterialsApi";

const RAW_MATERIALS_KEY = "raw-materials";

export function useRawMaterials(filters: RawMaterialFilters = {}) {
  return useQuery({
    queryKey: [RAW_MATERIALS_KEY, filters],
    queryFn: () => rawMaterialsApi.list(filters),
    placeholderData: (previous) => previous,
  });
}

export function useRawMaterialMutations() {
  const queryClient = useQueryClient();
  const invalidate = () => queryClient.invalidateQueries({ queryKey: [RAW_MATERIALS_KEY] });

  const create = useMutation<RawMaterial, Error, CreateRawMaterialPayload>({
    mutationFn: rawMaterialsApi.create,
    onSuccess: invalidate,
  });

  const update = useMutation<RawMaterial, Error, { id: string; payload: UpdateRawMaterialPayload }>({
    mutationFn: ({ id, payload }) => rawMaterialsApi.update(id, payload),
    onSuccess: invalidate,
  });

  const setActive = useMutation<RawMaterial, Error, { id: string; isActive: boolean }>({
    mutationFn: ({ id, isActive }) => rawMaterialsApi.setActive(id, isActive),
    onSuccess: invalidate,
  });

  return { create, update, setActive };
}
