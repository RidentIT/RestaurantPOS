import type { OrderStatus, KitchenTicketStatus } from "@/entities/order";

/** A table on the floor plan. */
export interface RestaurantTable {
  id: string;
  number: string;
  seats: number;
  notes: string | null;
  isActive: boolean;
  /** Null when the table is free. */
  currentOrder: TableOrderSummary | null;
}

/** The live order sitting on a table, as much as the floor plan shows. */
export interface TableOrderSummary {
  orderId: string;
  orderNumber: number | null;
  status: OrderStatus;
  itemCount: number;
  total: number;
  confirmedAtUtc: string | null;
  cashierName: string;
  /** The least-advanced kitchen ticket. Null before anything reaches the kitchen. */
  kitchenStatus: KitchenTicketStatus | null;
}

export interface TablePayload {
  number: string;
  seats: number;
  notes: string | null;
}

/**
 * What a table tile shows at a glance. Derived rather than stored: a table is occupied exactly
 * when it has a live order, and how far along it is comes from the kitchen.
 */
export type TableDisplayStatus =
  | "Available"
  | "Draft"
  | "Ordered"
  | "Preparing"
  | "Ready"
  | "Served"
  | "Checkout";

export function tableDisplayStatus(table: RestaurantTable): TableDisplayStatus {
  const order = table.currentOrder;

  if (!order) return "Available";
  if (order.status === "Draft") return "Draft";
  if (order.status === "Checkout") return "Checkout";

  switch (order.kitchenStatus) {
    case "Preparing":
      return "Preparing";
    case "Ready":
      return "Ready";
    case "Served":
      return "Served";
    default:
      return "Ordered";
  }
}
