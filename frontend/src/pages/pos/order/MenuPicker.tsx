import { useMemo, useState } from "react";
import { Search, UtensilsCrossed } from "lucide-react";
import type { MenuItem } from "@/entities/menu-item";
import { Button, EmptyState, Input } from "@/shared/ui";
import { cn } from "@/shared/lib/utils";

/**
 * The menu, grouped by category (POS-002).
 *
 * Built as big tap targets rather than a dropdown: at a busy till the cashier is looking at the
 * customer, not the screen, and a grid can be hit by muscle memory in a way a select cannot.
 */
export function MenuPicker({
  menuItems,
  onPick,
  disabled,
}: {
  menuItems: MenuItem[];
  onPick: (item: MenuItem) => void;
  disabled: boolean;
}) {
  const [search, setSearch] = useState("");
  const [category, setCategory] = useState<string>("All");

  const categories = useMemo(
    () => ["All", ...Array.from(new Set(menuItems.map((m) => m.category))).sort()],
    [menuItems],
  );

  const visible = useMemo(() => {
    const term = search.trim().toLowerCase();

    return menuItems.filter(
      (item) =>
        (category === "All" || item.category === category) &&
        (term === "" || item.name.toLowerCase().includes(term)),
    );
  }, [menuItems, category, search]);

  return (
    <div className="flex h-full flex-col gap-3">
      <div className="relative">
        <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          placeholder="Search the menu"
          className="pl-9"
          aria-label="Search the menu"
        />
      </div>

      <div className="flex flex-wrap gap-1.5">
        {categories.map((name) => (
          <button
            key={name}
            type="button"
            onClick={() => setCategory(name)}
            className={cn(
              "rounded-full border px-3 py-1 text-sm transition-colors",
              category === name
                ? "border-primary bg-primary text-primary-foreground"
                : "border-input hover:bg-muted",
            )}
          >
            {name}
          </button>
        ))}
      </div>

      <div className="min-h-0 flex-1 overflow-y-auto">
        {visible.length === 0 ? (
          <EmptyState
            icon={<UtensilsCrossed className="size-6" />}
            title="Nothing on the menu matches"
            description={menuItems.length === 0 ? "Add menu items in Recipe Management first." : undefined}
          />
        ) : (
          <div className="grid grid-cols-2 gap-2 lg:grid-cols-3">
            {visible.map((item) => (
              <Button
                key={item.id}
                type="button"
                variant="outline"
                disabled={disabled}
                onClick={() => onPick(item)}
                className="h-auto flex-col items-start gap-1 whitespace-normal p-3 text-left"
              >
                <span className="font-medium leading-snug">{item.name}</span>
                <span className="text-sm text-muted-foreground tabular">{item.price.toFixed(2)}</span>
              </Button>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
