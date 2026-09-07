import type { UnitOfMeasurement } from "@/entities/raw-material";

export type StoreType = "MainStore" | "Kitchen";

export type StockMovementType =
  | "GoodsReceived"
  | "StockReleaseOut"
  | "StockReleaseIn"
  | "Adjustment"
  | "Consumption";

/** A raw material's current balance in one store. */
export interface StockLevel {
  rawMaterialId: string;
  rawMaterialName: string;
  unitOfMeasurement: UnitOfMeasurement;
  store: StoreType;
  quantityOnHand: number;
  isLowStock: boolean;
}

/** One row in the permanent stock ledger. */
export interface StockMovement {
  id: string;
  rawMaterialId: string;
  rawMaterialName: string;
  unitOfMeasurement: UnitOfMeasurement;
  store: StoreType;
  quantityDelta: number;
  type: StockMovementType;
  referenceId: string | null;
  performedByUserId: string;
  performedByName: string;
  occurredAtUtc: string;
  notes: string | null;
}

export interface StockMovementLine {
  rawMaterialId: string;
  rawMaterialName: string;
  unitOfMeasurement: UnitOfMeasurement;
  quantity: number;
}

export interface GoodsReceivedNote {
  id: string;
  supplierId: string;
  supplierName: string;
  receivedByUserId: string;
  receivedByName: string;
  receivedAtUtc: string;
  notes: string | null;
  purchaseOrderId: string | null;
  /** 1-5. Null when no rating was recorded for this delivery. */
  qualityRating: number | null;
  hasIssue: boolean;
  lines: StockMovementLine[];
}

export interface GoodsReceivedNoteSummary {
  id: string;
  supplierId: string;
  supplierName: string;
  receivedByName: string;
  receivedAtUtc: string;
  notes: string | null;
  purchaseOrderId: string | null;
  qualityRating: number | null;
  hasIssue: boolean;
  lineCount: number;
  rawMaterialNames: string[];
}

export interface StockRelease {
  id: string;
  requestedByUserId: string;
  requestedByName: string;
  requestedAtUtc: string;
  approvedByUserId: string;
  approvedByName: string;
  approvedAtUtc: string;
  notes: string | null;
  lines: StockMovementLine[];
}

export interface StockReleaseSummary {
  id: string;
  requestedByName: string;
  requestedAtUtc: string;
  approvedByName: string;
  approvedAtUtc: string;
  notes: string | null;
  lineCount: number;
  rawMaterialNames: string[];
}

export interface StockLineInput {
  rawMaterialId: string;
  quantity: number;
}

export interface CreateGoodsReceivedNotePayload {
  supplierId: string;
  lines: StockLineInput[];
  notes: string | null;
  purchaseOrderId?: string | null;
  qualityRating?: number | null;
  hasIssue?: boolean;
}

export interface CreateStockReleasePayload {
  lines: StockLineInput[];
  pin: string;
  notes: string | null;
}

export interface CreateStockAdjustmentPayload {
  rawMaterialId: string;
  store: StoreType;
  quantityDelta: number;
  reason: string;
}

export interface StockMovementFilters {
  rawMaterialId?: string;
  from?: string;
  to?: string;
}

export interface ConsumedLine {
  rawMaterialId: string;
  rawMaterialName: string;
  quantityDeducted: number;
  unitOfMeasurement: UnitOfMeasurement;
}

export interface ConsumptionResult {
  deducted: boolean;
  lines: ConsumedLine[];
}
