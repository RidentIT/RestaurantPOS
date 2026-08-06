import type { RestaurantTable, TablePayload } from "@/entities/table";
import { API_ENDPOINTS, apiService } from "@/shared/api/endpoints";

export const tablesApi = {
  list: (isActive?: boolean) =>
    apiService.get<RestaurantTable[]>(API_ENDPOINTS.TABLES.BASE, { isActive }),

  create: (payload: TablePayload) => apiService.post<RestaurantTable>(API_ENDPOINTS.TABLES.BASE, payload),

  update: (id: string, payload: TablePayload) =>
    apiService.put<RestaurantTable>(API_ENDPOINTS.TABLES.BY_ID(id), payload),

  setActive: (id: string, isActive: boolean) =>
    apiService.put<RestaurantTable>(API_ENDPOINTS.TABLES.STATUS(id), { isActive }),
};
