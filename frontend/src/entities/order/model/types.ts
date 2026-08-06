/** Where an order sits in its lifecycle (POS-033). */
export type OrderStatus = "Draft" | "Open" | "Checkout" | "Completed" | "Cancelled";

/** How far the kitchen has got with one printed slip. */
export type KitchenTicketStatus = "New" | "Preparing" | "Ready" | "Served";

/** Why a slip was printed. */
export type KitchenTicketKind = "New" | "Addition" | "Modification" | "Cancellation";

/** How a customer settles a bill (POS-022). */
export type OrderPaymentMethod = "Cash" | "Card" | "Qr" | "BankTransfer";

export const ORDER_PAYMENT_METHODS: OrderPaymentMethod[] = ["Cash", "Card", "Qr", "BankTransfer"];

/** Labels for payment methods, since "Qr" and "BankTransfer" do not read well raw. */
export const PAYMENT_METHOD_LABELS: Record<OrderPaymentMethod, string> = {
  Cash: "Cash",
  Card: "Card",
  Qr: "QR",
  BankTransfer: "Bank Transfer",
};

export type DiscountType = "None" | "Percentage" | "Fixed";

export interface OrderItem {
  id: string;
  menuItemId: string;
  menuItemName: string;
  unitPrice: number;
  quantity: number;
  specialInstructions: string | null;
  /** A voided line stays on the bill as a record but contributes nothing. */
  isCancelled: boolean;
  lineTotal: number;
}

export interface OrderPayment {
  id: string;
  method: OrderPaymentMethod;
  amount: number;
  tenderedAmount: number | null;
  changeGiven: number;
  reference: string | null;
  createdAtUtc: string;
}

export interface Order {
  id: string;
  orderNumber: number | null;
  orderDate: string | null;
  tableId: string;
  tableNumber: string;
  status: OrderStatus;
  cashierUserId: string;
  cashierName: string;
  createdAtUtc: string;
  confirmedAtUtc: string | null;
  completedAtUtc: string | null;
  discountType: DiscountType;
  discountValue: number;
  subtotal: number;
  discountAmount: number;
  total: number;
  amountPaid: number;
  changeDue: number;
  kitchenStatus: KitchenTicketStatus | null;
  receiptNumber: string | null;
  items: OrderItem[];
  payments: OrderPayment[];
}

export interface OrderSummary {
  id: string;
  orderNumber: number | null;
  tableId: string;
  tableNumber: string;
  status: OrderStatus;
  cashierName: string;
  createdAtUtc: string;
  confirmedAtUtc: string | null;
  itemCount: number;
  total: number;
  kitchenStatus: KitchenTicketStatus | null;
}

export interface AddOrderItemInput {
  menuItemId: string;
  quantity: number;
  specialInstructions: string | null;
}

export interface OrderPaymentInput {
  method: OrderPaymentMethod;
  amount: number;
  /** Cash handed over when it exceeds the amount; the difference is the change. */
  tenderedAmount: number | null;
  reference: string | null;
}

/** A printable kitchen slip. */
export interface KotDocument {
  ticketId: string;
  restaurantName: string;
  orderNumber: number | null;
  tableNumber: string;
  ticketNumber: number;
  kind: KitchenTicketKind;
  cashierName: string;
  printedAtUtc: string;
  printCount: number;
  lines: KotDocumentLine[];
}

export interface KotDocumentLine {
  menuItemName: string;
  quantity: number;
  specialInstructions: string | null;
  note: string | null;
}

/**
 * An order after a change, plus any slip the change obliges. `kot` is null when nothing needs
 * printing — an edit to a draft the kitchen has never seen.
 */
export interface OrderMutationResult {
  order: Order;
  kot: KotDocument | null;
}

/** A printable customer receipt (POS-027). */
export interface ReceiptDocument {
  receiptNumber: string;
  restaurantName: string;
  addressLine1: string;
  addressLine2: string | null;
  city: string | null;
  phone: string | null;
  orderNumber: number | null;
  tableNumber: string;
  cashierName: string;
  issuedAtUtc: string;
  printCount: number;
  lines: ReceiptLine[];
  subtotal: number;
  discountAmount: number;
  /** Always zero — no VAT or GST is applied (BR-POS-011). */
  taxAmount: number;
  total: number;
  changeGiven: number;
  payments: OrderPayment[];
  /** Encoded on the slip as a QR code (POS-028). */
  qrPayload: string;
}

export interface ReceiptLine {
  menuItemName: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}
