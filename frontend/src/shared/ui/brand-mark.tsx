import * as React from "react";
import { cn } from "@/shared/lib/utils";

/** "Sri Lakshmi Family Restaurant" → "SL" — up to two letters, one per leading word. */
export function initialsOf(name: string): string {
  return name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((word) => word[0]?.toUpperCase() ?? "")
    .join("");
}

export interface BrandMarkProps {
  /** Where the logo image can be loaded from. Undefined skips straight to initials. */
  src?: string;
  name: string;
  className?: string;
}

/**
 * The restaurant's mark wherever its identity is shown — the uploaded logo when there is one,
 * initials in a coloured box otherwise. A logo that 404s (none uploaded yet, or the file went
 * missing) falls back the same way rather than showing a broken image.
 */
export function BrandMark({ src, name, className }: BrandMarkProps) {
  const [failed, setFailed] = React.useState(false);

  // A brand-new src (a fresh upload) deserves a fresh chance to load rather than staying stuck
  // on whatever the last one failed with.
  React.useEffect(() => setFailed(false), [src]);

  if (!src || failed) {
    return (
      <div
        className={cn(
          "flex items-center justify-center rounded-md bg-primary font-bold text-primary-foreground",
          className,
        )}
      >
        {initialsOf(name)}
      </div>
    );
  }

  return (
    <img
      src={src}
      alt={`${name} logo`}
      className={cn("rounded-md object-contain", className)}
      onError={() => setFailed(true)}
    />
  );
}
