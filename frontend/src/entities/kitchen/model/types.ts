import type { KitchenTicketKind, KitchenTicketStatus, KotDocumentLine } from "@/entities/order";

/** One card on the kitchen display. */
export interface KitchenTicket {
  id: string;
  orderId: string;
  orderNumber: number | null;
  /** Null for a takeaway order, which never holds a table. */
  tableNumber: string | null;
  ticketNumber: number;
  kind: KitchenTicketKind;
  status: KitchenTicketStatus;
  printedAtUtc: string;
  startedAtUtc: string | null;
  readyAtUtc: string | null;
  servedAtUtc: string | null;
  printCount: number;
  /** Minutes since the slip printed — what tells the kitchen what is going cold. */
  waitingMinutes: number;
  lines: KotDocumentLine[];
}

/** The status a ticket moves to next, or null once it is finished. */
export function nextTicketStatus(status: KitchenTicketStatus): KitchenTicketStatus | null {
  switch (status) {
    case "New":
      return "Preparing";
    case "Preparing":
      return "Ready";
    case "Ready":
      return "Served";
    default:
      return null;
  }
}

/** The verb on the button that moves a ticket on. */
export const TICKET_ACTION_LABELS: Record<KitchenTicketStatus, string> = {
  New: "Start cooking",
  Preparing: "Mark ready",
  Ready: "Mark served",
  Served: "Done",
};
