import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { ChefHat, MoreHorizontal, Pencil, Plus, Search, ShieldCheck, ShieldOff } from "lucide-react";
import { toast } from "sonner";
import type { MenuItem, MenuItemFilters } from "@/entities/menu-item";
import { getPriceRange, getRecipeCoverage } from "@/entities/menu-item";
import { useMenuItemMutations, useMenuItems } from "@/features/menu-items";
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
  Input,
  LoadingState,
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/shared/ui";

const STATUS_FILTER_ALL = "all";

const currency = new Intl.NumberFormat("en-LK", { style: "currency", currency: "LKR" });

export default function RecipesPage() {
  const navigate = useNavigate();
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState(STATUS_FILTER_ALL);

  const filters: MenuItemFilters = {
    search: search || undefined,
    isActive: statusFilter === STATUS_FILTER_ALL ? undefined : statusFilter === "active",
  };

  const { data: items, isLoading } = useMenuItems(filters);
  const { setActive } = useMenuItemMutations();

  const toggleActive = async (item: MenuItem) => {
    try {
      await setActive.mutateAsync({ id: item.id, isActive: !item.isActive });
      toast.success(item.isActive ? `${item.name} was deactivated.` : `${item.name} was reactivated.`);
    } catch (error) {
      toast.error(toApiError(error).message);
    }
  };

  return (
    <div className="space-y-6 p-8">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold">Recipe Management</h1>
          <p className="text-sm text-muted-foreground">
            Manage your menu and attach a recipe to track what each dish consumes.
          </p>
        </div>
        <Button asChild>
          <Link to="/recipes/new">
            <Plus /> Add menu item
          </Link>
        </Button>
      </div>

      <div className="flex flex-wrap gap-3">
        <div className="relative w-full max-w-xs">
          <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search by name or category…"
            className="pl-9"
          />
        </div>

        <Select value={statusFilter} onValueChange={setStatusFilter}>
          <SelectTrigger className="w-40">
            <SelectValue placeholder="Status" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value={STATUS_FILTER_ALL}>All statuses</SelectItem>
            <SelectItem value="active">Active</SelectItem>
            <SelectItem value="inactive">Deactivated</SelectItem>
          </SelectContent>
        </Select>
      </div>

      <Card>
        {isLoading ? (
          <LoadingState label="Loading menu items…" />
        ) : !items || items.length === 0 ? (
          <EmptyState
            icon={<ChefHat className="size-6" />}
            title="No menu items match your filters"
            description="Try clearing the search or filters, or add a new item."
          />
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Name</TableHead>
                <TableHead>Category</TableHead>
                <TableHead>Price</TableHead>
                <TableHead>Recipe</TableHead>
                <TableHead>Status</TableHead>
                <TableHead className="w-12" />
              </TableRow>
            </TableHeader>
            <TableBody>
              {items.map((item) => {
                const { min, max } = getPriceRange(item);
                const coverage = getRecipeCoverage(item);

                return (
                <TableRow key={item.id}>
                  <TableCell className="font-medium">
                    {item.name}
                    {item.variants.length > 1 && (
                      <span className="ml-1.5 text-xs font-normal text-muted-foreground">
                        ({item.variants.length} sizes)
                      </span>
                    )}
                  </TableCell>
                  <TableCell className="text-sm text-muted-foreground">{item.category}</TableCell>
                  <TableCell className="tabular">
                    {min === max ? currency.format(min) : `${currency.format(min)} – ${currency.format(max)}`}
                  </TableCell>
                  <TableCell>
                    {coverage === true ? (
                      <Badge variant="success">Recipe set</Badge>
                    ) : coverage === false ? (
                      <Badge variant="outline">No recipe</Badge>
                    ) : (
                      <Badge variant="warning">Partial</Badge>
                    )}
                  </TableCell>
                  <TableCell>
                    {item.isActive ? (
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
                        <DropdownMenuItem onSelect={() => navigate(`/recipes/${item.id}`)}>
                          <Pencil /> Edit / manage recipe
                        </DropdownMenuItem>
                        <DropdownMenuItem destructive={item.isActive} onSelect={() => toggleActive(item)}>
                          {item.isActive ? (
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
                );
              })}
            </TableBody>
          </Table>
        )}
      </Card>
    </div>
  );
}
