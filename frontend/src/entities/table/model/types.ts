import type { KitchenTicketStatus, OrderStatus } from "@/entities/order";
import { orderDisplayStatus } from "@/entities/order";

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
  /** The steward serving this table, or null if none has been assigned yet. */
  stewardName: string | null;
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
  return table.currentOrder ? orderDisplayStatus(table.currentOrder) : "Available";
}
