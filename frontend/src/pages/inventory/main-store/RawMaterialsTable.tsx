import { MoreHorizontal, Package, Pencil, ShieldCheck, ShieldOff } from "lucide-react";
import { toast } from "sonner";
import type { RawMaterial } from "@/entities/raw-material";
import { UNIT_ABBREVIATIONS } from "@/entities/raw-material";
import { useRawMaterialMutations } from "@/features/raw-materials";
import { toApiError } from "@/shared/api/problem";
import {
  Badge,
  Button,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  EmptyState,
  LoadingState,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/shared/ui";

export function RawMaterialsTable({
  rawMaterials,
  isLoading,
  onEdit,
}: {
  rawMaterials: RawMaterial[] | undefined;
  isLoading: boolean;
  onEdit: (rawMaterial: RawMaterial) => void;
}) {
  const { setActive } = useRawMaterialMutations();

  const toggleActive = async (material: RawMaterial) => {
    try {
      await setActive.mutateAsync({ id: material.id, isActive: !material.isActive });
      toast.success(material.isActive ? `${material.name} was deactivated.` : `${material.name} was reactivated.`);
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  };

  if (isLoading) {
    return <LoadingState label="Loading raw materials…" />;
  }

  if (!rawMaterials || rawMaterials.length === 0) {
    return (
      <EmptyState
        icon={<Package className="size-6" />}
        title="No raw materials yet"
        description="Add one to start building recipes and tracking stock."
      />
    );
  }

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Name</TableHead>
          <TableHead>Unit</TableHead>
          <TableHead>Main Store reorder level</TableHead>
          <TableHead>Kitchen par level</TableHead>
          <TableHead>Status</TableHead>
          <TableHead className="w-12" />
        </TableRow>
      </TableHeader>
      <TableBody>
        {rawMaterials.map((material) => (
          <TableRow key={material.id}>
            <TableCell className="font-medium">{material.name}</TableCell>
            <TableCell className="text-sm text-muted-foreground">
              {UNIT_ABBREVIATIONS[material.unitOfMeasurement]}
            </TableCell>
            <TableCell className="tabular text-sm text-muted-foreground">
              {material.mainStoreReorderLevel ?? "—"}
            </TableCell>
            <TableCell className="tabular text-sm text-muted-foreground">
              {material.kitchenParLevel ?? "—"}
            </TableCell>
            <TableCell>
              {material.isActive ? (
                <Badge variant="success">Active</Badge>
              ) : (
                <Badge variant="destructive">Deactivated</Badge>
              )}
            </TableCell>
            <TableCell>
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="ghost" size="icon">
                    <MoreHorizontal className="size-4" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end">
                  <DropdownMenuItem onSelect={() => onEdit(material)}>
                    <Pencil /> Edit
                  </DropdownMenuItem>
                  <DropdownMenuItem destructive={material.isActive} onSelect={() => toggleActive(material)}>
                    {material.isActive ? (
                      <>
                        <ShieldOff /> Deactivate
                      </>
                    ) : (
                      <>
                        <ShieldCheck /> Reactivate
                      </>
                    )}
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}
