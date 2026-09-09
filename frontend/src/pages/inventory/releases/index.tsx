import { useState } from "react";
import { PackageSearch, Plus } from "lucide-react";
import { StockReleaseDialog, useStockReleases } from "@/features/inventory";
import { useRawMaterials } from "@/features/raw-materials";
import {
  Button,
  Card,
  EmptyState,
  LoadingState,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/shared/ui";

export default function ReleasesPage() {
  const [dialogOpen, setDialogOpen] = useState(false);
  const { data: releases, isLoading } = useStockReleases();
  const { data: rawMaterials } = useRawMaterials();

  const activeRawMaterials = (rawMaterials ?? []).filter((r) => r.isActive);

  return (
    <div className="space-y-6 p-8">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold">Kitchen Stock Release</h1>
          <p className="text-sm text-muted-foreground">
            Move stock from the Main Store to the Kitchen, authorised by an administrator's PIN.
          </p>
        </div>
        <Button onClick={() => setDialogOpen(true)}>
          <Plus /> Release stock
        </Button>
      </div>

      <Card>
        {isLoading ? (
          <LoadingState label="Loading releases…" />
        ) : !releases || releases.length === 0 ? (
          <EmptyState
            icon={<PackageSearch className="size-6" />}
            title="No stock has been released yet"
            description="Release raw materials to the kitchen with the button above."
          />
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Date</TableHead>
                <TableHead>Raw materials</TableHead>
                <TableHead>Requested by</TableHead>
                <TableHead>Approved by</TableHead>
                <TableHead>Notes</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {releases.map((release) => (
                <TableRow key={release.id}>
                  <TableCell className="whitespace-nowrap text-sm text-muted-foreground">
                    {new Date(release.requestedAtUtc).toLocaleString()}
                  </TableCell>
                  <TableCell
                    className="max-w-56 truncate text-sm"
                    title={(release.rawMaterialNames ?? []).join(", ")}
                  >
                    {release.rawMaterialNames?.length ? release.rawMaterialNames.join(", ") : release.lineCount}
                  </TableCell>
                  <TableCell className="text-sm text-muted-foreground">{release.requestedByName}</TableCell>
                  <TableCell className="text-sm text-muted-foreground">{release.approvedByName}</TableCell>
                  <TableCell className="max-w-56 truncate text-sm text-muted-foreground">
                    {release.notes ?? "—"}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </Card>

      <StockReleaseDialog open={dialogOpen} onOpenChange={setDialogOpen} rawMaterials={activeRawMaterials} />
    </div>
  );
}
