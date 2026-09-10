import type { Steward } from "@/entities/steward";
import { API_ENDPOINTS, apiService } from "@/shared/api/endpoints";

export const stewardsApi = {
  list: (isActive?: boolean) =>
    apiService.get<Steward[]>(API_ENDPOINTS.STEWARDS.BASE, { isActive }),

  create: (name: string) => apiService.post<Steward>(API_ENDPOINTS.STEWARDS.BASE, { name }),

  rename: (id: string, name: string) => apiService.put<Steward>(API_ENDPOINTS.STEWARDS.BY_ID(id), { name }),

  setActive: (id: string, isActive: boolean) =>
    apiService.put<Steward>(API_ENDPOINTS.STEWARDS.STATUS(id), { isActive }),
};
