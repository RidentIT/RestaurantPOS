import { useState } from "react";
import { MoreHorizontal, Pencil, Plus, ShieldCheck, ShieldOff, UserRound } from "lucide-react";
import { toast } from "sonner";
import type { Steward } from "@/entities/steward";
import { StewardFormDialog, useStewardMutations, useStewards } from "@/features/stewards";
import { toApiError } from "@/shared/api/problem";
import {
  Badge,
  Button,
  Card,
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

/**
 * The waiting staff a table order can be credited to. Not accounts — no logins, no permissions —
 * just a list the cashier picks from and the sales-by-steward report groups on.
 */
export function StewardsTab() {
  const { data: stewards, isLoading } = useStewards();
  const { setActive } = useStewardMutations();

  const [form, setForm] = useState<Steward | null | undefined>(undefined);

  const toggleActive = async (steward: Steward) => {
    try {
      await setActive.mutateAsync({ id: steward.id, isActive: !steward.isActive });
      toast.success(steward.isActive ? `${steward.name} was retired.` : `${steward.name} is active again.`);
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between gap-4">
        <p className="text-sm text-muted-foreground">
          A retired steward stays on past reports but drops out of the order screen&rsquo;s picker.
        </p>
        <Button onClick={() => setForm(null)}>
          <Plus /> Add steward
        </Button>
      </div>

      <Card>
        {isLoading ? (
          <LoadingState label="Loading stewards…" />
        ) : !stewards || stewards.length === 0 ? (
          <EmptyState
            icon={<UserRound className="size-6" />}
            title="No stewards yet"
            description="Add the waiting staff so orders can be credited to them."
          />
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Name</TableHead>
                <TableHead>Status</TableHead>
                <TableHead className="w-12" />
              </TableRow>
            </TableHeader>
            <TableBody>
              {stewards.map((steward) => (
                <TableRow key={steward.id}>
                  <TableCell className="font-medium">{steward.name}</TableCell>
                  <TableCell>
                    {steward.isActive ? (
                      <Badge variant="success">Active</Badge>
                    ) : (
                      <Badge variant="destructive">Retired</Badge>
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
                        <DropdownMenuItem onSelect={() => setForm(steward)}>
                          <Pencil /> Rename
                        </DropdownMenuItem>
                        <DropdownMenuItem
                          destructive={steward.isActive}
                          onSelect={() => toggleActive(steward)}
                        >
                          {steward.isActive ? (
                            <>
                              <ShieldOff /> Retire
                            </>
                          ) : (
                            <>
                              <ShieldCheck /> Reinstate
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
        )}
      </Card>

      <StewardFormDialog
        open={form !== undefined}
        onOpenChange={(open) => !open && setForm(undefined)}
        steward={form ?? undefined}
      />
    </div>
  );
}
