import * as React from "react";
import { cn } from "@/shared/lib/utils";

export interface SegmentedTabsProps {
  tabs: ReadonlyArray<{ value: string; label: string; icon?: React.ReactNode }>;
  value: string;
  onValueChange: (value: string) => void;
  className?: string;
}

/**
 * A simple, accessible tab switcher for a page with a handful of local sections. Built from
 * plain buttons rather than a Radix primitive — for a handful of in-page views like these,
 * hand-rolled ARIA tab semantics are all that's needed, and it avoids a dependency this project
 * doesn't otherwise use.
 */
export function SegmentedTabs({ tabs, value, onValueChange, className }: SegmentedTabsProps) {
  return (
    <div
      role="tablist"
      className={cn("inline-flex flex-wrap gap-1 rounded-lg bg-muted p-1", className)}
    >
      {tabs.map((tab) => {
        const selected = tab.value === value;

        return (
          <button
            key={tab.value}
            type="button"
            role="tab"
            aria-selected={selected}
            onClick={() => onValueChange(tab.value)}
            className={cn(
              "inline-flex items-center gap-1.5 rounded-md px-3 py-1.5 text-sm font-medium transition-colors",
              selected
                ? "bg-background text-foreground shadow-sm"
                : "text-muted-foreground hover:text-foreground",
            )}
          >
            {tab.icon}
            {tab.label}
          </button>
        );
      })}
    </div>
  );
}
