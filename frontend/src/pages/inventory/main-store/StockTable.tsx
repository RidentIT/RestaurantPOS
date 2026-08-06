import { PackageOpen } from "lucide-react";
import type { StockLevel } from "@/entities/inventory";
import { UNIT_ABBREVIATIONS } from "@/entities/raw-material";
import { Badge, EmptyState, LoadingState, Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/shared/ui";

export function StockTable({ stock, isLoading }: { stock: StockLevel[] | undefined; isLoading: boolean }) {
  if (isLoading) {
    return <LoadingState label="Loading stock levels…" />;
  }

  if (!stock || stock.length === 0) {
    return (
      <EmptyState
        icon={<PackageOpen className="size-6" />}
        title="No raw materials yet"
        description="Add a raw material to start tracking stock."
      />
    );
  }

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Raw material</TableHead>
          <TableHead>On hand</TableHead>
          <TableHead />
        </TableRow>
      </TableHeader>
      <TableBody>
        {stock.map((row) => (
          <TableRow key={row.rawMaterialId}>
            <TableCell className="font-medium">{row.rawMaterialName}</TableCell>
            <TableCell className="tabular">
              {row.quantityOnHand} {UNIT_ABBREVIATIONS[row.unitOfMeasurement]}
            </TableCell>
            <TableCell>{row.isLowStock && <Badge variant="warning">Low stock</Badge>}</TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}
