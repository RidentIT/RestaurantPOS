import type { Backup, RestaurantSettings } from "@/entities/settings";
import { API_ENDPOINTS, apiService } from "@/shared/api/endpoints";

export const settingsApi = {
  get: () => apiService.get<RestaurantSettings>(API_ENDPOINTS.SETTINGS.BASE),

  updateProfile: (input: {
    name: string;
    addressLine1: string;
    addressLine2: string | null;
    city: string | null;
    phone: string | null;
    logoPath: string | null;
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
