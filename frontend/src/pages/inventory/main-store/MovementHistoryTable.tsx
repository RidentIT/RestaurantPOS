import { History } from "lucide-react";
import type { StockMovement, StockMovementType } from "@/entities/inventory";
import { UNIT_ABBREVIATIONS } from "@/entities/raw-material";
import { Badge, EmptyState, LoadingState, Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/shared/ui";

const TYPE_LABELS: Record<StockMovementType, string> = {
  GoodsReceived: "Goods received",
  StockReleaseOut: "Released to kitchen",
  StockReleaseIn: "Received from store",
  Adjustment: "Adjustment",
  Consumption: "Sale",
};

export function MovementHistoryTable({ movements, isLoading }: { movements: StockMovement[] | undefined; isLoading: boolean }) {
  if (isLoading) {
    return <LoadingState label="Loading history…" />;
  }

  if (!movements || movements.length === 0) {
    return (
      <EmptyState
        icon={<History className="size-6" />}
        title="No stock movements yet"
        description="Receiving goods, releasing stock and adjustments will all show up here."
      />
    );
  }

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>When</TableHead>
          <TableHead>Raw material</TableHead>
          <TableHead>Type</TableHead>
          <TableHead>Change</TableHead>
          <TableHead>By</TableHead>
          <TableHead>Notes</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {movements.map((m) => (
          <TableRow key={m.id}>
            <TableCell className="whitespace-nowrap text-sm text-muted-foreground">
              {new Date(m.occurredAtUtc).toLocaleString()}
            </TableCell>
            <TableCell className="font-medium">{m.rawMaterialName}</TableCell>
            <TableCell>
              <Badge variant="outline">{TYPE_LABELS[m.type]}</Badge>
            </TableCell>
            <TableCell className={`tabular ${m.quantityDelta < 0 ? "text-destructive" : "text-success"}`}>
              {m.quantityDelta > 0 ? "+" : ""}
              {m.quantityDelta} {UNIT_ABBREVIATIONS[m.unitOfMeasurement]}
            </TableCell>
            <TableCell className="text-sm text-muted-foreground">{m.performedByName}</TableCell>
            <TableCell className="max-w-56 truncate text-sm text-muted-foreground">{m.notes ?? "—"}</TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}
