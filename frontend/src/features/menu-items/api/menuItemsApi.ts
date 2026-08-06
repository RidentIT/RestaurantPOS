import type { CreateMenuItemPayload, MenuItem, MenuItemFilters, UpdateMenuItemPayload } from "@/entities/menu-item";
import { API_ENDPOINTS, apiService } from "@/shared/api/endpoints";

export const menuItemsApi = {
  list: (filters: MenuItemFilters = {}) =>
    apiService.get<MenuItem[]>(API_ENDPOINTS.MENU_ITEMS.BASE, {
      search: filters.search || undefined,
      category: filters.category || undefined,
      isActive: filters.isActive,
    }),

  byId: (id: string) => apiService.get<MenuItem>(API_ENDPOINTS.MENU_ITEMS.BY_ID(id)),

  create: (payload: CreateMenuItemPayload) =>
    apiService.post<MenuItem>(API_ENDPOINTS.MENU_ITEMS.BASE, payload),

  update: (id: string, payload: UpdateMenuItemPayload) =>
    apiService.put<MenuItem>(API_ENDPOINTS.MENU_ITEMS.BY_ID(id), payload),

  setActive: (id: string, isActive: boolean) =>
    apiService.put<MenuItem>(API_ENDPOINTS.MENU_ITEMS.STATUS(id), { isActive }),
};
