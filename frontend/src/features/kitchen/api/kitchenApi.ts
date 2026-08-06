import type { KitchenTicket } from "@/entities/kitchen";
import type { KitchenTicketStatus, KotDocument } from "@/entities/order";
import { API_ENDPOINTS, apiService } from "@/shared/api/endpoints";

export const kitchenApi = {
  tickets: (includeServed = false) =>
    apiService.get<KitchenTicket[]>(API_ENDPOINTS.KITCHEN.TICKETS, { includeServed }),

  advance: (id: string, status: KitchenTicketStatus) =>
    apiService.put<KitchenTicket>(API_ENDPOINTS.KITCHEN.TICKET_STATUS(id), { status }),

  reprint: (id: string) => apiService.post<KotDocument>(API_ENDPOINTS.KITCHEN.TICKET_REPRINT(id)),
};
