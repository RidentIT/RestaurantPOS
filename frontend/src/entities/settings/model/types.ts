/** Everything an administrator can configure about the restaurant itself. */
export interface RestaurantSettings {
  name: string;
  addressLine1: string;
  addressLine2: string | null;
  city: string | null;
  phone: string | null;
  logoPath: string | null;
  /** Printed under the address on receipts when set. Optional — not every restaurant is VAT-registered. */
  vatRegistrationNumber: string | null;
  /** Percentage added on top of the discounted subtotal, 0-100. Zero by default. */
  taxRatePercent: number;
  serviceChargeRatePercent: number;
  receiptFooterMessage: string;
  /** Where customer receipts print, and the fallback for kitchen tickets. Null = Windows default. */
  defaultPrinterName: string | null;
  /** Where kitchen tickets (KOTs) print. Null falls back to {@link defaultPrinterName}. */
  kitchenPrinterName: string | null;
  approvalPinMaxAttempts: number;
  approvalPinLockoutMinutes: number;
  /** Null uses the default Backups folder beside the app. */
  backupFolderPath: string | null;
  backupRetentionCount: number;
}

/** One backup archive on disk. */
export interface Backup {
  fileName: string;
  createdAtUtc: string;
  sizeBytes: number;
}

/** What a sign-in screen is allowed to know before a session exists. */
export interface Branding {
  name: string;
}
