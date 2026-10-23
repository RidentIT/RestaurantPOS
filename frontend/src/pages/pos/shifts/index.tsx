import { useState } from "react";
import { Link } from "react-router-dom";
import { ArrowLeft } from "lucide-react";
import { ShiftDetailDialog, ShiftsTable, useMyShifts } from "@/features/shifts";
import { Button, Card, LoadingState } from "@/shared/ui";

/**
 * The signed-in cashier's own shifts from the last seven days. Older ones aren't lost — the owner
 * can see the full history — this is only how far back a cashier can look.
 */
export default function MyShiftsPage() {
  const { data: shifts, isLoading } = useMyShifts();
  const [openId, setOpenId] = useState<string | null>(null);

  return (
    <div className="space-y-6 p-8">
      <div className="space-y-1">
        <Button variant="ghost" size="sm" asChild className="-ml-2">
          <Link to="/pos">
            <ArrowLeft /> POS &amp; Billing
          </Link>
        </Button>
        <h1 className="text-2xl font-semibold">My shifts</h1>
        <p className="text-sm text-muted-foreground">Your shifts from the last 7 days. Select one to see its orders.</p>
      </div>

      <Card className="p-4">
        {isLoading || !shifts ? (
          <LoadingState label="Loading your shifts…" />
        ) : (
          <ShiftsTable
            shifts={shifts}
            onOpen={(shift) => setOpenId(shift.id)}
            emptyTitle="You haven't worked a shift in the last 7 days."
          />
        )}
      </Card>

      <ShiftDetailDialog shiftId={openId} onOpenChange={(open) => !open && setOpenId(null)} />
    </div>
  );
}
