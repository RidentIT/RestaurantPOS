import type { CreateUserPayload, UpdateUserPayload, User, UserFilters } from "@/entities/user";
import { API_ENDPOINTS, apiService } from "@/shared/api/endpoints";

export const usersApi = {
  list: (filters: UserFilters = {}) =>
    apiService.get<User[]>(API_ENDPOINTS.USERS.BASE, {
      search: filters.search || undefined,
      role: filters.role,
      isActive: filters.isActive,
    }),

  byId: (id: string) => apiService.get<User>(API_ENDPOINTS.USERS.BY_ID(id)),

  create: (payload: CreateUserPayload) => apiService.post<User>(API_ENDPOINTS.USERS.BASE, payload),

  update: (id: string, payload: UpdateUserPayload) =>
    apiService.put<User>(API_ENDPOINTS.USERS.BY_ID(id), payload),

  setActive: (id: string, isActive: boolean) =>
    apiService.put<User>(API_ENDPOINTS.USERS.STATUS(id), { isActive }),

  resetPassword: (id: string, newPassword: string) =>
    apiService.post<void>(API_ENDPOINTS.USERS.PASSWORD(id), { newPassword }),
};
