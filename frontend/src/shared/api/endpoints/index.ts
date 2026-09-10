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
    PROFILE: "/auth/profile",
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
  STEWARDS: {
    BASE: "/stewards",
    BY_ID: (id: string) => `/stewards/${id}`,
    STATUS: (id: string) => `/stewards/${id}/status`,
  },
  MENU_ITEMS: {
    BASE: "/menu-items",
    BY_ID: (id: string) => `/menu-items/${id}`,
    STATUS: (id: string) => `/menu-items/${id}/status`,
    CATEGORIES: "/menu-items/categories",
    // Recipes belong to a size (MenuItemVariant), not the item itself — every size can have its own.
    RECIPE: (menuItemVariantId: string) => `/menu-items/variants/${menuItemVariantId}/recipe`,
    RECIPE_STATUS: (menuItemVariantId: string) => `/menu-items/variants/${menuItemVariantId}/recipe/status`,
  },
  RAW_MATERIALS: {
    BASE: "/raw-materials",
    BY_ID: (id: string) => `/raw-materials/${id}`,
    STATUS: (id: string) => `/raw-materials/${id}/status`,
  },
  SUPPLIERS: {
    BASE: "/suppliers",
    BY_ID: (id: string) => `/suppliers/${id}`,
    STATUS: (id: string) => `/suppliers/${id}/status`,
    PRICES: "/suppliers/prices",
    SET_PRICE: (id: string) => `/suppliers/${id}/prices`,
    PRICE_HISTORY: (id: string, rawMaterialId: string) =>
      `/suppliers/${id}/prices/${rawMaterialId}/history`,
    PERFORMANCE: (id: string) => `/suppliers/${id}/performance`,
  },
  PURCHASE_ORDERS: {
    BASE: "/purchase-orders",
    BY_ID: (id: string) => `/purchase-orders/${id}`,
    SUBMIT: (id: string) => `/purchase-orders/${id}/submit`,
    CONFIRM: (id: string) => `/purchase-orders/${id}/confirm`,
    CANCEL: (id: string) => `/purchase-orders/${id}/cancel`,
    PAYMENTS: (id: string) => `/purchase-orders/${id}/payments`,
  },
  TABLES: {
    BASE: "/tables",
    BY_ID: (id: string) => `/tables/${id}`,
    STATUS: (id: string) => `/tables/${id}/status`,
  },
  ORDERS: {
    BASE: "/orders",
    BY_ID: (id: string) => `/orders/${id}`,
    ITEMS: (id: string) => `/orders/${id}/items`,
    ITEM_QUANTITY: (id: string, itemId: string) => `/orders/${id}/items/${itemId}/quantity`,
    VOID_ITEM: (id: string, itemId: string) => `/orders/${id}/items/${itemId}/void`,
    CONFIRM: (id: string) => `/orders/${id}/confirm`,
    CANCEL: (id: string) => `/orders/${id}/cancel`,
    STEWARD: (id: string) => `/orders/${id}/steward`,
    DISCOUNT: (id: string) => `/orders/${id}/discount`,
    CHECKOUT: (id: string) => `/orders/${id}/checkout`,
    REOPEN: (id: string) => `/orders/${id}/reopen`,
    PAYMENTS: (id: string) => `/orders/${id}/payments`,
    REPRINT_RECEIPT: (id: string) => `/orders/${id}/receipt/reprint`,
  },
  NOTIFICATIONS: {
    BASE: "/notifications",
    EVALUATE: "/notifications/evaluate",
    READ: "/notifications/read",
    PREFERENCES: "/notifications/preferences",
    THRESHOLDS: "/notifications/thresholds",
  },
  EXPENSES: {
    BASE: "/expenses",
    BY_ID: (id: string) => `/expenses/${id}`,
    PAID: (id: string) => `/expenses/${id}/paid`,
    SUBMIT: (id: string) => `/expenses/${id}/submit`,
    APPROVE: "/expenses/approve",
    REJECT: "/expenses/reject",
    CATEGORIES: "/expenses/categories",
    CATEGORY_BY_ID: (id: string) => `/expenses/categories/${id}`,
    CATEGORY_STATUS: (id: string) => `/expenses/categories/${id}/status`,
    ATTACHMENTS: (id: string) => `/expenses/${id}/attachments`,
    ATTACHMENT_BY_ID: (attachmentId: string) => `/expenses/attachments/${attachmentId}`,
    REMOVE_ATTACHMENT: (id: string, attachmentId: string) => `/expenses/${id}/attachments/${attachmentId}`,
    RECURRING: "/expenses/recurring",
    RECURRING_BY_ID: (id: string) => `/expenses/recurring/${id}`,
    RECURRING_STATUS: (id: string) => `/expenses/recurring/${id}/status`,
    RECURRING_GENERATE: "/expenses/recurring/generate",
    REPORT_DAILY: "/expenses/reports/daily",
    REPORT_MONTHLY: "/expenses/reports/monthly",
    REPORT_RANGE: "/expenses/reports/range",
  },
  KITCHEN: {
    TICKETS: "/kitchen/tickets",
    TICKET_STATUS: (id: string) => `/kitchen/tickets/${id}/status`,
    TICKET_REPRINT: (id: string) => `/kitchen/tickets/${id}/reprint`,
  },
  REPORTS: {
    SALES_DAILY: "/reports/sales/daily",
    SALES_MONTHLY: "/reports/sales/monthly",
  },
  SETTINGS: {
    BASE: "/settings",
    PROFILE: "/settings/profile",
    BRANDING: "/settings/branding",
    LOGO: "/settings/logo",
    BILL_CHARGES: "/settings/bill-charges",
    RECEIPT_FOOTER: "/settings/receipt-footer",
    PRINTER: "/settings/printer",
    APPROVAL_PIN_POLICY: "/settings/approval-pin-policy",
    BACKUP: "/settings/backup",
    BACKUPS: "/settings/backups",
    BACKUPS_RESTORE: "/settings/backups/restore",
    BACKUPS_RUN_DAILY: "/settings/backups/run-daily",
  },
  INVENTORY: {
    MAIN_STORE_STOCK: "/inventory/main-store/stock",
    MAIN_STORE_MOVEMENTS: "/inventory/main-store/movements",
    MAIN_STORE_ADJUSTMENTS: "/inventory/main-store/adjustments",
    GOODS_RECEIVED: "/inventory/main-store/goods-received",
    GOODS_RECEIVED_BY_ID: (id: string) => `/inventory/main-store/goods-received/${id}`,
    KITCHEN_STOCK: "/inventory/kitchen/stock",
    KITCHEN_MOVEMENTS: "/inventory/kitchen/movements",
    KITCHEN_ADJUSTMENTS: "/inventory/kitchen/adjustments",
    CONSUMPTION: (menuItemVariantId: string) => `/inventory/kitchen/consumption/${menuItemVariantId}`,
    RELEASES: "/inventory/releases",
    RELEASE_BY_ID: (id: string) => `/inventory/releases/${id}`,
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

  /**
   * For a `FormData` body specifically. `axiosClient` fixes `Content-Type: application/json` as
   * a default for every request, which otherwise stomps on the `multipart/form-data; boundary=…`
   * header a file upload needs — the browser (via axios) can only set that correctly once nothing
   * has already claimed the header first, which the explicit `undefined` here clears the way for.
   */
  uploadFile: <T>(url: string, form: FormData) =>
    axiosClient.post<T>(url, form, { headers: { "Content-Type": undefined } }).then((res) => res.data),
};
