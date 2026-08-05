import type {
  CreateRawMaterialPayload,
  RawMaterial,
  RawMaterialFilters,
  UpdateRawMaterialPayload,
} from "@/entities/raw-material";
import { API_ENDPOINTS, apiService } from "@/shared/api/endpoints";

export const rawMaterialsApi = {
  list: (filters: RawMaterialFilters = {}) =>
    apiService.get<RawMaterial[]>(API_ENDPOINTS.RAW_MATERIALS.BASE, {
      search: filters.search || undefined,
      isActive: filters.isActive,
    }),

  create: (payload: CreateRawMaterialPayload) =>
    apiService.post<RawMaterial>(API_ENDPOINTS.RAW_MATERIALS.BASE, payload),

  update: (id: string, payload: UpdateRawMaterialPayload) =>
    apiService.put<RawMaterial>(API_ENDPOINTS.RAW_MATERIALS.BY_ID(id), payload),

  setActive: (id: string, isActive: boolean) =>
    apiService.put<RawMaterial>(API_ENDPOINTS.RAW_MATERIALS.STATUS(id), { isActive }),
};
