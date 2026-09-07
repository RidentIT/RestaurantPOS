import { Truck } from "lucide-react";
import type { GoodsReceivedNoteSummary } from "@/entities/inventory";
import { EmptyState, LoadingState, Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/shared/ui";

export function GoodsReceivedTable({
  notes,
  isLoading,
}: {
  notes: GoodsReceivedNoteSummary[] | undefined;
  isLoading: boolean;
}) {
  if (isLoading) {
    return <LoadingState label="Loading goods received notes…" />;
  }

  if (!notes || notes.length === 0) {
    return (
      <EmptyState
        icon={<Truck className="size-6" />}
        title="No goods received yet"
        description="Record stock coming in from a supplier with “Receive goods”."
      />
    );
  }

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Date</TableHead>
          <TableHead>Supplier</TableHead>
          <TableHead>Raw materials</TableHead>
          <TableHead>Received by</TableHead>
          <TableHead>Notes</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {notes.map((note) => (
          <TableRow key={note.id}>
            <TableCell className="whitespace-nowrap text-sm text-muted-foreground">
              {new Date(note.receivedAtUtc).toLocaleString()}
            </TableCell>
            <TableCell className="font-medium">{note.supplierName}</TableCell>
            <TableCell className="max-w-56 truncate text-sm" title={(note.rawMaterialNames ?? []).join(", ")}>
              {note.rawMaterialNames?.length ? note.rawMaterialNames.join(", ") : note.lineCount}
            </TableCell>
            <TableCell className="text-sm text-muted-foreground">{note.receivedByName}</TableCell>
            <TableCell className="max-w-56 truncate text-sm text-muted-foreground">{note.notes ?? "—"}</TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}
