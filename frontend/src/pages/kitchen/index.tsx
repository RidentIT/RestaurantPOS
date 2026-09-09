import { useState } from "react";
import { ChefHat, CircleCheck, Clock, Printer, TriangleAlert } from "lucide-react";
import { toast } from "sonner";
import type { KitchenTicket } from "@/entities/kitchen";
import { nextTicketStatus, TICKET_ACTION_LABELS } from "@/entities/kitchen";
import type { KitchenTicketStatus } from "@/entities/order";
import { useKitchenMutations, useKitchenTickets } from "@/features/kitchen";
import { toApiError } from "@/shared/api/problem";
import { Badge, Button, Card, EmptyState, LoadingState } from "@/shared/ui";
import { cn } from "@/shared/lib/utils";

/** How long a ticket may wait before the card starts shouting about it. */
const LATE_AFTER_MINUTES = 15;
const OVERDUE_AFTER_MINUTES = 25;

/** "45m", or "2h 15m" once it's been sitting long enough that raw minutes stop being readable. */
function formatWaiting(minutes: number): string {
  if (minutes < 60) return `${minutes}m`;

  const hours = Math.floor(minutes / 60);
  const rest = minutes % 60;

  return rest === 0 ? `${hours}h` : `${hours}h ${rest}m`;
}

const STATUS_BADGE: Record<KitchenTicketStatus, "outline" | "warning" | "success" | "secondary"> = {
  New: "outline",
  Preparing: "warning",
  Ready: "success",
  Served: "secondary",
};

function TicketCard({
  ticket,
  onAdvance,
  onReprint,
  busy,
}: {
  ticket: KitchenTicket;
  onAdvance: (ticket: KitchenTicket, status: KitchenTicketStatus) => void;
  onReprint: (ticket: KitchenTicket) => void;
  busy: boolean;
}) {
  const next = nextTicketStatus(ticket.status);
  const isCancellation = ticket.kind === "Cancellation";
  const overdue = ticket.waitingMinutes >= OVERDUE_AFTER_MINUTES;
  const late = ticket.waitingMinutes >= LATE_AFTER_MINUTES;

  return (
    <Card
      className={cn(
        "flex flex-col gap-3 border-2 p-4",
        isCancellation && "border-destructive bg-destructive/5",
        !isCancellation && overdue && "border-destructive",
        !isCancellation && !overdue && late && "border-warning",
      )}
    >
      <div className="flex items-start justify-between gap-2">
        <div>
          <p className="text-2xl font-semibold">{ticket.tableNumber ? `Table ${ticket.tableNumber}` : "Takeaway"}</p>
          <p className="text-sm text-muted-foreground">
            Order #{String(ticket.orderNumber ?? 0).padStart(3, "0")} · KOT-{ticket.ticketNumber}
          </p>
        </div>
        <div className="flex flex-col items-end gap-1">
          <Badge variant={isCancellation ? "destructive" : STATUS_BADGE[ticket.status]}>
            {isCancellation ? "Cancelled" : ticket.status}
          </Badge>
          <span
            className={cn(
              "flex items-center gap-1 text-sm tabular",
              overdue ? "font-semibold text-destructive" : late ? "text-warning" : "text-muted-foreground",
            )}
          >
            <Clock className="size-3.5" />
            {formatWaiting(ticket.waitingMinutes)}
          </span>
        </div>
      </div>

      {ticket.kind !== "New" && !isCancellation && (
        <Badge variant="warning" className="w-fit gap-1">
          <TriangleAlert className="size-3.5" />
          {ticket.kind === "Addition" ? "Added to an existing order" : "Quantity changed"}
        </Badge>
      )}

      <ul className="space-y-2">
        {ticket.lines.map((line, index) => (
          <li key={`${ticket.id}-${index}`} className="flex gap-3">
            <span className="min-w-10 text-2xl font-bold tabular">{line.quantity}×</span>
            <div className="min-w-0">
              <p className={cn("text-lg font-medium leading-snug", isCancellation && "line-through")}>
                {line.menuItemName}
              </p>
              {line.specialInstructions && (
                <p className="text-sm italic text-muted-foreground">{line.specialInstructions}</p>
              )}
              {line.note && <p className="text-sm font-semibold text-destructive">** {line.note} **</p>}
            </div>
          </li>
        ))}
      </ul>

      <div className="mt-auto flex gap-2 pt-1">
        {next && (
          <Button className="flex-1" disabled={busy} onClick={() => onAdvance(ticket, next)}>
            {isCancellation ? "Acknowledge" : TICKET_ACTION_LABELS[ticket.status]}
          </Button>
        )}
        <Button
          variant="outline"
          size="icon"
          aria-label={`Reprint KOT-${ticket.ticketNumber} for ${ticket.tableNumber ? `table ${ticket.tableNumber}` : "this takeaway order"}`}
          disabled={busy}
          onClick={() => onReprint(ticket)}
        >
          <Printer className="size-4" />
        </Button>
      </div>
    </Card>
  );
}

/**
 * The kitchen display (POS "Kitchen Operations").
 *
 * A wall screen nobody touches except to bump a ticket forward, so it refreshes itself and leans
 * on size and colour: table number and quantities are the largest things on each card, and a
 * ticket that has been waiting turns amber and then red on its own.
 */
export default function KitchenDisplayPage() {
  const [showServed, setShowServed] = useState(false);
  const { data: tickets, isLoading } = useKitchenTickets(showServed);
  const { advance, reprint } = useKitchenMutations();

  const busy = advance.isPending || reprint.isPending;

  const advanceTicket = async (ticket: KitchenTicket, status: KitchenTicketStatus) => {
    try {
      await advance.mutateAsync({ id: ticket.id, status });
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  };

  const reprintTicket = async (ticket: KitchenTicket) => {
    try {
      await reprint.mutateAsync(ticket.id);
      toast.success("Slip sent to the printer.");
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  };

  const waiting = (tickets ?? []).filter((t) => t.status !== "Served").length;

  return (
    <div className="space-y-5 p-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">Kitchen</h1>
          <p className="text-sm text-muted-foreground">
            {waiting === 0 ? "Nothing waiting." : `${waiting} ticket${waiting === 1 ? "" : "s"} on the pass`}
            {showServed && " · showing today's served and cancelled tickets too"}
          </p>
        </div>
        <Button variant="outline" onClick={() => setShowServed((current) => !current)}>
          {showServed ? "Hide served" : "Show served"}
        </Button>
      </div>

      {isLoading ? (
        <LoadingState label="Loading the pass…" />
      ) : !tickets || tickets.length === 0 ? (
        <EmptyState
          icon={showServed ? <CircleCheck className="size-6" /> : <ChefHat className="size-6" />}
          title="Nothing to cook"
          description="New orders appear here the moment the till confirms them."
        />
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
          {tickets.map((ticket) => (
            <TicketCard
              key={ticket.id}
              ticket={ticket}
              onAdvance={advanceTicket}
              onReprint={reprintTicket}
              busy={busy}
            />
          ))}
        </div>
      )}
    </div>
  );
}
