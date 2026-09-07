import type { PurchaseOrderLine } from "@/entities/supplier";

/**
 * A short, stable reference for a purchase order.
 *
 * Orders carry no sequential number of their own, only a GUID, which is unreadable on screen and
 * impossible to say out loud to a supplier. The first block of the GUID is short enough to read
 * and already unique in practice across one restaurant's order history.
 */
export function purchaseOrderRef(id: string): string {
  return `PO-${id.replace(/-/g, "").slice(0, 6).toUpperCase()}`;
}

/**
 * Names the materials on an order, in as few characters as a dropdown row allows — the first two
 * spelled out, the rest counted. What was ordered is what actually distinguishes two orders placed
 * with the same supplier on the same day.
 */
export function describeOrderContents(lines: PurchaseOrderLine[]): string {
  if (lines.length === 0) {
    return "No materials on this order";
  }

  const named = lines.slice(0, 2).map((line) => line.rawMaterialName).join(", ");

  return lines.length > 2 ? `${named} +${lines.length - 2} more` : named;
}
