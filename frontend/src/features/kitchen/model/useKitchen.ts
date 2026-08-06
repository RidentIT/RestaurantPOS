import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import type { KitchenTicket } from "@/entities/kitchen";
import type { KitchenTicketStatus, KotDocument } from "@/entities/order";
import { printKot } from "@/features/printing";
import { kitchenApi } from "../api/kitchenApi";

const KITCHEN_TICKETS_KEY = "kitchen-tickets";

/**
 * The kitchen queue.
 *
 * Polled hard — every five seconds — because this screen hangs on a wall in the kitchen with
 * nobody touching it. A new order has to appear on its own, and a few seconds of staleness is a
 * few seconds a dish is not being cooked.
 */
export function useKitchenTickets(includeServed = false, pollMs = 5_000) {
  return useQuery({
    queryKey: [KITCHEN_TICKETS_KEY, includeServed],
    queryFn: () => kitchenApi.tickets(includeServed),
    refetchInterval: pollMs,
    placeholderData: (previous) => previous,
  });
}

export function useKitchenMutations() {
  const queryClient = useQueryClient();

  const advance = useMutation<KitchenTicket, Error, { id: string; status: KitchenTicketStatus }>({
    mutationFn: ({ id, status }) => kitchenApi.advance(id, status),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [KITCHEN_TICKETS_KEY] }),
  });

  const reprint = useMutation<KotDocument, Error, string>({
    mutationFn: kitchenApi.reprint,
    onSuccess: async (kot) => {
      queryClient.invalidateQueries({ queryKey: [KITCHEN_TICKETS_KEY] });
      const outcome = await printKot(kot);

      if (!outcome.success) {
        toast.error(`The slip did not print: ${outcome.message}`);
      }
    },
  });

  return { advance, reprint };
}
