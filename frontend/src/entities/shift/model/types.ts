import type { OrderPaymentMethod, OrderStatus } from "@/entities/order";

export interface ShiftPaymentTotal {
  method: OrderPaymentMethod;
  amount: number;
}

/** A shift's sales, totalled from the orders rung up during it. */
export interface ShiftSummary {
  ordersCompleted: number;
  itemsSold: number;
  /** Menu value of everything sold, before any discount. */
  grossSales: number;
  discountsGiven: number;
  netSales: number;
  averageBill: number;
  payments: ShiftPaymentTotal[];
  /** Cash the shift took — what it adds to the drawer. */
  cashSales: number;
  /** Orders opened in this shift still waiting to be paid or cancelled. */
  unsettledOrders: number;
}

export interface Shift {
  id: string;
  cashierUserId: string;
  cashierName: string;
  checkedInAtUtc: string;
  checkedOutAtUtc: string | null;
  isOpen: boolean;
  openingCash: number;
  /** Opening cash plus cash sales — live while open, frozen once closed. */
  expectedCash: number;
  closingCash: number | null;
  /** Counted less expected: negative is a shortage. Null when nobody counted. */
  variance: number | null;
  note: string | null;
  summary: ShiftSummary;
}

export interface CurrentShift {
  shift: Shift | null;
}

export interface ShiftOrder {
  orderId: string;
  orderNumber: number | null;
  tableNumber: string | null;
  status: OrderStatus;
  total: number;
  completedAtUtc: string | null;
}

export interface ShiftDetail {
  shift: Shift;
  orders: ShiftOrder[];
}

export interface CheckOutPayload {
  closingCash: number;
  note: string | null;
}
