import { History } from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  EmptyState,
  LoadingState,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/shared/ui";
import { useSupplierPriceHistory } from "../model/useSuppliers";

export interface SupplierPriceHistoryDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  supplierId: string;
  rawMaterialId: string;
  rawMaterialName: string;
}

/** Every price a supplier has been recorded as charging for one raw material, newest first. */
export function SupplierPriceHistoryDialog({
  open,
  onOpenChange,
  supplierId,
  rawMaterialId,
  rawMaterialName,
}: SupplierPriceHistoryDialogProps) {
  const { data: history, isLoading } = useSupplierPriceHistory(supplierId, rawMaterialId, open);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-lg">
        <DialogHeader>
          <DialogTitle>Price history</DialogTitle>
          <DialogDescription>{rawMaterialName}</DialogDescription>
        </DialogHeader>

        {isLoading ? (
          <LoadingState label="Loading history…" />
        ) : !history || history.length === 0 ? (
          <EmptyState icon={<History className="size-6" />} title="No price history yet" />
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Price</TableHead>
                <TableHead>Recorded by</TableHead>
                <TableHead>Date</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {history.map((entry, index) => (
                <TableRow key={`${entry.recordedAtUtc}-${index}`}>
                  <TableCell className="font-medium tabular">{entry.price.toFixed(2)}</TableCell>
                  <TableCell className="text-sm text-muted-foreground">{entry.recordedByName}</TableCell>
                  <TableCell className="whitespace-nowrap text-sm text-muted-foreground">
                    {new Date(entry.recordedAtUtc).toLocaleString()}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </DialogContent>
    </Dialog>
  );
}
