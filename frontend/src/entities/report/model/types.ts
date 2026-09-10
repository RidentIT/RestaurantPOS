import type { OrderPaymentMethod } from "@/entities/order";
import type { ProfitSummary } from "@/entities/expense";

export interface TopMenuItem {
  menuItemVariantId: string;
  name: string;
  category: string;
  quantitySold: number;
  revenue: number;
}

export interface CategorySales {
  category: string;
  revenue: number;
  /** Share of the period's item revenue, 0-100. */
  percentageOfTotal: number;
  quantitySold: number;
}

export interface PaymentMethodBreakdown {
  method: OrderPaymentMethod;
  amount: number;
  /** Share of the period's takings, 0-100. */
  percentageOfTotal: number;
  count: number;
}

/** Takings for one hour of the day (0-23), in the restaurant's own local time. */
export interface HourlySales {
  hour: number;
  revenue: number;
  orderCount: number;
}

/** Matches the backend `DayOfWeek` enum, serialised by name — "Sunday" through "Saturday". */
export type DayOfWeekName =
  | "Sunday"
  | "Monday"
  | "Tuesday"
  | "Wednesday"
  | "Thursday"
  | "Friday"
  | "Saturday";

export interface DayOfWeekSales {
  day: DayOfWeekName;
  revenue: number;
  orderCount: number;
}

export interface DiscountSummary {
  totalDiscountGiven: number;
  ordersWithDiscount: number;
  totalOrders: number;
  /** Share of orders that carried a discount, 0-100. */
  percentageOfOrdersDiscounted: number;
}

/**
 * One steward's share of a period's sales. Takeaway and un-assigned dine-in orders collapse into
 * a single row with `stewardId` null and the name "Unassigned", always sorted last.
 */
export interface SalesBySteward {
  stewardId: string | null;
  stewardName: string;
  ordersServed: number;
  itemsSold: number;
  /** Menu value of everything sold, before any discount. */
  grossSales: number;
  discountsGiven: number;
  /** Gross less discounts — the figure the table is ranked on. */
  netSales: number;
  averageBill: number;
}

/** A period's sales in depth: what sold, how it was paid for, and when the till was busy. */
export interface SalesReport {
  from: string;
  to: string;
  periodLabel: string;
  summary: ProfitSummary;
  topItems: TopMenuItem[];
  categories: CategorySales[];
  paymentMethods: PaymentMethodBreakdown[];
  hourlyPattern: HourlySales[];
  dayOfWeekPattern: DayOfWeekSales[];
  /** Sales per steward, best (highest net) first. */
  stewardSales: SalesBySteward[];
  discounts: DiscountSummary;
}
