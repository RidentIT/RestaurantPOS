import { useMemo, useState } from "react";
import { Plus, UtensilsCrossed, X } from "lucide-react";
import type { MenuItem, MenuItemVariant } from "@/entities/menu-item";
import { useFrequentCategories, useFrequentCategoryMutations } from "@/features/menu-items";
import {
  Button,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  EmptyState,
} from "@/shared/ui";
import { cn } from "@/shared/lib/utils";

/**
 * The menu, grouped by category (POS-002).
 *
 * Built as big tap targets rather than a dropdown: at a busy till the cashier is looking at the
 * customer, not the screen, and a grid can be hit by muscle memory in a way a select cannot. A
 * single-size item is one tap target, same as always; a sized item (Normal/Full…) shows its sizes
 * as their own tap targets right on the card, rather than adding a second screen just to ask which
 * size — the cashier already knows what the customer ordered.
 */
export function MenuPicker({
  menuItems,
  onPick,
  disabled,
  search,
}: {
  menuItems: MenuItem[];
  onPick: (item: MenuItem, variant: MenuItemVariant) => void;
  disabled: boolean;
  /** The till-wide menu search, entered up in the order header rather than in here. */
  search: string;
}) {
  const [category, setCategory] = useState<string>("All");

  const { data: frequentCategories } = useFrequentCategories();
  const { pin, unpin } = useFrequentCategoryMutations();

  const categories = useMemo(
    () => ["All", ...Array.from(new Set(menuItems.map((m) => m.category))).sort()],
    [menuItems],
  );

  /** Categories not already pinned, offered in the "+" menu — "All" is never a pinnable category. */
  const pinnable = useMemo(
    () => categories.filter((name) => name !== "All" && !(frequentCategories ?? []).includes(name)),
    [categories, frequentCategories],
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
      <div className="flex flex-wrap items-center gap-1.5">
        {(frequentCategories ?? []).map((name) => (
          <div
            key={name}
            className={cn(
              "flex shrink-0 items-center gap-1 whitespace-nowrap rounded-full border-2 py-1 pl-3 pr-1 text-sm font-medium transition-colors",
              category === name
                ? "border-primary bg-primary text-primary-foreground"
                : "border-primary/40 hover:bg-muted",
            )}
          >
            <button type="button" onClick={() => setCategory(name)}>
              {name}
            </button>
            <button
              type="button"
              onClick={() => unpin.mutate(name)}
              aria-label={`Stop pinning ${name}`}
              className="rounded-full p-0.5 opacity-70 hover:opacity-100 hover:bg-black/10"
            >
              <X className="size-3" />
            </button>
          </div>
        ))}

        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <button
              type="button"
              aria-label="Pin a frequent category"
              className="flex size-7 shrink-0 items-center justify-center rounded-full border-2 border-dashed border-primary/40 text-muted-foreground transition-colors hover:bg-muted"
            >
              <Plus className="size-4" />
            </button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="start">
            {pinnable.length === 0 ? (
              <DropdownMenuItem disabled>Every category is already pinned</DropdownMenuItem>
            ) : (
              pinnable.map((name) => (
                <DropdownMenuItem key={name} onSelect={() => pin.mutate(name)}>
                  {name}
                </DropdownMenuItem>
              ))
            )}
          </DropdownMenuContent>
        </DropdownMenu>
      </div>

      {/*
       * One scrolling row rather than letting the chips wrap: with a full-size menu (a dozen-plus
       * categories) wrapping ate 4-5 rows of height on a small POS screen before the cashier ever
       * saw a single dish — all of it taken from the scrollable grid below, which is what's
       * actually being searched for a table order. A swipeable strip costs one row no matter how
       * many categories the menu grows to.
       */}
      <div className="flex gap-1.5 overflow-x-auto pb-1">
        {categories.map((name) => (
          <button
            key={name}
            type="button"
            onClick={() => setCategory(name)}
            className={cn(
              "shrink-0 whitespace-nowrap rounded-full border px-3 py-1 text-sm transition-colors",
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
            description={
              menuItems.length === 0 ? "Add menu items in Recipe Management first." : undefined
            }
          />
        ) : (
          <div className="grid grid-cols-2 gap-2 lg:grid-cols-3">
            {visible.map((item) =>
              item.variants.length === 1 ? (
                <Button
                  key={item.id}
                  type="button"
                  variant="outline"
                  disabled={disabled}
                  onClick={() => onPick(item, item.variants[0])}
                  className="h-auto flex-col items-start gap-1 whitespace-normal p-3 text-left"
                >
                  <span className="font-medium leading-snug">{item.name}</span>
                  <span className="tabular text-sm text-muted-foreground">
                    {item.variants[0].price.toFixed(2)}
                  </span>
                </Button>
              ) : (
                <div key={item.id} className="flex flex-col gap-1.5 rounded-lg border p-3">
                  <span className="font-medium leading-snug">{item.name}</span>
                  <div className="flex flex-wrap gap-1.5">
                    {item.variants.map((variant) => (
                      <Button
                        key={variant.id}
                        type="button"
                        variant="outline"
                        size="sm"
                        disabled={disabled}
                        onClick={() => onPick(item, variant)}
                        className="h-auto flex-col items-start gap-0.5 whitespace-normal py-1.5 text-left"
                      >
                        <span className="text-xs">{variant.name}</span>
                        <span className="tabular text-xs text-muted-foreground">
                          {variant.price.toFixed(2)}
                        </span>
                      </Button>
                    ))}
                  </div>
                </div>
              ),
            )}
          </div>
        )}
      </div>
    </div>
  );
}
