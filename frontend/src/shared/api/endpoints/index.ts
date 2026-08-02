import { axiosClient } from "../axiosClient";

/**
 * Every API path in one place. Modules beyond user management are listed as they are built.
 */
export const API_ENDPOINTS = {
  AUTH: {
    LOGIN: "/auth/login",
    REFRESH: "/auth/refresh",
    LOGOUT: "/auth/logout",
    ME: "/auth/me",
    CHANGE_PASSWORD: "/auth/change-password",
    PIN: "/auth/pin",
    VERIFY_PIN: "/auth/pin/verify",
  },
  MODULES: "/modules",
  USERS: {
    BASE: "/users",
    BY_ID: (id: string) => `/users/${id}`,
    STATUS: (id: string) => `/users/${id}/status`,
    PASSWORD: (id: string) => `/users/${id}/password`,
  },
} as const;

/** Thin typed wrapper that unwraps `response.data`. */
export const apiService = {
  get: <T>(url: string, params?: Record<string, unknown>) =>
    axiosClient.get<T>(url, { params }).then((res) => res.data),

  post: <T>(url: string, data?: unknown) =>
    axiosClient.post<T>(url, data).then((res) => res.data),

  put: <T>(url: string, data?: unknown) => axiosClient.put<T>(url, data).then((res) => res.data),

  delete: <T>(url: string) => axiosClient.delete<T>(url).then((res) => res.data),
};
