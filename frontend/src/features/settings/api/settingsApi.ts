import type { Backup, Branding, RestaurantSettings } from "@/entities/settings";
import { axiosClient } from "@/shared/api/axiosClient";
import { API_ENDPOINTS, apiService } from "@/shared/api/endpoints";

export const settingsApi = {
  get: () => apiService.get<RestaurantSettings>(API_ENDPOINTS.SETTINGS.BASE),

  /** Reachable without signing in — the sign-in screen itself needs this. */
  getBranding: () => apiService.get<Branding>(API_ENDPOINTS.SETTINGS.BRANDING),

  /** Where the logo image can be loaded from directly, e.g. as an `<img src>`. */
  logoUrl: () => `${axiosClient.defaults.baseURL ?? ""}${API_ENDPOINTS.SETTINGS.LOGO}`,

  uploadLogo: (file: File) => {
    const form = new FormData();
    form.append("file", file);

    return apiService.uploadFile<RestaurantSettings>(API_ENDPOINTS.SETTINGS.LOGO, form);
  },

  removeLogo: () => apiService.delete<RestaurantSettings>(API_ENDPOINTS.SETTINGS.LOGO),

  updateProfile: (input: {
    name: string;
    addressLine1: string;
    addressLine2: string | null;
    city: string | null;
    phone: string | null;
    logoPath: string | null;
    vatRegistrationNumber: string | null;
  }) => apiService.put<RestaurantSettings>(API_ENDPOINTS.SETTINGS.PROFILE, input),

  updateBillCharges: (taxRatePercent: number, serviceChargeRatePercent: number) =>
    apiService.put<RestaurantSettings>(API_ENDPOINTS.SETTINGS.BILL_CHARGES, {
      taxRatePercent,
      serviceChargeRatePercent,
    }),

  updateReceiptFooter: (message: string) =>
    apiService.put<RestaurantSettings>(API_ENDPOINTS.SETTINGS.RECEIPT_FOOTER, { message }),

  updateDefaultPrinter: (printerName: string | null) =>
    apiService.put<RestaurantSettings>(API_ENDPOINTS.SETTINGS.PRINTER, { printerName }),

  updateApprovalPinPolicy: (maxAttempts: number, lockoutMinutes: number) =>
    apiService.put<RestaurantSettings>(API_ENDPOINTS.SETTINGS.APPROVAL_PIN_POLICY, {
      maxAttempts,
      lockoutMinutes,
    }),

  updateBackupSettings: (backupFolderPath: string | null, retentionCount: number) =>
    apiService.put<RestaurantSettings>(API_ENDPOINTS.SETTINGS.BACKUP, {
      backupFolderPath,
      retentionCount,
    }),

  listBackups: () => apiService.get<Backup[]>(API_ENDPOINTS.SETTINGS.BACKUPS),

  createBackup: () => apiService.post<Backup>(API_ENDPOINTS.SETTINGS.BACKUPS),

  restoreBackup: (fileName: string, pin: string, confirmationText: string) =>
    apiService.post<void>(API_ENDPOINTS.SETTINGS.BACKUPS_RESTORE, { fileName, pin, confirmationText }),

  /** Safe to call every time the app opens — a no-op once today's backup already exists. */
  runDailyBackup: () => apiService.post<boolean>(API_ENDPOINTS.SETTINGS.BACKUPS_RUN_DAILY),
};
