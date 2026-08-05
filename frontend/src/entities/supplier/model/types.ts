import type { UnitOfMeasurement } from "@/entities/raw-material";

/** A goods supplier: who a GRN credits stock to, who a PO is sent to. */
export interface Supplier {
  id: string;
  name: string;
  contactName: string | null;
  phone: string | null;
  email: string | null;
  address: string | null;
  /** Days after delivery payment is due. 0 means cash on delivery. */
  paymentTermsDays: number;
  /** Maximum outstanding balance this supplier extends. Null means no limit is tracked. */
  creditLimit: number | null;
  /** Typical days between placing an order and delivery. Null means not yet known. */
  leadTimeDays: number | null;
  isActive: boolean;
  createdAtUtc: string;
}

export interface SupplierPayload {
  name: string;
  contactName: string | null;
  phone: string | null;
  email: string | null;
  address: string | null;
  paymentTermsDays: number;
  creditLimit: number | null;
  leadTimeDays: number | null;
}

export interface SupplierFilters {
  search?: string;
  isActive?: boolean;
}

/** Matches the backend `PurchaseOrderStatus` enum, serialised by name. */
export type PurchaseOrderStatus = "Draft" | "Submitted" | "Confirmed" | "Delivered" | "Cancelled";

export const PURCHASE_ORDER_STATUSES: PurchaseOrderStatus[] = [
  "Draft",
  "Submitted",
  "Confirmed",
  "Delivered",
  "Cancelled",
];

export interface PurchaseOrderLine {
  rawMaterialId: string;
  rawMaterialName: string;
  unitOfMeasurement: UnitOfMeasurement;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface PurchaseOrderLineInput {
  rawMaterialId: string;
  quantity: number;
  unitPrice: number;
}

/** A purchase order's header for a list screen, without its lines. */
export interface PurchaseOrderSummary {
  id: string;
  supplierId: string;
  supplierName: string;
  status: PurchaseOrderStatus;
  createdAtUtc: string;
  expectedDeliveryDate: string | null;
  totalAmount: number;
  amountPaid: number;
  balance: number;
  lineCount: number;
}

export interface PurchaseOrder {
  id: string;
  supplierId: string;
  supplierName: string;
  status: PurchaseOrderStatus;
  createdByUserId: string;
  createdByName: string;
  createdAtUtc: string;
  expectedDeliveryDate: string | null;
  submittedAtUtc: string | null;
  notes: string | null;
  totalAmount: number;
  amountPaid: number;
  balance: number;
  lines: PurchaseOrderLine[];
}

export interface CreatePurchaseOrderPayload {
  supplierId: string;
  lines: PurchaseOrderLineInput[];
  expectedDeliveryDate: string | null;
  notes: string | null;
}

export interface UpdatePurchaseOrderPayload {
  lines: PurchaseOrderLineInput[];
  expectedDeliveryDate: string | null;
  notes: string | null;
}

export interface PurchaseOrderFilters {
  supplierId?: string;
  status?: PurchaseOrderStatus;
}

export interface SupplierPrice {
  supplierId: string;
  supplierName: string;
  rawMaterialId: string;
  rawMaterialName: string;
  unitOfMeasurement: UnitOfMeasurement;
  price: number;
  updatedAtUtc: string;
}

export interface SupplierPriceHistoryEntry {
  price: number;
  recordedByUserId: string;
  recordedByName: string;
  recordedAtUtc: string;
}

/** Matches the backend `PaymentMethod` enum, serialised by name. */
export type PaymentMethod = "Cash" | "BankTransfer" | "Cheque" | "Card" | "Other";

export const PAYMENT_METHODS: PaymentMethod[] = ["Cash", "BankTransfer", "Cheque", "Card", "Other"];

export interface SupplierPayment {
  id: string;
  purchaseOrderId: string;
  amount: number;
  paymentDateUtc: string;
  method: PaymentMethod;
  invoiceReference: string | null;
  recordedByUserId: string;
  recordedByName: string;
  notes: string | null;
}

export interface RecordSupplierPaymentPayload {
  amount: number;
  paymentDateUtc: string;
  method: PaymentMethod;
  invoiceReference: string | null;
  notes: string | null;
}

/** Delivery and quality statistics for a supplier, derived from their purchase orders and GRNs. */
export interface SupplierPerformance {
  supplierId: string;
  supplierName: string;
  totalOrders: number;
  deliveredOrders: number;
  onTimeDeliveries: number;
  /** Percentage, 0-100. Null when no delivered order had an expected date to compare against. */
  onTimeDeliveryRate: number | null;
  /** Null when no order has both a submission date and a recorded delivery. */
  averageDeliveryDays: number | null;
  /** 1-5. Null when no GRN for this supplier has a quality rating recorded. */
  averageQualityRating: number | null;
  issueCount: number;
}
