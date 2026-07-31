import { axiosClient } from "../axiosClient";

export const API_ENDPOINTS = {
  AUTH: {
    LOGIN: "/auth/login",
    REFRESH: "/auth/refresh",
    LOGOUT: "/auth/logout",
    ME: "/auth/me",
  },
  ORDERS: {
    BASE: "/orders",
    BY_ID: (id: string) => `/orders/${id}`,
    STATUS: (id: string) => `/orders/${id}/status`,
  },
  PRODUCTS: {
    BASE: "/products",
    BY_ID: (id: string) => `/products/${id}`,
    CATEGORIES: "/products/categories",
  },
  INVENTORY: {
    BASE: "/inventory",
    STOCK: "/inventory/stock",
  },
  KITCHEN: {
    KOT: "/kitchen/tickets",
    UPDATE_STATUS: (id: string) => `/kitchen/tickets/${id}/status`,
  },
  SUPPLIERS: {
    BASE: "/suppliers",
    BY_ID: (id: string) => `/suppliers/${id}`,
  },
  REPORTS: {
    SALES: "/reports/sales",
    INVENTORY: "/reports/inventory",
  },
} as const;

export const apiService = {
  get: <T>(url: string, params?: Record<string, unknown>) =>
    axiosClient.get<T>(url, { params }).then((res) => res.data),

  post: <T>(url: string, data?: unknown) =>
    axiosClient.post<T>(url, data).then((res) => res.data),

  put: <T>(url: string, data?: unknown) =>
    axiosClient.put<T>(url, data).then((res) => res.data),

  delete: <T>(url: string) =>
    axiosClient.delete<T>(url).then((res) => res.data),
};
