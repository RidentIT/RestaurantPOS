/**
 * Theme tokens live in `src/app/globals.css` as raw HSL channels and reach components through
 * Tailwind classes (`bg-primary`, `text-muted-foreground`, …) configured in `tailwind.config.ts`.
 *
 * These helpers exist only for the rare case where a colour is needed in JavaScript — a canvas
 * chart or an inline SVG fill — so such code still reads from the same source of truth instead
 * of hard-coding a hex value.
 */

export type ThemeToken =
  | "background"
  | "foreground"
  | "card"
  | "primary"
  | "secondary"
  | "muted"
  | "accent"
  | "destructive"
  | "success"
  | "warning"
  | "border";

/** Returns a CSS colour expression for a token, e.g. `hsl(var(--primary) / 0.5)`. */
export function themeColor(token: ThemeToken, alpha = 1): string {
  return alpha === 1 ? `hsl(var(--${token}))` : `hsl(var(--${token}) / ${alpha})`;
}

/** Applies a theme by toggling the `dark` class that the Tailwind config keys off. */
export function applyTheme(theme: "light" | "dark"): void {
  if (typeof document === "undefined") return;

  document.documentElement.classList.toggle("dark", theme === "dark");
}
