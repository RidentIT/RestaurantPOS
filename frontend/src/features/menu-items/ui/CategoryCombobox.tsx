import { useEffect, useMemo, useRef, useState } from "react";
import { Check, Plus } from "lucide-react";
import { useCreateMenuCategory, useMenuCategories } from "../model/useMenuItems";
import { Input } from "@/shared/ui";
import { cn } from "@/shared/lib/utils";

export interface CategoryComboboxProps {
  id?: string;
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
}

/**
 * Picks a menu category from every name already known to the system, or types a new one.
 *
 * A plain text field would have every admin retyping "Rice & Curry" by hand and eventually
 * getting it slightly wrong — which silently splits that category's sales figures in two on the
 * Reports screen. This suggests what already exists first, and only falls through to "create a
 * new category" when nothing on the list matches.
 */
export function CategoryCombobox({ id, value, onChange, placeholder }: CategoryComboboxProps) {
  const { data: categories } = useMenuCategories();
  const createCategory = useCreateMenuCategory();

  const [open, setOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const onClickOutside = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setOpen(false);
      }
    };

    document.addEventListener("mousedown", onClickOutside);
    return () => document.removeEventListener("mousedown", onClickOutside);
  }, []);

  const trimmed = value.trim();

  const matches = useMemo(() => {
    const term = trimmed.toLowerCase();
    return (categories ?? []).filter((category) => category.toLowerCase().includes(term));
  }, [categories, trimmed]);

  const exactMatch = (categories ?? []).some((category) => category.toLowerCase() === trimmed.toLowerCase());
  const canOfferNew = trimmed.length > 0 && !exactMatch;

  const pick = (category: string) => {
    onChange(category);
    setOpen(false);
  };

  const createNew = () => {
    onChange(trimmed);
    setOpen(false);
    // Fire-and-forget: registering the name is a convenience for next time, not something the
    // item being saved right now needs to wait on. A benign race with someone else registering
    // the same name at the same moment is just a "that already exists" the admin never sees.
    createCategory.mutate(trimmed);
  };

  return (
    <div ref={containerRef} className="relative">
      <Input
        id={id}
        value={value}
        onChange={(event) => {
          onChange(event.target.value);
          setOpen(true);
        }}
        onFocus={() => setOpen(true)}
        placeholder={placeholder}
        autoComplete="off"
      />

      {open && (matches.length > 0 || canOfferNew) && (
        <div className="absolute z-50 mt-1 max-h-56 w-full overflow-auto rounded-md border bg-popover p-1 text-popover-foreground shadow-md">
          {matches.map((category) => (
            <button
              key={category}
              type="button"
              onClick={() => pick(category)}
              className="flex w-full items-center gap-2 rounded-sm px-2 py-1.5 text-left text-sm hover:bg-accent hover:text-accent-foreground"
            >
              <Check
                className={cn("size-4 shrink-0", category === value ? "opacity-100" : "opacity-0")}
              />
              {category}
            </button>
          ))}

          {canOfferNew && (
            <button
              type="button"
              onClick={createNew}
              className="flex w-full items-center gap-2 rounded-sm px-2 py-1.5 text-left text-sm text-primary hover:bg-accent"
            >
              <Plus className="size-4 shrink-0" />
              Create category &ldquo;{trimmed}&rdquo;
            </button>
          )}
        </div>
      )}
    </div>
  );
}
